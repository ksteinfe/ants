using Grasshopper.Kernel;
using System;
using Rhino;
using Rhino.Runtime;
using System.Collections;

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

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_DoubleParam("Value", "Val", "The extracted value", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            string pyString = "";
            if ( DA.GetData(0, ref pyString) ) //if it works...
            {
                _py = PythonScript.Create();
                _compiled_py = _py.Compile(pyString);

                double d = EvaluateCell();

                DA.SetData(0, d);
            }
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
