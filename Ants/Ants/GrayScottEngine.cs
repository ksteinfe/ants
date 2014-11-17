using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Drawing;
using Rhino;
using Rhino.Runtime;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Ants {
    public class AntsEngineGrayScott : GH_Component {
   
        protected PythonScript _py;
        private PythonCompiledCode _compiled_py;
        readonly StringList m_py_output = new StringList(); // python output stream is piped to m_py_output

        public AntsEngineGrayScott()
            //Call the base constructor
            : base("Gray Scott Engine", "Gray Scott", "Creates a time history sequence based on the Gray Scott model", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{588576D6-E050-4BF8-A96B-06C6C36AC9D9}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_ant_world; } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph.", GH_ParamAccess.item);
            pManager.Register_DoubleParam("u values", "U", "Initial U Values", GH_ParamAccess.list);
            pManager.Register_DoubleParam("v values", "V", "Initial V Values", GH_ParamAccess.list);

            pManager.Register_DoubleParam("f coefficient", "F", "Feed Rate Coefficient", 0.023, GH_ParamAccess.item);
            pManager.Register_DoubleParam("k coefficient", "K", "Removal Rate Coefficient", 0.077, GH_ParamAccess.item);
            pManager.Register_DoubleParam("du coefficient", "DU", "U Diffusion Rate Coefficient", 0.095, GH_ParamAccess.item);
            pManager.Register_DoubleParam("dv coefficient", "DV", "V Diffusion Rate Coefficient", 0.03, GH_ParamAccess.item);
            pManager.Register_DoubleParam("timestep", "T", "Units of Time per Generation", 1.0, GH_ParamAccess.item);

            pManager.Register_IntegerParam("Generations", "G", "Number of Generations to create.", 1, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "WU", "The resulting AntsWorld of U Values.", GH_ParamAccess.item);
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "WV", "The resulting AntsWorld of V Values.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            
            SpatialGraph gph = new SpatialGraph();
            if (!DA.GetData(0, ref gph)) return;

            List<double> u_init = new List<double>();
            List<double> v_init = new List<double>();
            if (!DA.GetDataList(1, u_init)) return;
            if (!DA.GetDataList(2, v_init)) return;
            if (gph.nodes.Count != u_init.Count) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please supply exactly one U value for each node in the Spatial Graph");
            if (gph.nodes.Count != v_init.Count) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please supply exactly one V value for each node in the Spatial Graph");
            if ((gph.nodes.Count != u_init.Count) || (gph.nodes.Count != u_init.Count)) return;
            int cellCount = gph.nodes.Count;

            double f = 0.023; double k = 0.077; double du = 0.095; double dv = 0.03; double t = 1.0;
            if (!DA.GetData(3, ref f)) return;
            if (!DA.GetData(4, ref k)) return;
            if (!DA.GetData(5, ref du)) return;
            if (!DA.GetData(6, ref dv)) return;
            if (!DA.GetData(7, ref t)) return;

            int genCount = 1;
            if (!DA.GetData(8, ref genCount)) return;


            double[][] tabU = new double[genCount][];
            double[][] tabV = new double[genCount][];
            tabU[0] = u_init.ToArray();
            tabV[0] = v_init.ToArray();

            int[][] neighbors = new int[cellCount][];
            for (int n = 0; n < cellCount; n++) neighbors[n] = gph.NeighboringIndexesOf(n);

            for (int g = 1; g < genCount; g++) {
                tabU[g] = new double[cellCount];
                tabV[g] = new double[cellCount];
                for (int n = 0; n < cellCount; n++) {
                    int nei_count =  neighbors[n].Count();
                    double cur_u = tabU[g-1][n];
                    double cur_v = tabV[g-1][n];

                    double[] nei_u = new double[nei_count];
                    double[] nei_v = new double[nei_count];
                    for (int ni = 0; ni < nei_count; ni++) {
                        nei_u[ni] = tabU[g-1][neighbors[n][ni]];
                        nei_v[ni] = tabV[g-1][neighbors[n][ni]];
                    }

                    double d2 = cur_u * cur_v * cur_v;
                    double cur_du = du *(nei_u.Sum() - nei_count * cur_u) - d2;
                    double cur_dv = dv * (nei_v.Sum() - nei_count * cur_v) + d2;
                    double nxt_u = cur_u + t * (cur_du + f * (1.0 - cur_u));
                    double nxt_v = cur_v + t * (cur_dv - k * cur_v);

                    if (Double.IsNaN(nxt_u))  nxt_u = 0.0;
                    if (Double.IsNaN(nxt_v))  nxt_v = 0.0;
                    if (nxt_u > 99999) nxt_u = 99999;
                    if (nxt_v > 99999) nxt_v = 99999;

                    tabU[g][n] = Math.Max(0.0,nxt_u);
                    tabV[g][n] = Math.Max(0.0,nxt_v);
                }
            }

            AWorld wrldU = new AWorld(gph, u_init.Cast<object>().ToList());
            for (int g = 1; g < genCount; g++) wrldU.AddGen(tabU[g].Cast<object>().ToList());
            DA.SetData(0, wrldU);

            AWorld wrldV = new AWorld(gph, v_init.Cast<object>().ToList());
            for (int g = 1; g < genCount; g++) wrldV.AddGen(tabV[g].Cast<object>().ToList());
            DA.SetData(1, wrldV);
        }
       
        private void AddErrorNicely(StringList sw, Exception ex) {
            sw.Write(string.Format("Runtime error ({0}): {1}", ex.GetType().Name, ex.Message));

            string error = _py.GetStackTraceFromException(ex);

            error = error.Replace(", in <module>, \"<string>\"", ", in script");
            error = error.Trim();

            sw.Write(error);
        }

    }
}
