using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using Rhino;
using Rhino.Runtime;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;

namespace Ants {
    public class AWorldByGrid : GH_Component
    {

        public AWorldByGrid()
            //Call the base constructor
            : base("Ants Grid World", "GridWorld", "Creates an AntsWorld that looks like a regular grid", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{FDCC238D-44C3-4C14-8996-7C753518CF48}"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.Register_IntegerParam("Rows", "M", "Number of rows.", 3, GH_ParamAccess.item);
            pManager.Register_IntegerParam("Number of Columns", "N", "Number of Columns", 3, GH_ParamAccess.item);
            pManager.Register_DoubleParam("Value", "V", "Initial Values", GH_ParamAccess.list);
            pManager.Register_BooleanParam("Connect Corners", "C", "Connect up the neighbors at corners?", false, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int mCount = 0;
            int nCount = 0;
            List<double> v_list = new List<double>();            
            bool cnrs = false;
            
            if (!DA.GetData(0, ref mCount)) return;
            if (!DA.GetData(1, ref nCount)) return;
            if (!DA.GetDataList(2, v_list)) return;
            if (!DA.GetData(3, ref cnrs)) return;

            // TODO: the numbering of the nodes in the resulting graph don't conform to the ordering of a grid.
            // either make them do so, or find another mechanism for matching given list of values to resulting graph
            SpatialGraph gph = SpatialGraph.GraphFromGrid(mCount, nCount, cnrs);

            // Sets the initial Generation by using the input v_list
            // if it runs out of values, it starts over (wraps)
            double[] val_list = new double[gph.nodes.Count];
            int v_i = 0;
            for (int i = 0; i < gph.nodes.Count; i++) {
                if (v_i == v_list.Count) v_i = 0;
                val_list[i] = v_list[v_i];
                v_i++;
            }

            AWorld wrld = new AWorld(gph, val_list);

            DA.SetData(0, wrld);
        }


    }



    public class AWorldToLines : GH_Component
    {

        public AWorldToLines()
            //Call the base constructor
            : base("Ants Grid To Lines", "WorldToLines", "Convert the graph of an AntsWorld to a network of lines", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{6D8C293A-A7FF-4C6C-871A-478DAC246B59}"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_LineParam("Lines", "L", "The resulting network of lines", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AWorld wrld = new AWorld();
            if (!DA.GetData(0, ref wrld) || !wrld.IsValid) return;

            List<Line> lines = wrld.gph.EdgesToLines();

            List<GH_Line> ghLines = new List<GH_Line>();
            foreach (Line ln in lines) ghLines.Add(new GH_Line(ln));
            DA.SetDataList(0, ghLines);
        }

    }


    public class AWorldSelect : GH_Component
    {

        public AWorldSelect()
            //Call the base constructor
            : base("Ants World Selector", "AWorldSelect", "Select a specific Ant World generation", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{43D5097F-F320-4672-AB69-70A1C541F7AF}"); } }
        

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Generation", "G", "Generation to select.", 0, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_DoubleParam("List", "List", "List of extracted values", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AWorld refwrld = new AWorld();
            if (!DA.GetData(0, ref refwrld) || !refwrld.IsValid) return;
            AWorld wrld = new AWorld(refwrld);

            int g = 0;
            if (!DA.GetData(1, ref g)) return;
            if (g > wrld.GenCount - 1) g = wrld.GenCount - 1;
            DA.SetDataList(0, wrld.gens[g]);
        }

    }

}

