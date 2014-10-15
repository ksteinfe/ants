﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Drawing;
using Rhino;
using Rhino.Runtime;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using IronPython;
using IronPython.Modules;
using IronPython.Compiler.Ast;
using IronPython.Runtime.Binding;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;
using IronPython.Runtime.Types;
using IronPython.Runtime.Operations;
using IronPython.Compiler;
using IronPython.Hosting;


//using Picnic;

namespace Ants {
    public class AntsEngineByFunction : GH_Component {
   
        protected PythonScript _py;
        private PythonCompiledCode _compiled_py;
        readonly StringList m_py_output = new StringList(); // python output stream is piped to m_py_output

        public AntsEngineByFunction()
            //Call the base constructor
            : base("Ants Engine", "Ants", "Creates a time history sequence. Build 10.14.14.", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{7A7838C0-2EDA-451D-A9CF-973B72247E5E}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_ant_world; } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph.", GH_ParamAccess.item);
            pManager.Register_GenericParam("Script", "F", "The function to execute", GH_ParamAccess.item);
            pManager.Register_GenericParam("Value", "V", "Initial Values", GH_ParamAccess.list);
            pManager.Register_IntegerParam("Generations", "G", "Number of Generations to create.", 0, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_StringParam("Console", "...", "Messages from Python", GH_ParamAccess.tree);
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            AWorld refwrld = new AWorld();
            List<object> v_list = new List<object>();
            //List<object> vals = new List<object>();
            var IP_List_Type = Type.GetType("IronPython.Runtime.List,IronPython");


            List<GH_ObjectWrapper> vals = new List<GH_ObjectWrapper>();
            //List<GH_ObjectWrapper> v_list = new List<GH_ObjectWrapper>();
            List<GH_ObjectWrapper> output = new List<GH_ObjectWrapper>();


            GH_ObjectWrapper f = new GH_ObjectWrapper();

            SpatialGraph gph = new SpatialGraph();
            if (!DA.GetData(0, ref gph)) return;

            int nGen = 0;
            if (!DA.GetData(1, ref f)) return;
            if (!DA.GetDataList(2, vals)) return;
            if (!DA.GetData(3, ref nGen)) return;

            // Convert List<GH_ObjectWrappers> to List<Object>
            // Where each object is a .NET type rather than a GH type

            v_list = AWorld.Convert_List(vals);

            var y = v_list[0].GetType();

            // Sets the initial Generation by using the input v_list
            // if it runs out of values, it starts over (wraps)
            //object[] val_list = new object[gph.nodes.Count];  **
            List<object> val_list = new List<object>();

            int v_i = 0;
            for (int i = 0; i < gph.nodes.Count; i++)
            {
                if (v_i == v_list.Count) v_i = 0;
                val_list.Add(v_list[v_i]);
                v_i++;
            }

            AWorld wrld = new AWorld(gph, val_list);

            _py = PythonScript.Create();
            _py.Output = this.m_py_output.Write;


            // Not quite sure why this works, but appears to be necessary to
            // initialize the python environment and allow output to be captured
            _py.SetVariable("test", f.Value);
            _compiled_py = _py.Compile("\n");
            _compiled_py.Execute(_py);

            ScriptEngine pyEngine = Python.CreateEngine();
            ScriptScope pyScope = pyEngine.CreateScope();


            // console out
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String> consoleOut = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String>();

            // Main evaluation cycle
            // Should move code into the Antsworld Class
            for (int g = 0; g < nGen; g++)
            {
                this.m_py_output.Reset();

                //object[] new_vals = new object[wrld.NodeCount]; **
                List<object> new_vals = new List<object>();

                for (int i = 0; i < wrld.NodeCount; i++)
                {
                    new_vals.Add(0);

                    int[] neighboring_indices = wrld.gph.NeighboringIndexesOf(i);

                    // build list of neighboring values
                    IList neighboring_vals = Activator.CreateInstance(IP_List_Type) as IList;

                    for (int k = 0; k < neighboring_indices.Length; k++) neighboring_vals.Add(wrld.LatestGen.values[neighboring_indices[k]]);

                    try
                    {
                        dynamic result = pyScope.Engine.Operations.Invoke(f.Value, wrld.LatestGen.values[i], (IList<object>)neighboring_vals);
                        new_vals[i] =  (object)result;
                    }
                    catch (Exception ex)
                    {
                        AddErrorNicely(m_py_output, ex);
                        new_vals[i] = new object();
                    }

                }

                wrld.AddGen(new_vals);

                // console out
                Grasshopper.Kernel.Data.GH_Path key_path = new Grasshopper.Kernel.Data.GH_Path(g);
                List<Grasshopper.Kernel.Types.GH_String> gh_strs = new List<Grasshopper.Kernel.Types.GH_String>();
                foreach (String str in this.m_py_output.Result) gh_strs.Add(new Grasshopper.Kernel.Types.GH_String(str));
                consoleOut.AppendRange(gh_strs, key_path);

            }

            DA.SetDataTree(0, consoleOut);
            DA.SetData(1, wrld);

        }


