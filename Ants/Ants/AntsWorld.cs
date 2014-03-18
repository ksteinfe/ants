using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using Rhino;
using Rhino.Runtime;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;

namespace Ants {

    public class SpatialGraph
    {
        public List<Point3d> nodes;
        public Dictionary<int, List<int>> edges;
        public Dictionary<Tuple<int, int>, double> weights;
        //public int edgeCount;

        public SpatialGraph()
        {
            this.nodes = new List<Point3d>();
            this.edges = new Dictionary<int, List<int>>();
            this.weights = new Dictionary<Tuple<int, int>, double>();
        }
        public SpatialGraph(SpatialGraph instance) {
            this.nodes = new List<Point3d>(instance.nodes);
            this.edges = new Dictionary<int, List<int>>(instance.edges);
            this.weights = new Dictionary<Tuple<int, int>, double>(instance.weights);
        }

        public int EdgeCount
        {
            get { return this.weights.Count; }
        }

        public bool AddEdge(Point3d fromPt, Point3d toPt, double weight=1.0){
            // Point3d should work okay as a dictionary key, but we may need to override Equals and GetHashCode in a child of Point3d class instead, as per http://stackoverflow.com/questions/3545237/c-sharp-object-as-dictionary-key-problem
            int fromIdx = -1;
            int toIdx = -1;
            if (this.nodes.Contains(fromPt)){
                fromIdx = nodes.IndexOf(fromPt);
            }else{
                this.nodes.Add(fromPt);
                fromIdx = nodes.Count -1;
                this.edges.Add(fromIdx, new List<int>());
            }
            if (this.nodes.Contains(toPt)){
                toIdx = nodes.IndexOf(toPt);
            }else{
                this.nodes.Add(toPt);
                toIdx = nodes.Count -1;
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
                ret[i] = this.weights[new Tuple<int, int>(nodeIdx,nidxs[i])];
            }
            return ret;
        }

