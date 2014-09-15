using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace WIP
{
  /// <summary>
  /// BoatShell class, this class defines the basic properties and methods for any Boatshell.
  /// </summary>
  public class BoatShell
  {
    #region fields
    private double m_width;
    private double m_length;
    private Point3d m_mast;
    private Brep m_shape;
    private Curve m_waterLine;
    #endregion

    #region constructors
    public BoatShell()
    {
      m_width = 0.0;
      m_length = 0.0;
      m_mast = Point3d.Unset;
    }
    public BoatShell(double width, double length, Brep shape, Point3d mast)
    {
      m_width = width;
      m_length = length;
      m_shape = shape;
      m_mast = mast;
    }
    public BoatShell Duplicate()
    {
      BoatShell dup = new BoatShell(Width, Length, Shape, Mast);
      if (m_waterLine != null)
        dup.m_waterLine = m_waterLine.DuplicateCurve();
      return dup;
    }
    #endregion

    #region properties
    public bool IsValid
    {
      get
      {
        if (Width <= 0.0) { return false; }
        if (Length <= 0.0) { return false; }
        if (m_shape == null) { return false; }

        if (!Rhino.RhinoMath.IsValidDouble(Width)) { return false; }
        if (!Rhino.RhinoMath.IsValidDouble(Length)) { return false; }

        return true;
      }
    }
    public double Width
    {
      get { return m_width; }
      set { m_width = value; }
    }
    public double Length
    {
      get { return m_length; }
      set { m_length = value; }
    }
    public Point3d Mast
    {
      get { return m_mast; }
      set { m_mast = value; }
    }
    public Point3d Flag
    {
      get { return Mast + new Vector3d(0, 0, 10); }
    }
    public double Area
    {
      get { return Length * Width; }
    }

    public Brep Shape
    {
      get { return m_shape; }
      set
      {
        m_shape = value;
        m_waterLine = null;
      }
    }
    public Curve WaterLine
    {
      get
      {
        if (m_waterLine != null) { return m_waterLine; }
        if (m_shape == null) { return null; }

        m_waterLine = ComputeWaterLine();
        return m_waterLine;
      }
    }
    #endregion

    #region methods
    public override string ToString()
    {
      return string.Format("W:{0:0.00}, L:{1:0.00}", Width, Length);
    }

    /// <summary>
    /// Compute the water line curve (elevation == 0.0)
    /// </summary>
    /// <returns>A section plane curve through the hull.</returns>
    private Curve ComputeWaterLine()
    {
      if (m_shape == null) { return null; }

      Curve[] curves;
      Point3d[] points;
      if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(m_shape, Plane.WorldXY, 0.01, out curves, out points))
      {
        return null;
      }

      if (curves == null) { return null; }
      if (curves.Length == 0) { return null; }
      if (curves.Length == 1) { return curves[0]; }

      curves = Curve.JoinCurves(curves, 0.05);
      if (curves == null) { return null; }
      return curves[0];
    }
    #endregion
  }

  /// <summary>
  /// BoatShell Goo wrapper class, makes sure BoatShell can be used in Grasshopper.
  /// </summary>
  public class BoatShellGoo : GH_GeometricGoo<BoatShell>, IGH_PreviewData
  {
    #region constructors
    public BoatShellGoo()
    {
      this.Value = new BoatShell();
    }
    public BoatShellGoo(BoatShell shell)
    {
      if (shell == null)
        shell = new BoatShell();
      this.Value = shell;
    }

    public override IGH_GeometricGoo DuplicateGeometry()
    {
      return DuplicateBoatShell();
    }
    public BoatShellGoo DuplicateBoatShell()
    {
      return new BoatShellGoo(Value == null ? new BoatShell() : Value.Duplicate());
    }
    #endregion

    #region properties
    public override bool IsValid
    {
      get
      {
        if (Value == null) { return false; }
        return Value.IsValid;
      }
    }
    public override string IsValidWhyNot
    {
      get
      {
        if (Value == null) { return "No internal BoatShell instance"; }
        if (Value.IsValid) { return string.Empty; }
        return "Invalid BoatShell instance"; //Todo: beef this up to be more informative.
      }
    }
    public override string ToString()
    {
      if (Value == null)
        return "Null BoatShell";
      else
        return Value.ToString();
    }
    public override string TypeName
    {
      get { return ("BoatShell"); }
    }
    public override string TypeDescription
    {
      get { return ("Defines a single boat shell shape"); }
    }

    public override BoundingBox Boundingbox
    {
      get
      {
        if (Value == null) { return BoundingBox.Empty; }
        if (Value.Shape == null) { return BoundingBox.Empty; }
        return Value.Shape.GetBoundingBox(true);
      }
    }
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value == null) { return BoundingBox.Empty; }
      if (Value.Shape == null) { return BoundingBox.Empty; }
      return Value.Shape.GetBoundingBox(xform);
    }
    #endregion

    #region casting methods
    public override bool CastTo<Q>(out Q target)
    {
      //Cast to BoatShell.
      if (typeof(Q).IsAssignableFrom(typeof(BoatShell)))
      {
        if (Value == null)
          target = default(Q);
        else
          target = (Q)(object)Value;
        return true;
      }

      //Cast to Brep.
      if (typeof(Q).IsAssignableFrom(typeof(Brep)))
      {
        if (Value == null)
          target = default(Q);
        else if (Value.Shape == null)
          target = default(Q);
        else
          target = (Q)(object)Value.Shape.DuplicateShallow();
        return true;
      }

      //Todo: cast to point, number, mesh, curve?

      target = default(Q);
      return false;
    }
    public override bool CastFrom(object source)
    {
      if (source == null) { return false; }

      //Cast from BoatShell
      if (typeof(BoatShell).IsAssignableFrom(source.GetType()))
      {
        Value = (BoatShell)source;
        return true;
      }

      return false;
    }
    #endregion

    #region transformation methods
    public override IGH_GeometricGoo Transform(Transform xform)
    {
      //It's debatable whether you should maintain a BoatShell through transformations. 
      //It might not be easy/make sense to apply scaling/rotations, shears etc.
      //In this example, I'll convert the shell to a brep.
      //Perhaps you will want to check for translations/rotations only, operations that make sense.
      if (Value == null) { return null; }
      if (Value.Shape == null) { return null; }

      Brep brep = Value.Shape.DuplicateBrep();
      brep.Transform(xform);
      return new GH_Brep(brep);
    }
    public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
    {
      if (Value == null) { return null; }
      if (Value.Shape == null) { return null; }

      Brep brep = Value.Shape.DuplicateBrep();
      xmorph.Morph(brep);
      return new GH_Brep(brep);
    }
    #endregion

    #region drawing methods
    public BoundingBox ClippingBox
    {
      get { return Boundingbox; }
    }
    public void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      //No meshes are drawn.   
    }
    public void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value == null) { return; }

      //Draw hull shape.
      if (Value.Shape != null)
      {
        args.Pipeline.DrawBrepWires(Value.Shape, args.Color, -1);
      }

      //Draw waterline.
      if (Value.WaterLine != null)
      {
        //You could also try and draw your waterline as dotted, 
        //but that would involve further caching.
        args.Pipeline.DrawCurve(Value.WaterLine, System.Drawing.Color.SkyBlue, 4);
      }

      //Draw mast.
      if (Value.Mast.IsValid)
      {
        Point3d p0 = Value.Mast;
        Point3d p1 = Value.Flag;

        args.Pipeline.DrawPoint(p0, Rhino.Display.PointStyle.ControlPoint, 4, args.Color);
        args.Pipeline.DrawPoint(p1, Rhino.Display.PointStyle.ControlPoint, 2, args.Color);
        args.Pipeline.DrawLine(p0, p1, args.Color);
      }
    }
    #endregion
  }

  /// <summary>
  /// This class provides a Parameter interface for the Data_BoatShell type.
  /// </summary>
  public class BoatShellParameter : GH_PersistentGeometryParam<BoatShellGoo>, IGH_PreviewObject
  {
    public BoatShellParameter()
      : base(new GH_InstanceDescription("Boat Shell", "Shell", "Maintains a collection of Boat Shell data.", "WIP", "Boat"))
    {
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return null; //Todo, provide an icon.
      }
    }
    public override GH_Exposure Exposure
    {
      get
      {
        // If you want to provide this parameter on the toolbars, use something other than hidden.
        return GH_Exposure.hidden;
      }
    }
    public override Guid ComponentGuid
    {
      get { return new Guid("ee30da59-f7e4-45b1-8fac-45cf6a057bed"); }
    }

    //We do not allow users to pick boatshells, 
    //therefore the following 4 methods disable all this ui.
    protected override GH_GetterResult Prompt_Plural(ref List<BoatShellGoo> values)
    {
      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Singular(ref BoatShellGoo value)
    {
      return GH_GetterResult.cancel;
    }
    protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomSingleValueItem()
    {
      System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
      item.Text = "Not available";
      item.Visible = false;
      return item;
    }
    protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomMultiValueItem()
    {
      System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
      item.Text = "Not available";
      item.Visible = false;
      return item;
    }

    #region preview methods
    public BoundingBox ClippingBox
    {
      get
      {
        return Preview_ComputeClippingBox();
      }
    }
    public void DrawViewportMeshes(IGH_PreviewArgs args)
    {
      //Meshes aren't drawn.
    }
    public void DrawViewportWires(IGH_PreviewArgs args)
    {
      //Use a standard method to draw gunk, you don't have to specifically implement this.
      Preview_DrawWires(args);
    }

    private bool m_hidden = false;
    public bool Hidden
    {
      get { return m_hidden; }
      set { m_hidden = value; }
    }
    public bool IsPreviewCapable
    {
      get { return true; }
    }
    #endregion
  }

  public class BoatShellComponent : GH_Component
  {
    public BoatShellComponent()
      : base("Boat Shell", "Shell", "Create a new Boat shell", "WIP", "Boat") { }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return null; //Todo: create an icon.
      }
    }
    public override GH_Exposure Exposure
    {
      get
      {
        return GH_Exposure.primary;
      }
    }
    public override Guid ComponentGuid
    {
      get { return new Guid("243d7d74-3890-4a41-b87f-04f143a1576a"); }
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.Register_DoubleParam("Width", "W", "Boat shell width.");
      pManager.Register_DoubleParam("Length", "L", "Boat shell length.");
      pManager.Register_BRepParam("Shape", "S", "Boat shell shape.");
      pManager.Register_PointParam("Mast", "M", "Boat mast location.");

      pManager.HideParameter(2);
      pManager.HideParameter(3);
    }
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.RegisterParam(new BoatShellParameter(), "Shell", "S", "Boat shell");
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      double width = 0.0;
      double length = 0.0;
      Brep shape = null;
      Point3d mast = Point3d.Unset;

      if (!DA.GetData(0, ref width)) { return; }
      if (!DA.GetData(1, ref length)) { return; }
      if (!DA.GetData(2, ref shape)) { return; }
      if (!DA.GetData(3, ref mast)) { return; }

      BoatShell BS = new BoatShell(width, length, shape, mast);
      DA.SetData(0, BS);
    }
  }

  public class HarbourComponent : GH_Component
  {
    public HarbourComponent()
      : base("Harbour", "Harbour", "Harbour", "WIP", "Boat")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.RegisterParam(new BoatShellParameter(), "Boat A", "A", "First boat shell");
      pManager.RegisterParam(new BoatShellParameter(), "Boat B", "B", "Second boat shell");
    }
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.Register_StringParam("TotalArea", "A", "Boat shell total area");
      pManager.Register_BRepParam("Flags", "F", "Boat shell flags location");
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      BoatShell A = null;
      BoatShell B = null;

      if (!DA.GetData(0, ref A)) { return; }
      if (!DA.GetData(1, ref B)) { return; }

      double area = A.Area + B.Area;
      Point3d[] flags = new Point3d[2] { A.Flag, B.Flag };

      DA.SetData("TotalArea", area);
      DA.SetDataList("Flags", flags);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return base.Icon; //Todo: create icon.
      }
    }
    public override GH_Exposure Exposure
    {
      get
      {
        return GH_Exposure.primary;
      }
    }
    public override Guid ComponentGuid
    {
      get { return new Guid("243d7d74-3890-4a41-b87f-04f143a1577b"); }
    }
  }
}