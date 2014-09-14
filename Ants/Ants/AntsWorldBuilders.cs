using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Drawing;
using Rhino;
using Rhino.Runtime;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;

namespace Ants {
    public class GraphByGrid : GH_Component
    {

        public GraphByGrid()
            //Call the base constructor
            : base("Create Graph of a Grid", "GrGph", "Creates a Spatial Graph that looks like a regular grid", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{FDCC238D-44C3-4C14-8996-7C753518CF48}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_grid; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.Register_IntegerParam("Rows", "M", "Number of rows.", 3, GH_ParamAccess.item);
            pManager.Register_IntegerParam("Number of Columns", "N", "Number of Columns", 3, GH_ParamAccess.item);
            //pManager.Register_DoubleParam("Value", "V", "Initial Values", GH_ParamAccess.list);
            pManager.Register_BooleanParam("Connect Corners", "C", "Connect up the neighbors at corners?", false, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
            //pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The resulting AntsWorld.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int mCount = 0;
            int nCount = 0;
            //List<double> v_list = new List<double>();            
            bool cnrs = false;
            
            if (!DA.GetData(0, ref mCount)) return;
            if (!DA.GetData(1, ref nCount)) return;
            //if (!DA.GetDataList(2, v_list)) return;
            if (!DA.GetData(2, ref cnrs)) return;

            // TODO: the numbering of the nodes in the resulting graph don't conform to the ordering of a grid.
            // either make them do so, or find another mechanism for matching given list of values to resulting graph
            SpatialGraph gph = SpatialGraph.GraphFromGrid(mCount, nCount, cnrs);

            // Sets the initial Generation by using the input v_list
            // if it runs out of values, it starts over (wraps)
            //double[] val_list = new double[gph.nodes.Count];
            //int v_i = 0;
            //for (int i = 0; i < gph.nodes.Count; i++) {
            //    if (v_i == v_list.Count) v_i = 0;
            //    val_list[i] = v_list[v_i];
            //    v_i++;
            //}

            //AWorld wrld = new AWorld(gph, val_list);


            DA.SetData(0, gph);
            // DA.SetData(0, wrld);

        }


    }

    public class GraphByPoints : GH_Component
    {

        public GraphByPoints()
            //Call the base constructor
            : base("Create Graph from Points", "PtsGph", "Creates a Spatial Graph from a set of points and a distance.", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{E8892815-CEFA-4F72-9BC1-3E520BF1ED67}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_points; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.Register_PointParam("Points", "P", "Points.", GH_ParamAccess.list); 
            pManager.Register_DoubleParam("Dist", "D", "Distance.", 1.0, GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double dist = 1.0;
            List<Point3d> points_in = new List<Point3d>();        

            if (!DA.GetDataList(0, points_in)) return;
            if (!DA.GetData(1, ref dist)) return;

            SpatialGraph gph = SpatialGraph.GraphFromPoints(points_in, dist);

            DA.SetData(0, gph);

        }


    }

    public class GraphByLines : GH_Component
    {

        public GraphByLines()
            //Call the base constructor
            : base("Create Graph from Lines", "LnsGph", "Creates a Spatial Graph from a set of lines.", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{4B2CA66B-20BB-49E4-9420-0BF618F770E2}"); } }

        //protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_points; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.Register_LineParam("Lines", "L", "Lines.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines_in = new List<Line>();

            if (!DA.GetDataList(0, lines_in)) return;

            SpatialGraph gph = SpatialGraph.GraphFromLines(lines_in);

            DA.SetData(0, gph);

        }


    }

    public class GraphByShapes : GH_Component
    {

        public GraphByShapes()
            //Call the base constructor
            : base("Create Graph from Shapes", "CrvShp", "Creates a Spatial Graph from a set of Shapes (Curves).", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{2740EF67-789D-44CE-B104-A78DD1316F19}"); } }

        //protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_points; } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.Register_CurveParam("Shapes", "S", "Shapes.", GH_ParamAccess.list);
            pManager.Register_BooleanParam("Connect Corners", "C", "Connect up the neighbors at corners?", false, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves_in = new List<Curve>();
            bool cnrs = true;

            if (!DA.GetDataList(0, curves_in)) return;
            if (!DA.GetData(1, ref cnrs)) return;

            SpatialGraph gph = SpatialGraph.GraphFromCurves(curves_in, cnrs);

            DA.SetData(0, gph);

        }


    }

    public class GraphToEdges : GH_Component
    {

        public GraphToEdges()
            //Call the base constructor
            : base("Graph to Edges", "GphLn", "Converts a Graph object to a network of lines", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{6D8C293A-A7FF-4C6C-871A-478DAC246B59}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_to_lines; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph to convert.", GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_LineParam("Edges", "E", "The resulting network of lines", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SpatialGraph gph = new SpatialGraph();
            //if (!DA.GetData(0, ref gph) || !gph.IsValid) return;
            if (!DA.GetData(0, ref gph)) return;

            List<Line> lines = gph.EdgesToLines();

            List<GH_Line> ghLines = new List<GH_Line>();
            foreach (Line ln in lines) ghLines.Add(new GH_Line(ln));
            DA.SetDataList(0, ghLines);
        }

    }

    public class GraphToNodes : GH_Component {

