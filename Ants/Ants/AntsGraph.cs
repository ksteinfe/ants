using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Drawing;
using Rhino;
using Rhino.Runtime;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;

namespace Ants
{

    public class SpatialGraph : GH_Goo<object>, GH_IO.GH_ISerializable
    {
        public List<Point3d> nodes;
        public Dictionary<int, List<int>> edges;
        public Dictionary<Tuple<int, int>, double> weights;
        public bool initialized;
        //public int edgeCount;

        public SpatialGraph() : base() {
            this.nodes = new List<Point3d>();
            this.edges = new Dictionary<int, List<int>>();
            this.weights = new Dictionary<Tuple<int, int>, double>();
            this.initialized = false;
        }


        public int EdgeCount
        {
            get { return this.weights.Count; }
        }

        public bool AddEdge(Point3d fromPt, Point3d toPt, double weight = 1.0)
        {
            // Point3d should work okay as a dictionary key, but we may need to override Equals and GetHashCode in a child of Point3d class instead, as per http://stackoverflow.com/questions/3545237/c-sharp-object-as-dictionary-key-problem
            int fromIdx = -1;
            int toIdx = -1;
            if (this.nodes.Contains(fromPt))
            {
                fromIdx = nodes.IndexOf(fromPt);
            }
            else
            {
                this.nodes.Add(fromPt);
                fromIdx = nodes.Count - 1;
                this.edges.Add(fromIdx, new List<int>());
            }
            if (this.nodes.Contains(toPt))
            {
                toIdx = nodes.IndexOf(toPt);
            }
            else
            {
                this.nodes.Add(toPt);
                toIdx = nodes.Count - 1;
                this.edges.Add(toIdx, new List<int>());
            }

            if (fromIdx == toIdx) return false;
            if ((fromIdx < 0) || (toIdx < 0)) return false;

            //if (!this.edges[fromIdx].Contains(toIdx) && !this.edges[toIdx].Contains(fromIdx)) this.edgeCount++;
            if (!this.edges[fromIdx].Contains(toIdx))
            {
                this.edges[fromIdx].Add(toIdx);
                this.edges[fromIdx].Sort();
            }
            if (!this.edges[toIdx].Contains(fromIdx))
            {
                this.edges[toIdx].Add(fromIdx);
                this.edges[toIdx].Sort();
            }

            if (fromIdx < toIdx) this.weights[new Tuple<int, int>(fromIdx, toIdx)] = weight;
            else this.weights[new Tuple<int, int>(toIdx, fromIdx)] = weight;

            return true;
        }

        public Point3d[] NeighboringPointsOf(int nodeIdx)
        {
            if (!this.edges.ContainsKey(nodeIdx)) return new Point3d[0];

            List<int> idxs = this.edges[nodeIdx];
            Point3d[] ret = new Point3d[idxs.Count];
            for (int i = 0; i < idxs.Count; i++)
            {
                ret[i] = this.nodes[idxs[i]];
            }
            return ret;
        }

        public int[] NeighboringIndexesOf(int nodeIdx)
        {
            if (!this.edges.ContainsKey(nodeIdx)) return new int[0];
            return this.edges[nodeIdx].ToArray();
        }

        public double[] NeighboringWeightsOf(int nodeIdx)
        {
            int[] nidxs = NeighboringIndexesOf(nodeIdx);
            double[] ret = new double[nidxs.Length];
            for (int i = 0; i < nidxs.Length; i++)
            {
                ret[i] = this.weights[new Tuple<int, int>(nodeIdx, nidxs[i])];
            }
            return ret;
        }

        public List<Line> EdgesToLines()
        {
            List<Line> ret = new List<Line>();
            List<Tuple<int, int>> drawnEdges = new List<Tuple<int, int>>();

            foreach (KeyValuePair<int, List<int>> pair in this.edges)
            {
                int fromIdx = pair.Key;
                foreach (int toIdx in pair.Value)
                {
                    if (!drawnEdges.Contains(new Tuple<int, int>(fromIdx, toIdx)) && !drawnEdges.Contains(new Tuple<int, int>(toIdx, fromIdx)))
                    {
                        drawnEdges.Add(new Tuple<int, int>(fromIdx, toIdx));
                        ret.Add(new Line(this.nodes[fromIdx], this.nodes[toIdx]));
                    }
                }
            }
            return ret;

        }

        public static SpatialGraph GraphFromGrid(int mCount, int nCount, bool cnrs)
        {
            SpatialGraph gph = new SpatialGraph();
            int fromIdx = 0;

            // Create the nodes first so that their indices are orderly: 0,1,2,...
            for (int m = 0; m < mCount; m++)
                for (int n = 0; n < nCount; n++)
                {
                    gph.nodes.Add(new Point3d(n, m, 0));
                    fromIdx = gph.nodes.Count - 1;
                    gph.edges.Add(fromIdx, new List<int>());
                }

            // Now add edges.
            for (int m = 0; m < mCount; m++)
                for (int n = 0; n < nCount; n++)
                {
                    if (n > 0) gph.AddEdge(new Point3d(n, m, 0), new Point3d(n - 1, m, 0));
                    if (m > 0) gph.AddEdge(new Point3d(n, m, 0), new Point3d(n, m - 1, 0));
                    if (cnrs)
                    {
                        if ((n > 0) && (m > 0)) gph.AddEdge(new Point3d(n, m, 0), new Point3d(n - 1, m - 1, 0));
                        if ((n > 0) && (m < mCount - 1)) gph.AddEdge(new Point3d(n, m, 0), new Point3d(n - 1, m + 1, 0));
                    }
                }
            return gph;
        }