        public List<Line> EdgesToLines(){
            List<Line> ret = new List<Line>();
            List<Tuple<int, int>> drawnEdges = new List<Tuple<int, int>>();

            foreach (KeyValuePair<int, List<int>> pair in this.edges)
            {
                int fromIdx = pair.Key;
                foreach (int toIdx in pair.Value){
                    if (!drawnEdges.Contains(new Tuple<int, int>(fromIdx, toIdx)) && !drawnEdges.Contains(new Tuple<int, int>(toIdx, fromIdx)))
                    {
                        drawnEdges.Add(new Tuple<int, int>(fromIdx, toIdx));
                        ret.Add(new Line(this.nodes[fromIdx],this.nodes[toIdx]));
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


        #region // SERIALIZATION
        public bool Write(GH_IO.Serialization.GH_IWriter writer)
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

        public bool Read(GH_IO.Serialization.GH_IReader reader)
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
                    this.nodes.Add(new Point3d(double.Parse(coords[0]),double.Parse(coords[1]),double.Parse(coords[2])));
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


    public class AWorld : GH_Goo<object>, GH_IO.GH_ISerializable
    {
        public SpatialGraph gph;
        // a list of arrays of doubles. 
        // since we don't know how many timesteps we'll create, we use a List for outer container.
        // but, since we do know how many nodes we have in each timestep, we can use a fixed-size array for the inner container
        public List<double []> gens;
        public double[] igen;
        public bool initialized;

        public AWorld()
            : base() {
                this.initialized = false;
        }
        public AWorld(SpatialGraph initalGraph, double[] initalValues) : base() {
            this.gph = new SpatialGraph(initalGraph);
            this.igen = initalValues;
            this.ClearGens();
            this.initialized = true;
        }

        public double[] LatestGen
        {
            // use this method to grab out the latest generation of values.
            // there should always be the same number of values stored here as there are nodes in this.gph
            get { return this.gens[gens.Count - 1]; }
        }

        public void AddGen(double[] vals)
        {
            //TODO: figure out how to use this method to successively add generations to a world from outside this class
            // TODO: ensure that there are the same number of values stored in the appended list as there are nodes in this.gph
            double[] ret = new double[vals.Length];
            for (int i = 0; i < vals.Length; i++) ret[i] = vals[i];
            this.gens.Add(ret);
        }

        public void ClearGens() {
            this.gens = new List<double[]>();
            this.gens.Add(this.igen);
        }

        public int NodeCount {
            get { return this.gph.nodes.Count; }
        }

        public int GenCount {
            get { return this.gens.Count; }
        }

        #region // REQUIRED GH STUFF

        public AWorld(AWorld instance)
        {

            this.gph = new SpatialGraph(instance.gph);
            this.igen = instance.igen;
            this.gens = new List<double[]>();
            foreach (double[] gen in instance.gens) {
                this.gens.Add(gen);
            }
            this.initialized = true;
        }
        public override IGH_Goo Duplicate() { return new AWorld(this); }

        

        public override bool IsValid
        {
            get
            {
                if (!this.initialized) return false;
                for (int i = 0; i < gens.Count; i++) if (gens[i].Length != gph.nodes.Count) return false;
                return true;
            }
        }
        public override object ScriptVariable() { return new AWorld(this); }
        public override string ToString()
        {
            return String.Format("I am an Ants World.\n I have {0} nodes in my graph, {1} connections, and {2} generations of history. What else would you like to know?", this.gph.nodes.Count, this.gph.EdgeCount, this.gens.Count);
        }
        public override string TypeDescription { get { return "Represents an Ants Graph"; } }
        public override string TypeName { get { return "Ants Graph"; } }

        // This function is called when Grasshopper needs to convert other data into AntsGraph type.
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
            public SpatialGraph gph;
            public List<double []> gens;
            public double[] igen;
            public bool initialized;
             */
            //writer.SetString("GenCount", this.GenCount.ToString());

            List<String> genstrings = new List<string>();
            foreach (double[] gen in gens) genstrings.Add(string.Join(",", gen));
            writer.SetString("gens", string.Join(";", genstrings));
            
            writer.SetString("igen", string.Join(",", this.igen));

            if (!this.gph.Write(writer)) return false;

            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {

            string igenstring = "";
            string genstrings = "";
            if (!reader.TryGetString("igen", ref igenstring) || !reader.TryGetString("gens", ref genstrings)) return false;
            try
            {
                string[] igenstringArr = igenstring.Split(',');
                if (igenstringArr.Length == 0) return false;
                this.igen = new double[igenstringArr.Length];
                for (int i = 0; i < igenstringArr.Length; i++) igen[i] = (double.Parse(igenstringArr[i]));

                string[] genstringsArr = genstrings.Split(';');
                if (genstringsArr.Length == 0) return false;
                this.gens = new List<double[]>();
                foreach (String genstring in genstringsArr){
                    string[] genstringArr = genstring.Split(',');
                    if (genstringArr.Length == 0) return false;
                    double[] gen = new double[genstringArr.Length];
                    for (int i = 0; i < genstringArr.Length; i++) gen[i] = (double.Parse(genstringArr[i]));
                    this.gens.Add(gen);
                }
            }
            catch
            {
                return false;
            }

            this.gph = new SpatialGraph();
            if (!this.gph.Read(reader)) return false;

            this.initialized = true;
            return base.Read(reader);
        }








        #endregion

    }

    public class GHParam_AWorld : GH_PersistentParam<AWorld>
    {
        public GHParam_AWorld()
            : base(new GH_InstanceDescription("Ants World", "AWorld", "Stores a graph of nodes and connections, and a history of values for each node for a given number of timesteps", "Ants", "Worlds"))
        { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override System.Guid ComponentGuid { get { return new Guid("{4657FAD6-4F6E-4FCA-8CAC-9B32669B5451}"); } }
        //protected override Bitmap Icon { get { return DYear.Properties.Resources.Icons_Param_YearMask; } }

        protected override GH_GetterResult Prompt_Singular(ref AWorld value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<AWorld> values)
        {
            return GH_GetterResult.cancel;
        }

    }

}

