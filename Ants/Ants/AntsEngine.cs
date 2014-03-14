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
            pManager.Register_IntegerParam("Rows", "M", "Number of rows.", 0, GH_ParamAccess.item);
            pManager.Register_IntegerParam("Number of Columns", "N", "Number of Columns", 0, GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_DoubleParam("Value", "Val", "The extracted value", GH_ParamAccess.item);
            pManager.Register_DoubleParam("List", "List", "List of extracted values", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            string pyString = "";
            int m = 0;
            int n = 0;
            int tot = 0;


            if (!DA.GetData(0, ref pyString)) return;
            if (!DA.GetData(1, ref m)) return;
            if (!DA.GetData(2, ref n)) return;

            tot = m * n;
            //if ( DA.GetData(0, ref pyString) ) //if it works...
            //{
            _py = PythonScript.Create();
            _compiled_py = _py.Compile(pyString);

            double d = EvaluateCell();

            var t = Type.GetType("IronPython.Runtime.List,IronPython");
            IList val_list = Activator.CreateInstance(t) as IList;
            for (int i = 0; i < tot; i++)
            {
                object cast = i;
                val_list.Add(cast);
            }

            DA.SetData(0, d);
            DA.SetDataList(1,val_list);

            //}
        }

        private double EvaluateCell() {
            _py.SetVariable("n_vals", NeighborList());
            _py.SetVariable("h_val", 0.0);

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
