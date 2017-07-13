using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Hadronium
{
  abstract class Transform
  {
    public abstract Point ToScreen(double[] d);
    public abstract double[] ToWorld(Point p);
    public abstract double[] ToWorld(Vector v);
    public Box ToWorld(Rect r)
    {
      return new Box(ToWorld(r.TopLeft), ToWorld(r.BottomRight));
    }

    public double ViewScale
    {
      get { return scale / PixelsPerMeter; }
      set { scale = value * PixelsPerMeter; }
    }

    public bool changeScale(double newScale, Point fixedPoint)
    {
      if(ViewScale == newScale)
        return false;
      Offset = new Vector(
        fixedPoint.X - (fixedPoint.X - Offset.X) * newScale / ViewScale,
        fixedPoint.Y - (fixedPoint.Y - Offset.Y) * newScale / ViewScale);
      ViewScale = newScale;
      return true;
    }

    public Vector Offset = new Vector(0, 0);
    protected double scale = PixelsPerMeter;
    private const int PixelsPerMeter = 500;
  }

  class Transform1D: Transform
  {
    public override Point ToScreen(double[] d)
    {
      return new Point(0, scale * d[0] + Offset.Y);
    }
    public override double[] ToWorld(Point p)
    {
      return new double[1] { (p.Y - Offset.Y) / scale };
    }
    public override double[] ToWorld(Vector v)
    {
      return new double[1] { v.Y / scale };
    }

  }

  class Transform2D : Transform
  {
    public override Point ToScreen(double[] d)
    {
      return new Point(
        scale * d[1] + Offset.X, 
        scale * d[0] + Offset.Y);
    }
    public override double[] ToWorld(Point p)
    {
      return new double[2] 
      { 
        (p.Y - Offset.Y) / scale,
        (p.X - Offset.X) / scale,
      };
    }
    public override double[] ToWorld(Vector v)
    {
      return new double[2] 
      { 
        v.Y / scale,
        v.X / scale,
      };
    }
  }

}

   
