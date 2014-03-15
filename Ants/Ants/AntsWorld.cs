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
        public int edgeCount;

        public SpatialGraph()
        {
            this.nodes = new List<Point3d>();
            this.edges = new Dictionary<int, List<int>>();
            this.weights = new Dictionary<Tuple<int, int>, double>();
            this.edgeCount = 0;
        }

        public void AddEdge(Point3d fromPt, Point3d toPt, double weight=1.0){
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
            
            // ensure that edge does not already exist, and add edge if not
            if (!this.edges[fromIdx].Contains(toIdx) && !this.edges[toIdx].Contains(fromIdx)) this.edgeCount++;
            if (!this.edges[fromIdx].Contains(toIdx)) this.edges[fromIdx].Add(toIdx);
            if (!this.edges[toIdx].Contains(fromIdx)) this.edges[toIdx].Add(fromIdx);

            this.weights[new Tuple<int, int>(fromIdx, toIdx)] = weight;
            this.weights[new Tuple<int, int>(toIdx, fromIdx)] = weight;
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

    }


    public class AWorld : GH_Goo<object>
    {
        public SpatialGraph gph;
        // a list of arrays of doubles. 
        // since we don't know how many timesteps we'll create, we use a List for outer container.
        // but, since we do know how many nodes we have in each timestep, we can use a fixed-size array for the inner container
        public List<double []> gens; 

        public AWorld(SpatialGraph initalGraph) : base() {
            this.gph = initalGraph;
            this.gens = new List<double[]>();
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
            this.gens.Add(vals);
        }

        #region // REQUIRED GH STUFF

        public AWorld(AWorld instance)
        {
            this.gph = instance.gph;
        }
        public override IGH_Goo Duplicate() { return new AWorld(this); }

        

        public override bool IsValid
        {
            get
            {
                return true;
                //if (this.type != MaskType.Invalid) { return true; }
                //return false;
            }
        }
        public override object ScriptVariable() { return new AWorld(this); }
        public override string ToString()
        {
            return String.Format("I am an Ants World.\n I have {0} nodes in my graph, {1} connections, and {1} generations of history. What else would you like to know?", this.gph.nodes.Count, this.gph.edgeCount, this.gens.Count);
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


        #endregion

    }

    public class GHParam_AWorld : GH_Param<AWorld>
    {
        public GHParam_AWorld()
            : base(new GH_InstanceDescription("Ants World", "AWorld", "Stores a graph of nodes and connections, and a history of values for each node for a given number of timesteps", "Ants", "Worlds"))
        { }
        public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
        public override System.Guid ComponentGuid { get { return new Guid("{4657FAD6-4F6E-4FCA-8CAC-9B32669B5451}"); } }
        //protected override Bitmap Icon { get { return DYear.Properties.Resources.Icons_Param_YearMask; } }
    }

}

