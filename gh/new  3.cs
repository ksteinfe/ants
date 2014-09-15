using System;
using System.Drawing;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

public class Param_TwoTypes : GH_Param<IGH_Goo>
{
  public Param_TwoTypes()
    : base("Colours & Curves", "C&C", "Contains a collection of colours and/or curves", "Custom", "Params", GH_ParamAccess.item)
  { }

  #region properties
  public override GH_Exposure Exposure
  {
    get
    {
      return GH_Exposure.primary;
    }
  }
  protected override Bitmap Icon
  {
    get
    {
      Bitmap icon = new Bitmap(24, 24, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      Graphics g = Graphics.FromImage(icon);
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
      g.Clear(Color.Transparent);

      g.FillEllipse(Brushes.HotPink, 4, 4, 16, 16);
      g.FillEllipse(Brushes.White, 7, 7, 5, 5);
      g.DrawEllipse(Pens.Maroon, 3, 3, 18, 18);
      g.DrawEllipse(Pens.Maroon, 4, 4, 16, 16);
      g.Dispose();
      return icon;
    }
  }
  public override Guid ComponentGuid
  {
    get { return new Guid("{DAF87443-CB0E-40B4-A0DD-1854364547DD}"); }
  }

  public override string TypeName
  {
    get
    {
      return "Colours & Curves";
    }
  }
  #endregion

  #region casting
  /// <summary>
  /// Since IGH_Goo is an interface rather than a class, we HAVE to override this method. 
  /// For IGH_Goo parameters it's usually good to return a blank GH_ObjectWrapper.
  /// </summary>
  protected override IGH_Goo InstantiateT()
  {
    return new GH_ObjectWrapper();
  }

  /// <summary>
  /// Since our parameter is of type IGH_Goo, it will accept ALL data. 
  /// We need to remove everything now that is not, GH_Colour, GH_Curve or null.
  /// </summary>
  protected override void OnVolatileDataCollected()
  {
    for (int p = 0; p < m_data.PathCount; p++)
    {
      List<IGH_Goo> branch = m_data.Branches[p];
      for (int i = 0; i < branch.Count; i++)
      {
        IGH_Goo goo = branch[i];

        //We accept existing nulls.
        if (goo == null) continue;

        //We accept colours.
        if (goo is GH_Colour) continue;

        //We accept curves.
        if (goo is GH_Curve) continue;

        //At this point the data is something other than a colour or a curve,
        //to be nice to the user, let's try and convert the data into a curve, then into a colour.

        GH_Curve castCurve = null;
        if (GH_Convert.ToGHCurve(goo, GH_Conversion.Both, ref castCurve))
        {
          //Yay, the data could be converted. Put the new curve back into our volatile data.
          branch[i] = castCurve;
          continue;
        }

        GH_Colour castColour = null;
        if (GH_Convert.ToGHColour(goo, GH_Conversion.Both, ref castColour))
        {
          //Yay, the data could be converted. Put the new colour back into our volatile data.
          branch[i] = castColour;
          continue;
        }

        //Tough luck, the data is beyond repair. We'll set a runtime error and insert a null.
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          string.Format("Data of type {0} could not be converted into either a colour or a curve", goo.TypeName));
        branch[i] = null;

        //As a side-note, we are not using the CastTo methods here on goo. If goo is of some unknown 3rd party type
        //which knows how to convert itself into a curve then this parameter will not work with that. 
        //If you want to know how to do this, ask.
      }
    }
  }
  #endregion
}