        public static SpatialGraph GraphFromPoints(List<Point3d> points_in, double dist)
        {
            SpatialGraph gph = new SpatialGraph();
            int pt_count = points_in.Count;
            

            // Create the nodes first so that their indices are orderly: 0,1,2,...
            for (int m = 0; m < pt_count; m++)
            {
                gph.nodes.Add(points_in[m]);
                gph.edges.Add(m, new List<int>());
            }
            for (int m = 0; m < pt_count-1; m++)
                for (int n = m+1; n < pt_count; n++)
                {
                    var vec = gph.nodes[m] - gph.nodes[n];
                    double length = vec.Length;
                    if (length < dist) gph.AddEdge(gph.nodes[m], gph.nodes[n], length);
                    
                }

            return gph;
        }

        #region // Required GH Stuff

        
        public SpatialGraph(SpatialGraph instance)
        {
            this.nodes = instance.nodes;
            this.edges = instance.edges;
            this.weights = instance.weights;

            this.initialized = true;
        }

        public override IGH_Goo Duplicate() { return new SpatialGraph(this); }

        

        public override bool IsValid
        {
            get
            {
                if (!this.initialized) return false;
                //for (int i = 0; i < gens.Count; i++) if (gens[i].Length != gph.nodes.Count) return false;
                return true;
            }
        }
        public override object ScriptVariable() { return new SpatialGraph(this); }
        public override string ToString()
        {
            return String.Format("I am a Spatial Graph.\n I have {0} nodes in my graph and {1} connections. What else would you like to know?", this.nodes.Count, this.EdgeCount);
        }
        public override string TypeDescription { get { return "Represents a Spatial Graph"; } }
        public override string TypeName { get { return "Spatial Graph"; } }

        // This function is called when Grasshopper needs to convert other data into SpatialGraph type.
        // We won't know what type of object the other thing is
        // We can try converting it to, say, a collection of lines, and running the algo for setting points and connections that way
        public override bool CastFrom(object source)
        {
            //Abort immediately on bogus data.
            if (source == null) { return false; }

            // here's an example of converting stuff into a string and trying to use that to do stuff
            //string str = null;
            //if (GH_Convert.ToString(source, out str, GH_Conversion.Both))
            //{
                //do stuff
                ///return true;
            //}

            return false;
        }


        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            /*
            public List<Point3d> nodes;
            public Dictionary<int, List<int>> edges;
            public Dictionary<Tuple<int, int>, double> weights;
            public int edgeCount;
             */
            List<String> nodestrings = new List<string>();
            foreach (Point3d pt in this.nodes) nodestrings.Add(pt.X.ToString() + "," + pt.Y.ToString() + "," + pt.Z.ToString());
            writer.SetString("nodes", string.Join(";", nodestrings));

            List<String> edgestrings = new List<string>();
            foreach (KeyValuePair<int, List<int>> entry in this.edges) edgestrings.Add(entry.Key.ToString() + ":" + string.Join(",", entry.Value));
            writer.SetString("edges", string.Join(";", edgestrings));

            List<String> weightstrings = new List<string>();
            foreach (KeyValuePair<Tuple<int, int>, double> entry in this.weights) weightstrings.Add(entry.Key.Item1 + "," + entry.Key.Item2 + ":" + entry.Value);
            writer.SetString("weights", string.Join(";", weightstrings));


            return true;
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {

            string nodestring = "";
            string edgestring = "";
            string weightstring = "";
            if (!reader.TryGetString("nodes", ref nodestring) || !reader.TryGetString("edges", ref edgestring) || !reader.TryGetString("weights", ref weightstring)) return false;
            try
            {
                string[] nodestringArr = nodestring.Split(';');
                this.nodes = new List<Point3d>();
                foreach (String ptstring in nodestringArr)
                {
                    string[] coords = ptstring.Split(',');
                    if (coords.Length != 3) return false;
                    this.nodes.Add(new Point3d(double.Parse(coords[0]), double.Parse(coords[1]), double.Parse(coords[2])));
                }

                string[] edgestringArr = edgestring.Split(';');
                this.edges = new Dictionary<int, List<int>>();
                foreach (String estring in edgestringArr)
                {
                    string[] keyvalstr = estring.Split(':');
                    int from = int.Parse(keyvalstr[0]);
                    List<int> to = new List<int>();
                    foreach (String tostr in keyvalstr[1].Split(',')) to.Add(int.Parse(tostr));
                    this.edges[from] = to;
                }

                string[] weightstringArr = weightstring.Split(';');
                this.weights = new Dictionary<Tuple<int, int>, double>();
                foreach (String wstring in weightstringArr)
                {
                    string[] keyvalstr = wstring.Split(':');
                    string[] keystr = keyvalstr[0].Split(',');
                    Tuple<int, int> key = new Tuple<int, int>(int.Parse(keystr[0]), int.Parse(keystr[1]));
                    double value = double.Parse(keyvalstr[1]);
                    this.weights[key] = value;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }


        #endregion
    }

    public class GHParam_SpatialGraph : GH_PersistentParam<SpatialGraph>
    {
        public GHParam_SpatialGraph()
            : base(new GH_InstanceDescription("Spatial Graph", "SG", "Stores a graph of nodes and connections", "Ants", "Graphs"))
        { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override System.Guid ComponentGuid { get { return new Guid("{7D6E6F46-F68E-4AE2-AF84-32A10E8D79F9}"); } }

        
        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_Param_SpatialGraph1; } }

        protected override GH_GetterResult Prompt_Singular(ref SpatialGraph value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<SpatialGraph> values)
        {
            return GH_GetterResult.cancel;
        }

    }

    class AntsGraph
    {
    }
}