        private object EvaluateCell(int cell_index, object cur_val, List<object> n_vals) {
            var t = Type.GetType("IronPython.Runtime.List,IronPython");
            IList n_list = Activator.CreateInstance(t) as IList;
            foreach (object d in n_vals) {
                object cast = d;
                n_list.Add(cast);
            }
            _py.SetVariable("n_vals", n_list);
            _py.SetVariable("h_val", cur_val);
            _py.SetVariable("h_idx", cell_index);

            try {
                _compiled_py.Execute(_py);
            } catch(Exception ex) {
                AddErrorNicely(m_py_output, ex);
            }
            object o = _py.GetVariable("h_val");
            return System.Convert.ToDouble(o);
        }


       
        private void AddErrorNicely(StringList sw, Exception ex) {
            sw.Write(string.Format("Runtime error ({0}): {1}", ex.GetType().Name, ex.Message));

            string error = _py.GetStackTraceFromException(ex);

            error = error.Replace(", in <module>, \"<string>\"", ", in script");
            error = error.Trim();

            sw.Write(error);
        }

    }

    //public class AntsEngineBySelection : GH_Component
    ///// Experiment with Selection Function
    //{

    //    protected PythonScript _py;
    //    private PythonCompiledCode _compiled_py;
    //    protected PythonScript _spy;
    //    private PythonCompiledCode _compiled_spy;

    //    readonly StringList m_py_output = new StringList(); // python output stream is piped to m_py_output

    //    public AntsEngineBySelection()
    //        //Call the base constructor
    //        : base("Ants Engine2", "Ants", "Creates a time history sequence with selection function.", "Ants", "Worlds") { }
    //    public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
    //    public override Guid ComponentGuid { get { return new Guid("{82B2E21E-E70D-4689-B31A-457F8CAD0300}"); } }

    //    protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_ant_world; } }

    //    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    //    {
    //        pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph.", GH_ParamAccess.item);
    //        pManager.Register_StringParam("Selection Script", "SFunc", "The selection function to execute", GH_ParamAccess.item);
    //        pManager.Register_BooleanParam("One or All", "Bool", "Select one or all", GH_ParamAccess.item);
    //        pManager.Register_StringParam("Eval Script", "EFunc", "The evaluation function to execute", GH_ParamAccess.item);
    //        pManager.Register_DoubleParam("Value", "V", "Initial Values", GH_ParamAccess.list);
    //        pManager.Register_IntegerParam("Generations", "G", "Number of Generations to create.", 0, GH_ParamAccess.item);
    //    }

