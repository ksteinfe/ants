using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Drawing;
using Rhino;
using Rhino.Runtime;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;
using GH_IO;


namespace Ants
{
    public class AntFood : GH_Goo<object>, GH_IO.GH_ISerializable
    {
        public List<object> values;
        public bool initialized;

        public AntFood() : base() {
            this.values = new List<object>();
            this.initialized = false;
        }

        public int Val_Count
        {
            get { return this.values.Count; }
        }

        public string Val_Type
        {
            get { return this.values[0].GetType().ToString(); }
        }


     #region // Required GH Stuff

        public AntFood(AntFood instance)
        {
            this.values = instance.values;
            this.initialized = true;
        }

        public override IGH_Goo Duplicate() { return new AntFood(this); }

        public override bool IsValid
        {
            get
            {
                if (!this.initialized) return false;
                //for (int i = 0; i < gens.Count; i++) if (gens[i].Length != gph.nodes.Count) return false;
                return true;
            }
        }
        public override object ScriptVariable() { return new AntFood(this); }
        public override string ToString()
        {
            return String.Format("I am AntFood.\n I have {0} vales in my graph of type {1}. What else would you like to know?", this.Val_Count, this.Val_Type);
        }
        public override string TypeDescription { get { return "Represents a list of values for Ants."; } }
        public override string TypeName { get { return "AntFood"; } }

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
            create a writer
             */

            GH_IO.Serialization.GH_IWriter ch_writer = writer.CreateChunk("values");
            int j = 0;
            ch_writer.SetInt32("Count", this.Val_Count);
            foreach (object val in this.values)
            {
                GH_IO.Serialization.GH_IWriter item_writer = ch_writer.CreateChunk("Item", j);

                IGH_Goo ig = GH_Convert.ToGoo(val);
                //bool tf = ig.Write(val_writer);


               // Type o_type = gen[i].GetType();

               // System.Reflection.MethodInfo WriteMethod = o_type.GetMethod("Write");
               //object w_o = WriteMethod.Invoke(gen[i], new object[] { val_writer });

                //GH_ObjectWrapper gh_o = new GH_ObjectWrapper(val);

                //Type o_type = TP.GetType();

                //System.Reflection.MethodInfo WriteMethod = o_type.GetMethod("Write");
                //object w_o = WriteMethod.Invoke(TP, new object[] { item_writer});

                //bool tf = gh_o.Value.Write(item_writer);

                if (!ig.Write(item_writer)) return false;


                j++;
            }


            return true;
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            AntFood ant = new AntFood();
            List<object> vals = new List<object>();

            if (reader.ChunkExists("values"))
            {
                GH_IO.Serialization.GH_IReader ch_reader = reader.FindChunk("values");
                //int num = reader.FindItem("Count").Read;
                int num = ch_reader.GetInt32("Count");
                int num3 = num - 1;
                bool flag = true;
                bool tf = false;
                for (int i = 0; i <= num3; i++)
                {
                    GH_IO.Serialization.GH_IReader reader2 = ch_reader.FindChunk("Item", i);
                    if (reader2 == null)
                    {
                        reader.AddMessage("Missing persistent data entry.", GH_IO.Serialization.GH_Message_Type.error);
                    }
                    else
                    {
                        //T data = this.InstantiateT();
                        GH_ObjectWrapper data = new GH_ObjectWrapper();
                        foreach (GH_IO.Types.GH_Item item in reader2.Items)
                        {
                            var n = item.Name;
                            var t = item.Type;
                            tf = data.Read(reader2);

                            if (tf)
                            {
                                var o = data.Value;
                                vals.Add(o);

                                //var y = GH_Convert.ToPoint3d_Primary(o, ref pt);

                                //this.nodes.Add(pt);

                            }
                            else
                            {
                                flag = false;
                                reader.AddMessage("Persistent data deserialization failed.", GH_IO.Serialization.GH_Message_Type.error);
                            }

                            //data = RuntimeHelpers.GetObjectValue(Activator.CreateInstance(t));

                        }

                    }
                }

                this.values = vals;
            }

            this.initialized = true;

            return base.Read(reader);

        }


        #endregion

    }


    //public class GHParam_AntFood : GH_PersistentParam<AntFood>
    //{
    //    public GHParam_AntFood()
    //        : base(new GH_InstanceDescription("AntFood", "AF", "Stores a list of values for Ants", "Ants", "Worlds"))
    //    { }
    //    public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
    //    public override System.Guid ComponentGuid { get { return new Guid("{36951A74-B21D-4E4F-85FF-A40EADA34318}"); } }

        
    //    protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_Param_SpatialGraph1; } }

    //    protected override GH_GetterResult Prompt_Singular(ref AntFood value)
    //    {
    //        return GH_GetterResult.cancel;
    //    }
    //    protected override GH_GetterResult Prompt_Plural(ref List<AntFood> values)
    //    {
    //        return GH_GetterResult.cancel;
    //    }

    //}


    //public class MakeFood : GH_Component
    //{

    //    public MakeFood()
    //        //Call the base constructor
    //        : base("Make Ant Food", "MkAF", "Creates an AntFood object from a list.", "Ants", "Worlds") { }
    //    public override Grasshopper.Kernel.GH_Exposure Exposure { get { return GH_Exposure.primary; } }
    //    public override Guid ComponentGuid { get { return new Guid("{34701606-F023-4CAB-A4B4-A060791E3C8D}"); } }

    //    //protected override Bitmap Icon { get { return Ants.Properties.Resources.Ants_Icons_graph_by_grid; } }


    //    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    //    {
    //        pManager.Register_GenericParam("Value", "V", "Initial Values", GH_ParamAccess.list);
    //    }

    //    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    //    {
    //        pManager.RegisterParam(new GHParam_AntFood(), "AntFood", "A", "The resulting AntFood.", GH_ParamAccess.item);
    //    }

    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {
    //        AntFood af = new AntFood();

    //        List<object> v_list = new List<object>();

    //        if (!DA.GetDataList(0, v_list)) return;

    //        af.values = v_list;

    //        DA.SetData(0, af);

    //    }


    //}



}