        public GraphToNodes()
            //Call the base constructor
            : base("Graph to Nodes", "GphPts", "Returns a Spatial Graph as a collection of Nodes.", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{CB16E7BA-6327-4A70-B6CD-76ED776861F6}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_to_points; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph to convert.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.Register_PointParam("Points", "P", "Node positions.", GH_ParamAccess.list);
            //pManager.Register_DoubleParam("Values", "V", "A tree of values, each branch corresponds with a single generation of node values.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            // Since this component does not operate on the AWorld, we can grab it this way instead of creating a copy
            //AWorld wrld = new AWorld();
            //if (!DA.GetData(0, ref wrld) || !wrld.IsValid) return;
            SpatialGraph gph = new SpatialGraph();
            if (!DA.GetData(0, ref gph)) return;

            List<Line> lines = gph.EdgesToLines();

            List<GH_Point> ghPoints = new List<GH_Point>();
            foreach (Point3d pt in gph.nodes) ghPoints.Add(new GH_Point(pt));
            DA.SetDataList(0, ghPoints);

            // Put this in another component
            //Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number> valueTreeOut = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number>();
            //for (int g = 0; g < wrld.GenCount; g++){
            //    Grasshopper.Kernel.Data.GH_Path key_path = new Grasshopper.Kernel.Data.GH_Path(g);
            //    List<Grasshopper.Kernel.Types.GH_Number> gh_vals = new List<Grasshopper.Kernel.Types.GH_Number>();
            //    foreach (double d in wrld.gens[g]) gh_vals.Add(new Grasshopper.Kernel.Types.GH_Number(d));
            //    valueTreeOut.AppendRange(gh_vals, key_path);
            //}
            //DA.SetDataTree(1, valueTreeOut);

        }

    }

    public class GraphExplorer : GH_Component
    {

        public GraphExplorer()
            //Call the base constructor
            : base("Graph Explorer", "GphExp", "Explore a Graph.", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{EFD017FD-8EB6-4074-AE5B-82B7C6D9E523}"); } }

        //protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_to_points; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph to convert.", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Index of Node", "I", "Index of Node to Explore.", 0, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PointParam("Points", "P", "Node position.", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Neighbors", "N", "Neighbor Indices.", GH_ParamAccess.list);
            pManager.Register_LineParam("Edges", "E", "Neighboring Edges", GH_ParamAccess.list);
            pManager.Register_DoubleParam("Weights", "W", "Edge Weights.", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SpatialGraph gph = new SpatialGraph();
            int indx = 0;

            if (!DA.GetData(0, ref gph)) return;
            if (!DA.GetData(1, ref indx)) return;

            if (indx > gph.nodes.Count - 1) indx = 0;

            GH_Point ghPoint = new GH_Point(gph.nodes[indx]);
            int[] neighbors = gph.NeighboringIndexesOf(indx);
            double[] weights = gph.NeighboringWeightsOf(indx);

            List<Line> lines = gph.EdgesToLines();
            List<GH_Line> ghLines = new List<GH_Line>();

            foreach (int neighbor in neighbors) ghLines.Add(new GH_Line(new Line(gph.nodes[indx], gph.nodes[neighbor])));


            DA.SetData(0, ghPoint);
            DA.SetDataList(1, neighbors);
            DA.SetDataList(2, ghLines);
            DA.SetDataList(3, weights);


        }

    }

    public class GraphChange : GH_Component
    {

        public GraphChange()
            //Call the base constructor
            : base("Change Nodes of a Graph.", "ChPts", "Replaces nodes in a Spatial Graph.", "Ants", "Graphs") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{D99A038D-418E-4530-A1B1-62AA627B298A}"); } }

        //protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_points; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "Spatial Graph", "S", "The Spatial Graph to change.", GH_ParamAccess.item);
            pManager.Register_PointParam("Points", "P", "Points.", GH_ParamAccess.list);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> points_in = new List<Point3d>();
            SpatialGraph gph = new SpatialGraph();

            if (!DA.GetData(0, ref gph)) return;
            if (!DA.GetDataList(1, points_in)) return;

            bool test = gph.ChangePts(points_in);

            DA.SetData(0, gph);

        }


    }


    public class AWorldGenVals : GH_Component
    {

        public AWorldGenVals()
            //Call the base constructor
            : base("Get Values", "GenVals", "Returns values for a given AntsWorld generation", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{43D5097F-F320-4672-AB69-70A1C541F7AF}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_ant_select; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
            pManager.Register_IntegerParam("Generation", "G", "Generation to select.", 0, GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Values", "V", "List of extracted values", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AWorld refwrld = new AWorld();
            List<GH_ObjectWrapper> output = new List<GH_ObjectWrapper>();
            if (!DA.GetData(0, ref refwrld) || !refwrld.IsValid) return;
            AWorld wrld = new AWorld(refwrld);

            int g = 0;
            if (!DA.GetData(1, ref g)) return;
            if (g > wrld.GenCount ) g = wrld.GenCount - 1;

            for (int i = 0; i < wrld.gens[g].Length; i++)
            {

                GH_ObjectWrapper temp = new GH_ObjectWrapper(wrld.gens[g][i]);

                output.Add(temp);

            }


            DA.SetDataList(0, output);
        }

    }

    public class AWorldToGraph : GH_Component
    {

        public AWorldToGraph()
            //Call the base constructor
            : base("Antworld to Graph", "AWGph", "Reads a Spatial Graph from an Antworld", "Ants", "Worlds") { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override Guid ComponentGuid { get { return new Guid("{3FE80255-6BF1-45B1-A966-752F59DF7478}"); } }

        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_ants_to_graph; } }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_AWorld(), "AWorld", "W", "The AntsWorld to convert.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new GHParam_SpatialGraph(), "SGraph", "S", "The resulting Spatial Graph.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AWorld refwrld = new AWorld();
            if (!DA.GetData(0, ref refwrld) || !refwrld.IsValid) return;
            AWorld wrld = new AWorld(refwrld);

            DA.SetData(0, wrld.gph);

        }

    }

}

