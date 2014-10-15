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
        // a list of arrays of objects. 
        // since we don't know how many timesteps we'll create, we use a List for outer container.
        // but, since we do know how many nodes we have in each timestep, we can use a fixed-size array for the inner container
        //public List<object []> gens; **
        public List<AntFood> gens;
        public object[] igen;
        public bool initialized;

        public AWorld()
            : base() {
                this.initialized = false;
        }

        public AWorld(SpatialGraph initialGraph, List<object> initialValues) : base() {
            this.gph = new SpatialGraph(initialGraph);
            /// this.igen = initialValues;
            //this.ClearGens();
            //this.gens = new List<object[]>(); **

            this.gens = new List<AntFood>();
            AntFood new_gen = new AntFood();
            new_gen.values = initialValues;
            this.gens.Add(new_gen);

            //this.gens.Add(initialValues); **
            ///
            this.initialized = true;
        }

        public AntFood LatestGen
        {
            // use this method to grab out the latest generation of values.
            // there should always be the same number of values stored here as there are nodes in this.gph
            
            get { return this.gens[gens.Count - 1]; }
        }

        public void AddGen(List<object> vals)
        {
            //TODO: figure out how to use this method to successively add generations to a world from outside this class
            // TODO: ensure that there are the same number of values stored in the appended list as there are nodes in this.gph
            AntFood new_gen = new AntFood();

            //object[] ret = new object[vals.Length];
            for (int i = 0; i < vals.Count; i++) new_gen.values.Add(vals[i]);
            this.gens.Add(new_gen);
        }

        public void ClearGens() {
            this.gens = new List<AntFood>();
            //this.gens.Add(this.igen);
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
            //this.gens = new List<object[]>(); **
            this.gens = new List<AntFood>();
            foreach (AntFood gen in instance.gens) {
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
                for (int i = 0; i < gens.Count; i++) if (gens[i].Val_Count != gph.nodes.Count) return false;
                return true;
            }
        }
        public override object ScriptVariable() { return new AWorld(this); }
        public override string ToString()
        {
            return String.Format("I am an Ants World.\n I have {0} nodes in my graph, {1} connections, and {2} generations of history. What else would you like to know?", this.gph.nodes.Count, this.gph.EdgeCount, this.GenCount);
        }
        public override string TypeDescription { get { return "Represents an Ant World"; } }
        public override string TypeName { get { return "AntWorld"; } }

        /// <summary>
        ///  Generic Conversions
        /// </summary>
        /// <param name="obj_in"></param>
        /// <param name="obj_out"></param>

        public static List<object> Convert_List(List<GH_ObjectWrapper> raw_in)
        {
            To_Something Converter = null;
            List<object> obj_in = new List<object>();
            List<object> obj_out = new List<object>();

            //if (obj_in[0].GetType() == typeof(GH_ObjectWrapper))
            for (int i = 0; i < raw_in.Count; i++)
            {
                obj_in.Add(raw_in[i].Value);
            }


            // Create conversion dictionary

            Dictionary<Type, Ants.AWorld.To_Something> typeDict = new Dictionary<Type, Ants.AWorld.To_Something>
            {
                {typeof(GH_Number),To_Double},
                {typeof(GH_String),To_String},
                {typeof(GH_Point),To_Point},
                {typeof(GH_Boolean),To_Bool}
                // Add more type conversions here
            };

            // Try to read a converter from the dictionary
            // If there is none, catch the error and proceed
            // We'll let the user handle the data uncoverted in their Ironpython code

            try { Converter = typeDict[obj_in[0].GetType()]; }
            catch (SystemException) { }


            // Convert each object in list

            for (int i = 0; i < obj_in.Count; i++)
            {
                if (!(Converter == null))
                {
                    obj_out.Add(Converter(obj_in[i]));
                }
                else
                {
                    obj_out.Add(obj_in[i]);
                }
            }


            return obj_out;
        }

        public static object To_Double(object in_val)
        {
            double n = new double();
            var y = GH_Convert.ToDouble(in_val, out n, GH_Conversion.Both);
            return (object)n;
        }

        public static object To_String(object in_val)
        {
            string n = "";
            var y = GH_Convert.ToString(in_val, out n, GH_Conversion.Both);
            return (object)n;
        }

        public static object To_Point(object in_val)
        {
            Point3d n = new Point3d();
            var y = GH_Convert.ToPoint3d(in_val, ref n, GH_Conversion.Both);
            return (object)n;
        }

        public static object To_Bool(object in_val)
        {
            bool n = false;
            GH_Convert.ToBoolean(in_val, out n, GH_Conversion.Both);
            return (object)n;
        }

        public delegate object To_Something(object o);

        /// end of generic converter block


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

            // create a chunk for the generations
            GH_IO.Serialization.GH_IWriter ch_writer = writer.CreateChunk("gens");
            int j = 0;
            ch_writer.SetInt32("Count", this.gens.Count);
            foreach (AntFood gen in this.gens)
            {
                GH_IO.Serialization.GH_IWriter item_writer = ch_writer.CreateChunk("Item", j);

                if (!gen.Write(item_writer)) return false;

                j++;
            }

            // create a chunk for the graph
            GH_IO.Serialization.GH_IWriter gr_writer = writer.CreateChunk("graph");

            if (!this.gph.Write(gr_writer)) return false;

            return base.Write(writer);
        }


        // not used
        public static T ObjectDeserializer<T>(string XmlInput)
        {
            System.Xml.XmlDocument XmlDoc = new System.Xml.XmlDocument();
            XmlDoc.Load(new System.IO.StringReader(XmlInput));
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));
            T out_ob = (T)ser.Deserialize(new System.IO.StringReader(XmlInput));
            return out_ob;
        }
     

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.ClearGens();
            this.gph = new SpatialGraph();


            // read the generations chunk
            if (reader.ChunkExists("gens"))
            {
                GH_IO.Serialization.GH_IReader ch_reader = reader.FindChunk("gens");
                int num = ch_reader.GetInt32("Count") - 1;
                

                for (int i = 0; i <= num; i++)
                {
                    GH_IO.Serialization.GH_IReader reader2 = ch_reader.FindChunk("Item", i);
                    if (reader2 == null)
                    {
                        reader.AddMessage("Missing persistent data entry.", GH_IO.Serialization.GH_Message_Type.error);
                    }
                    else
                    {
                        AntFood data = new AntFood();
                        if (!data.Read(reader2)) return false;
                        this.AddGen(data.values);
                    }
                }
            }

            // read the graph chunk

            if (reader.ChunkExists("graph"))
            {
                GH_IO.Serialization.GH_IReader gr_reader = reader.FindChunk("graph");
                if (!this.gph.Read(gr_reader)) return false;
            }


            this.initialized = true;

            return base.Read(reader);
        }


        #endregion

    }

            // garbage here:

            //string igenstring = "";
            //string genstrings = "";
            //string type_name = "";

            //if (!reader.TryGetString("type", ref type_name)) return false;


            //if (!reader.TryGetString("gens", ref genstrings)) return false;
            //try
            //{

            //    object o = new object();
            //    string[] genstringsArr = genstrings.Split(';');
            //    if (genstringsArr.Length == 0) return false;
            //    this.gens = new List<object[]>();
            //    foreach (String genstring in genstringsArr){
            //        string[] genstringArr = genstring.Split(':');
            //        if (genstringArr.Length == 0) return false;
            //        object[] gen = new object[genstringArr.Length];
            //        for (int i = 0; i < genstringArr.Length; i++)
            //        {
            //            switch(type_name){
            //                case "Boolean":
            //                    o = (object)bool.Parse(genstringArr[i]);
            //                    break;
            //                case "Point3d":
            //                    string [] args = genstringArr[i].Split(',');
            //                    Point3d p = new Point3d(double.Parse(args[0]), double.Parse(args[1]), double.Parse(args[2]));
            //                    o = p as object;
            //                    break;
            //                case "Dictionary":
            //                    Dictionary<string, object> d = new Dictionary<string,object>();
            //                    string[] pairs = genstringArr[i].Split('/');
            //                    foreach (string pair in pairs) {
            //                        string [] kv = pair.Split('=');
            //                        d.Add(kv[0], ((object)double.Parse(kv[1])));
            //                    }
            //                    o = d as object;
            //                    break;
            //                default:
            //                    o = (object)double.Parse(genstringArr[i]);
            //                    break;
            //            }


                        //if (type_name == "Boolean")
                        //{
                        //    o = (object)bool.Parse(genstringArr[i]);
                        //}
                        //else if (type_name == "Point3d")
                        //{
                        //    string [] args = genstringArr[i].Split(',');
                        //    Point3d p = new Point3d(double.Parse(args[0]), double.Parse(args[1]), double.Parse(args[2]));
                        //    o = p as object;
                        //}

                        //else
                        //{
                        //    o = (object)double.Parse(genstringArr[i]);
                        //}

            //            gen[i] = o;

            //        }


            //        this.gens.Add(gen);
            //    }
            //}
            //catch
            //{
            //    return false;
            //}



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