    //    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    //    {
    //        pManager.Register_StringParam("Console", "...", "Messages from Python", GH_ParamAccess.tree);
    //        pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
    //    }

    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {
    //        AWorld refwrld = new AWorld();
    //        bool SelectType = false;
    //        List<object> v_list = new List<object>();
    //        Random rnd = new Random();

    //        //if (!DA.GetData(0, ref refwrld) || !refwrld.IsValid) return;
    //        //AWorld wrld = new AWorld(refwrld);

    //        SpatialGraph gph = new SpatialGraph();
    //        if (!DA.GetData(0, ref gph)) return;

    //        int nGen = 0;
    //        string pyString = "";
    //        string spyString = "";
    //        if (!DA.GetData(1, ref spyString)) return;
    //        if (!DA.GetData(2, ref SelectType)) return;
    //        if (!DA.GetData(3, ref pyString)) return;
    //        if (!DA.GetDataList(4, v_list)) return;
    //        if (!DA.GetData(5, ref nGen)) return;


    //        // Sets the initial Generation by using the input v_list
    //        // if it runs out of values, it starts over (wraps)
    //        object[] val_list = new object[gph.nodes.Count];
    //        int v_i = 0;
    //        for (int i = 0; i < gph.nodes.Count; i++)
    //        {
    //            if (v_i == v_list.Count) v_i = 0;
    //            val_list[i] = v_list[v_i];
    //            v_i++;
    //        }

    //        AWorld wrld = new AWorld(gph, val_list);

    //        // evaluation function
    //        _py = PythonScript.Create();
    //        _py.Output = this.m_py_output.Write;
    //        _compiled_py = _py.Compile(pyString);

    //        // selection function
    //        _spy = PythonScript.Create();
    //        _py.Output = this.m_py_output.Write;
    //        _compiled_spy = _py.Compile(spyString);

    //        // console out
    //        Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String> consoleOut = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String>();

    //        // Main evaluation cycle
    //        // Should move code into the Antsworld Class
    //        for (int g = 0; g < nGen; g++)
    //        {
    //            // console out
    //            this.m_py_output.Reset();
    //            object[] new_vals = new object[wrld.NodeCount];

    //            // build list to test
    //            List<int> nodes_to_test = new List<int>();

    //            for (int i = 0; i < wrld.NodeCount; i++)
    //            {
    //                // build this now since we will only change a few of them later
    //                new_vals[i] = wrld.LatestGen[i];

    //                int[] neighboring_indices = wrld.gph.NeighboringIndexesOf(i);
    //                double[] n_wts = wrld.gph.NeighboringWeightsOf(i);

    //                // build list of neighboring values
    //                List<object> neighboring_vals = new List<object>();
    //                for (int k = 0; k < neighboring_indices.Length; k++) neighboring_vals.Add(wrld.LatestGen[neighboring_indices[k]]);

    //                bool test = SelectCell(i, wrld.LatestGen[i], neighboring_vals, n_wts);

    //                if (test) nodes_to_test.Add(i);

    //            }

    //            if (SelectType)
    //            {
    //                int trial = rnd.Next(nodes_to_test.Count);
    //                int new_index = nodes_to_test[trial];
    //                nodes_to_test[0] = new_index;
    //                nodes_to_test.RemoveRange(1, nodes_to_test.Count - 1);
    //            }

    //            // evaluate list of cells
    //            for (int j = 0; j < nodes_to_test.Count; j++)
    //            {
    //                int i = nodes_to_test[j];
    //                int[] neighboring_indices = wrld.gph.NeighboringIndexesOf(i);

    //                // build list of neighboring values
    //                List<double> neighboring_vals = new List<double>();
    //                for (int k = 0; k < neighboring_indices.Length; k++) neighboring_vals.Add(wrld.LatestGen[neighboring_indices[k]]);

    //                double d = EvaluateCell(i, wrld.LatestGen[i], neighboring_vals, wrld.gph.NeighboringWeightsOf(i));
    //                //double d = g + i + 0.0;

    //                new_vals[i] = d;
    //            }
    //            wrld.AddGen(new_vals);

    //            // console out
    //            Grasshopper.Kernel.Data.GH_Path key_path = new Grasshopper.Kernel.Data.GH_Path(g);
    //            List<Grasshopper.Kernel.Types.GH_String> gh_strs = new List<Grasshopper.Kernel.Types.GH_String>();
    //            foreach (String str in this.m_py_output.Result) gh_strs.Add(new Grasshopper.Kernel.Types.GH_String(str));
    //            consoleOut.AppendRange(gh_strs, key_path);

    //        }

    //        DA.SetDataTree(0, consoleOut);
    //        DA.SetData(1, wrld);

    //    }

    //    private double EvaluateCell(int cell_index, double cur_val, List<double> n_vals, double[] n_wts)
    //    {
    //        var t = Type.GetType("IronPython.Runtime.List,IronPython");
    //        IList n_list = Activator.CreateInstance(t) as IList;
    //        foreach (double d in n_vals)
    //        {
    //            object cast = d;
    //            n_list.Add(cast);
    //        }
    //        _py.SetVariable("n_vals", n_list);
    //        _py.SetVariable("h_val", cur_val);
    //        _py.SetVariable("n_wts", n_wts);
    //        _py.SetVariable("h_idx", cell_index);

    //        try
    //        {
    //            _compiled_py.Execute(_py);
    //        }
    //        catch (Exception ex)
    //        {
    //            AddErrorNicely(m_py_output, ex);
    //        }
    //        object o = _py.GetVariable("h_val");
    //        return System.Convert.ToDouble(o);
    //    }

    //    private bool SelectCell(int cell_index, double cur_val, List<double> n_vals, double[] n_wts)
    //    {
    //        var t = Type.GetType("IronPython.Runtime.List,IronPython");
    //        IList n_list = Activator.CreateInstance(t) as IList;
    //        foreach (double d in n_vals)
    //        {
    //            object cast = d;
    //            n_list.Add(cast);
    //        }
    //        _spy.SetVariable("n_vals", n_list);
    //        _spy.SetVariable("h_val", cur_val);
    //        _spy.SetVariable("n_wts", n_wts);
    //        _spy.SetVariable("h_idx", cell_index);

    //        try
    //        {
    //            _compiled_spy.Execute(_spy);
    //        }
    //        catch (Exception ex)
    //        {
    //            AddErrorNicely(m_py_output, ex);
    //        }
    //        object o = _spy.GetVariable("h_val");
    //        return System.Convert.ToBoolean(o);
    //    }




    //    private void AddErrorNicely(StringList sw, Exception ex)
    //    {
    //        sw.Write(string.Format("Runtime error ({0}): {1}", ex.GetType().Name, ex.Message));

    //        string error = _py.GetStackTraceFromException(ex);

    //        error = error.Replace(", in <module>, \"<string>\"", ", in script");
    //        error = error.Trim();

    //        sw.Write(error);
    //    }
    //}


    /// <summary>
    /// Used to capture the output stream from an executing python script
    /// </summary>
    class StringList : System.IO.TextWriter
    {
        private readonly List<string> _txts = new List<string>();

        public override void Write(string s) {
            _txts.Add(s);
        }

        public void Reset() {
            _txts.Clear();
        }

        public IList<string> Result {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<string>(_txts); }
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
        public override string ToString() {
            var sb = new StringBuilder();
            foreach (string s in _txts)
                sb.AppendLine(s);
            return sb.ToString();
        }
    }

}
