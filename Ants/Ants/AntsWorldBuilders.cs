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
            bool cnrs = false;
            if (!DA.GetData(0, ref mCount)) return;
            if (!DA.GetData(1, ref nCount)) return;
            if (!DA.GetData(2, ref cnrs)) return;

            SpatialGraph gph = SpatialGraph.GraphFromGrid(mCount, nCount, cnrs);

            AWorld wrld = new AWorld(gph);

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
            AWorld wrld = new AWorld(new SpatialGraph());
            if (!DA.GetData(0, ref wrld)) return;

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
            AWorld wrld = new AWorld(new SpatialGraph());
            int Gen = 0;
            int nGen = 0;

            if (!DA.GetData(0, ref wrld)) return;
            if (!DA.GetData(1, ref Gen)) return;

            nGen = wrld.gens.Count;

            if (Gen > (nGen - 1)) Gen = 0;

            double[] val_list = new double[wrld.gph.nodes.Count];

            val_list = wrld.gens[Gen];

            DA.SetDataList(0, val_list);
        }

    }

}

