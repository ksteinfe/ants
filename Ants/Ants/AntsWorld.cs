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
        protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_Param_AntWorld; } }
        

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

