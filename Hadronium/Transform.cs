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
    protected abstract void setRenderSize(Size size);
    protected abstract void setOffset(Vector value);
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
        fixedPoint.X - (fixedPoint.X - offset.X) * newScale / ViewScale,
        fixedPoint.Y - (fixedPoint.Y - offset.Y) * newScale / ViewScale);
      ViewScale = newScale;
      return true;
    }

    public Vector Offset
    {
      get { return offset; }
      set { setOffset(value); }
    }
    public Size RenderSize
    {
      set { setRenderSize(value); }
    }

    public Vector offset = new Vector(0, 0);
    protected double scale = PixelsPerMeter;
    private const int PixelsPerMeter = 500;
  }

  class Transform1D: Transform
  {
    protected override void setOffset(Vector value)
    {
      offset.Y = value.Y;
    }
    protected override void setRenderSize(Size size)
    {
      offset.X = size.Width / 2;
    }
    public override Point ToScreen(double[] d)
    {
      return new Point(offset.X, scale * d[0] + offset.Y);
    }
    public override double[] ToWorld(Point p)
    {
      return new double[1] { (p.Y - offset.Y) / scale };
    }
    public override double[] ToWorld(Vector v)
    {
      return new double[1] { v.Y / scale };
    }

  }

  class Transform2D : Transform
  {
    protected override void setRenderSize(Size size)
    {
    }
    protected override void setOffset(Vector value)
    {
      offset = value;
    }
    public override Point ToScreen(double[] d)
    {
      return new Point(
        scale * d[1] + offset.X, 
        scale * d[0] + offset.Y);
    }
    public override double[] ToWorld(Point p)
    {
      return new double[2] 
      { 
        (p.Y - offset.Y) / scale,
        (p.X - offset.X) / scale,
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


  class Transform3D : Transform
  {
//    private double rotationX = 0;

    protected override void setRenderSize(Size size)
    {
    }
    protected override void setOffset(Vector value)
    {
      offset = value;
    }
    public override Point ToScreen(double[] d)
    {
      return new Point(
        scale * d[1] + offset.X,
        scale * d[0] + offset.Y);
    }
    public override double[] ToWorld(Point p)
    {
      return new double[2] 
      { 
        (p.Y - offset.Y) / scale,
        (p.X - offset.X) / scale,
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

   
