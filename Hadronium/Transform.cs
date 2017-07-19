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
        protected abstract void SetRenderSize(Size size);

        protected abstract void SetOffset(Vector value);

        protected virtual void SetRotation(double value)
        { }

        protected virtual double GetRotation()
        {
            return 0;
        }

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

        public double Rotation
        {
            get { return GetRotation(); }
            set { SetRotation(value); }
        }

        public bool ChangeScale(double newScale, Point fixedPoint)
        {
            if (ViewScale == newScale)
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
            set { SetOffset(value); }
        }

        public Size RenderSize
        {
            set { SetRenderSize(value); }
        }

        protected Vector offset = new Vector(0, 0);

        protected double scale = PixelsPerMeter;

        private const int PixelsPerMeter = 500;
    }

    class Transform1D : Transform
    {
        protected override void SetOffset(Vector value)
        {
            offset.Y = value.Y;
        }

        protected override void SetRenderSize(Size size)
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
        protected override void SetRenderSize(Size size)
        {
        }

        protected override void SetOffset(Vector value)
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
                (p.X - offset.X) / scale
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
        private double rotX = 0;

        protected override void SetRenderSize(Size size)
        {
            offset.X = size.Width / 2;
        }

        protected override void SetOffset(Vector value)
        {
            offset = value;
        }

        protected override void SetRotation(double value)
        {
            rotX = value;
        }

        protected override double GetRotation()
        {
            return rotX;
        }

        public override Point ToScreen(double[] d)
        {
            var cos = Math.Cos(rotX);
            var sin = Math.Sin(rotX);
            return new Point(
              scale * (d[1] * cos - d[2] * sin) + offset.X,
              scale * d[0] + offset.Y);
        }

        public override double[] ToWorld(Point p)
        {
            var cos = Math.Cos(rotX);
            var sin = Math.Sin(rotX);
            return new double[3] 
            { 
                (p.Y - offset.Y) / scale,
                (p.X - offset.X) * cos / scale,
                -(p.X - offset.X) * sin / scale
            };
        }

        public override double[] ToWorld(Vector v)
        {
            var cos = Math.Cos(rotX);
            var sin = Math.Sin(rotX);
            return new double[3] 
            { 
                v.Y / scale,
                v.X * cos / scale,
               -v.X * sin / scale
            };
        }

        public double Rotation
        {
            get { return rotX; }
            set { rotX = value; }
        }
    }

}


