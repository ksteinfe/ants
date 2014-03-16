using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using Rhino;
using Rhino.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace Ants {
    public class AntsEngineByFunction : GH_Component {
   
        protected PythonScript _py;
        private PythonCompiledCode _compiled_py;
        public AntsEngineByFunction()
            //Call the base constructor
            : base("Ants Compoent", "Ants", "Blah", "Ants", "Engine") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{7A7838C0-2EDA-451D-A9CF-973B72247E5E}"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
            pManager.Register_StringParam("Script", "Func", "The function to execute", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Generations", "G", "Number of Generations to create.", 0, GH_ParamAccess.item);
            //pManager.Register_IntegerParam("Number of Columns", "N", "Number of Columns", 0, GH_ParamAccess.item);
            //pManager.Register_BooleanParam("Connect Corners", "C", "Connect up the neighbors at corners?", false, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            AWorld refwrld = new AWorld();
            if (!DA.GetData(0, ref refwrld) || !refwrld.IsValid) return;
            AWorld wrld = new AWorld(refwrld);

            int nGen = 0;
            string pyString = "";
            if (!DA.GetData(1, ref pyString)) return;
            if (!DA.GetData(2, ref nGen)) return;
             
            _py = PythonScript.Create();
            _compiled_py = _py.Compile(pyString);

            //double d = EvaluateCell();
            //double d = 1.0;

            //var t = Type.GetType("IronPython.Runtime.List,IronPython");
            //IList val_list = Activator.CreateInstance(t) as IList;
            //List<double> val_list  = new List<double>();
            //int tot = wrld.gph.nodes.Count;
            //double[] val_list = new double[tot];

            // Main evaluation cycle
            // Should move code into the Antsworld Class

            for (int g = 0; g < nGen; g++)
            {
                
                //double[] cur_list = new double[tot];
                //cur_list = wrld.gens[g];
                //cur_list = val_list;

                double[] new_vals = new double[wrld.NodeCount];
                for (int i = 0; i < wrld.NodeCount; i++)
                {
                    int[] neighboring_indices = wrld.gph.NeighboringIndexesOf(i);
                    List<double> neighboring_vals = new List<double>();

                    // build list of neighboring values
                    for (int k = 0; k < neighboring_indices.Length; k++) neighboring_vals.Add(wrld.LatestGen[k]);

                    //double d = EvaluateCell(wrld.LatestGen[i], neighboring_vals);
                    double d = g + i + 0.0;

                    new_vals[i] = d;
                }
                wrld.AddGen(new_vals);
                //for (int i = 0; i < new_list.Length; i++) val_list[i] = new_list[i];

            }

            DA.SetData(0, wrld);

            //}
        }

        private double EvaluateCell(double cur_val, List<double> n_vals) {
           // _py.SetVariable("n_vals", NeighborList());
            _py.SetVariable("n_vals", n_vals);
            _py.SetVariable("h_val", cur_val);

            _compiled_py.Execute(_py);

            object o = _py.GetVariable("h_val");
            double d = System.Convert.ToDouble(o);
            return d;
        }

        private object NeighborList() {

            var t = Type.GetType("IronPython.Runtime.List,IronPython");
            IList list = Activator.CreateInstance(t) as IList;
            for (int i = 0; i < 8; i++) {
                object cast = i;
                list.Add(cast);
            }
            return list;
        }

    }


}
