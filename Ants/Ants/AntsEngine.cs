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
            pManager.Register_StringParam("Script", "Func", "The function to execute", GH_ParamAccess.item);
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Generations", "G", "Number of Generations to create.", 0, GH_ParamAccess.item);
            pManager.Register_DoubleParam("Value", "Vals", "Initial Values", GH_ParamAccess.list);
            //pManager.Register_IntegerParam("Number of Columns", "N", "Number of Columns", 0, GH_ParamAccess.item);
            //pManager.Register_BooleanParam("Connect Corners", "C", "Connect up the neighbors at corners?", false, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_DoubleParam("Value", "Val", "The extracted value", GH_ParamAccess.item);
            pManager.Register_DoubleParam("List", "List", "List of extracted values", GH_ParamAccess.list);
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            string pyString = "";
            AWorld wrld = new AWorld(new SpatialGraph());
            int nGen = 0;

            int tot = 0;

            List<double> v_list = new List<double>();

            if (!DA.GetData(0, ref pyString)) return;
            if (!DA.GetData(1, ref wrld)) return;
            if (!DA.GetData(2, ref nGen)) return;
            if (!DA.GetDataList(3, v_list)) return;
 

            tot = wrld.gph.nodes.Count;
            _py = PythonScript.Create();
            _compiled_py = _py.Compile(pyString);

            //double d = EvaluateCell();
            double d = 1.0;

            //var t = Type.GetType("IronPython.Runtime.List,IronPython");
            //IList val_list = Activator.CreateInstance(t) as IList;
            //List<double> val_list  = new List<double>();
            double[] val_list = new double[tot];
            double[] out_list = new double[tot];


            // Sets the initial Generation by using the input v_list
            // if it runs out of values, it starts over (wraps)
            int v_i = 0;
            for (int i = 0; i < tot; i++)
            {
                if (v_i == v_list.Count) v_i = 0;
                val_list[i] = v_list[v_i];
                v_i++;
            }

            wrld.AddGen(val_list);

            // Main evaluation cycle
            // Should move code into the Antsworld Class

            for (int g = 0; g < nGen; g++)
            {
                double[] new_list = new double[tot];
                double[] cur_list = new double[tot];

                //cur_list = wrld.gens[g];
                cur_list = val_list;

                for (int i = 0; i < tot; i++)
                {
                    int[] n_i = wrld.gph.NeighboringIndexesOf(i);
                    List<double> n_vals = new List<double>();

                    // build list of neighboring values
                    for (int k = 0; k < n_i.Length; k++) n_vals.Add(cur_list[n_i[k]]);

                    d = EvaluateCell(cur_list[i], n_vals);

                    new_list[i] = d;

                }

                wrld.AddGen(new_list);
                out_list = cur_list;
                for (int i = 0; i < new_list.Length; i++) val_list[i] = new_list[i];

            }

            DA.SetData(0, wrld.gens.Count);
            DA.SetDataList(1,out_list);
            DA.SetData(2, wrld);

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


//List<double> val_list = new List<double>();

//for (int i = 0; i < tot; i++)
//{
//    val_list.Add(i);
//}
