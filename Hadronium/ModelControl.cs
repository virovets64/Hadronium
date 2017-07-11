using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
#if Model3D
using System.Windows.Media.Media3D;
#endif
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;

namespace Hadronium
{
  public class ModelControl : FrameworkElement, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    public Model Model
    {
      get { return model; }
      set { model = value; }
    }
    public double RefreshPeriod
    {
      get { return refreshPeriod; }
      set
      {
        refreshPeriod = value;
        if (myTimer != null)
          myTimer.Interval = new TimeSpan((long)(refreshPeriod * 10000000));
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs("RefreshPeriod"));
      }
    }
    public Vector Offset
    {
      get { return offset; }
      set
      {
        offset = value;
        calcTransforms();
      }
    }
    public double ViewScale
    {
      get { return viewScale / PixelsPerMeter; }
      set { setViewScale(value * PixelsPerMeter, new Point(RenderSize.Width / 2, RenderSize.Height / 2)); }
    }
    public double ParticleSize
    {
      get { return particleSize; }
      set
      {
        particleSize = value;
        InvalidateVisual();
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs("ParticleSize"));
      }
    }
    public double TextSize
    {
      get { return textSize; }
      set
      {
        textSize = value;
        updateFontSize();
        InvalidateVisual();
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs("TextSize"));
      }
    }
    public double RenderElapsedTime
    {
      get { return renderElapsedTime; }
      set
      {
        renderElapsedTime = value;
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs("RenderElapsedTime"));
      }
    }

    public void Pin(bool value)
    {
      foreach (var particle in model.Particles)
      {
        DrawData drawData = particle.Tag as DrawData;
        if (drawData.Selected)
        {
          drawData.Pinned = value;
          particle.Fixed = value;
        }
      }
      InvalidateVisual();
    }

    public bool CanPin(bool value)
    {
      foreach (var particle in model.Particles)
      {
        DrawData drawData = particle.Tag as DrawData;
        if (drawData.Selected && drawData.Pinned != value)
          return true;
      }
      return false;
    }

    public RenderTargetBitmap RenderToBitmap()
    {

      DrawingVisual drawingVisual = new DrawingVisual();
      DrawingContext drawingContext = drawingVisual.RenderOpen();
      OnRender(drawingContext);
      drawingContext.Close();

      Size size = RenderSize;
      int k = 5;
      RenderTargetBitmap bmp = new RenderTargetBitmap((int)size.Width * k, (int)size.Height * k, 96d * k, 96d * k, PixelFormats.Pbgra32);
      bmp.Render(drawingVisual);
      return bmp;
    }

    public void NewRandomModel(int particleCount, int linkCount)
    {
      model.AddRandomParticles(particleCount, linkCount, getInitialRect());

      Random random = new Random();
      for (int i = 0; i < model.Particles.Count; i++)
      {
        Color color = new Color();
        Byte[] bytes = new Byte[3];
        random.NextBytes(bytes);
        color.R = bytes[0];
        color.G = bytes[1];
        color.B = bytes[2];
        color.A = 0xFF;
        model.Particles[i].FillColor = color;
        model.Particles[i].StrokeColor = Colors.Transparent;
      }
      InvalidateVisual();
    }

    
    private double refreshPeriod = 0.035;
    private const int PixelsPerMeter = 500;
    private const double RotationSpeed = 1;

    private Vector offset = new Vector(0, 0);
    private double viewScale = PixelsPerMeter;
    private double particleSize = 8;
    private double textSize = 12;

#if Model3D
        private Matrix3D w2s;
        private Matrix3D s2w;
        private Vector3D rotation = new Vector3D(0, 0, 0);

        public Vector3D Rotation
        {
            get { return rotation; }
            set { rotation = value; calcTransforms(); InvalidateVisual(); }
        }

#else
    private Matrix w2s = new Matrix();
    private Matrix s2w = new Matrix();
#endif

    private void calcTransforms()
    {
#if Model3D
            Point3D center = ToWorldCoord(new Point(RenderSize.Width / 2, RenderSize.Height / 2));
//            center.X = center.Y = 0;
            center.Z = 0;
            w2s = new Matrix3D();
            w2s.RotateAt(new Quaternion(new Vector3D(1, 0, 0), rotation.X * RotationSpeed), center);
            w2s.RotateAt(new Quaternion(new Vector3D(0, 1, 0), rotation.Y * RotationSpeed), center);
            w2s.RotateAt(new Quaternion(new Vector3D(0, 0, 1), rotation.Z * RotationSpeed), center);
            w2s.ScaleAt(new Vector3D(viewScale, viewScale, viewScale), new Point3D(0, 0, 0));
            w2s.Translate(new Vector3D(offset.X, offset.Y, 0));
#else
      w2s = new Matrix();
      w2s.Scale(viewScale, viewScale);
      w2s.Translate(offset.X, offset.Y);
#endif
      s2w = w2s;
      s2w.Invert();
    }


    private enum ToolKind
    {
      None,
      ScrollView,
      MoveSelectedParticles,
      SelectRectangle
    }
    private ToolKind toolKind = ToolKind.None;
    private Point savedMousePosition;
    //        private Stopwatch dragStopwatch = new Stopwatch();
    private Stopwatch renderStopwatch = new Stopwatch();
    private DispatcherTimer myTimer = null;
    private Model model;
    private long modelStepCount = 0;
    private Typeface textTypeface = new Typeface("Arial");

    private double renderElapsedTime;

    private class DrawData
    {
      public Brush Brush;
      public Pen Pen;
      public FormattedText Text;
      public bool Selected;
      public bool Pinned;
    }


    //static ModelControl()
    //{
    //    DefaultStyleKeyProperty.OverrideMetadata(typeof(ModelControl), new FrameworkPropertyMetadata(typeof(ModelControl)));
    //}

    private PathGeometry pinImage;

    private void createPinImage()
    {
      pinImage = new PathGeometry();
      var myPathFigure = new PathFigure();
      myPathFigure.StartPoint = new Point(0, -5);
      myPathFigure.Segments.Add(new LineSegment(new Point(-7, -10 - 5), true));
      myPathFigure.Segments.Add(new LineSegment(new Point(7, -10 - 5), true));
      myPathFigure.IsClosed = true;
      myPathFigure.IsFilled = true;
      pinImage.Figures.Add(myPathFigure);
      myPathFigure = new PathFigure();
      myPathFigure.Segments.Add(new LineSegment(new Point(0, -5), true));
      pinImage.Figures.Add(myPathFigure);
    }

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);
      //            model.Changed += new EventHandler(model_Changed);
      myTimer = new DispatcherTimer(new TimeSpan((long)(refreshPeriod * 10000)), DispatcherPriority.SystemIdle, timerProc, Dispatcher);
      calcTransforms();
      createPinImage();
    }


#if Model3D
        private Point3D ToWorldCoord(Point p)
        {
            return s2w.Transform(new Point3D(p.X, p.Y, 0));
        }
        private Vector3D ToWorldCoord(Vector v)
        {
            return s2w.Transform(new Vector3D(v.X, v.Y, 0));
        }
        private Size3D ToWorldCoord(Size s)
        {
            return (Size3D)s2w.Transform(new Vector3D(s.Width, s.Height, 0));
        }
        private Rect3D ToWorldCoord(Rect r)
        {
            return new Rect3D(ToWorldCoord(r.TopLeft), ToWorldCoord(r.Size));
        }
        private Point ToScreenCoord(Point3D p)
        {
            Point3D p1 = w2s.Transform(p);
            return new Point(p1.X, p1.Y);
        }

#else
    private Point ToWorldCoord(Point p)
    {
      return s2w.Transform(p);
    }
    private Vector ToWorldCoord(Vector v)
    {
      return s2w.Transform(v);
    }
    private Rect ToWorldCoord(Rect r)
    {
      return new Rect(ToWorldCoord(r.TopLeft), ToWorldCoord(r.BottomRight));
    }
    private Point ToScreenCoord(Point p)
    {
      return w2s.Transform(p);
    }
#endif

    private void timerProc(Object state, EventArgs e)
    {
      if (model.ActualStepCount != modelStepCount)
      {
        model.Refresh();
        modelStepCount = model.ActualStepCount;
        InvalidateVisual();
      }
    }

    private int indexOfPoint(Point p)
    {
      for (int i = model.Particles.Count - 1; i >= 0; i--)
      {
        Particle particle = model.Particles[i];
        Vector v = ToScreenCoord(particle.Position) - p;
        if (v.Length <= ParticleSize / 2)
          return i;
      }
      return -1;
    }

    private void updateFontSize()
    {
      if (model != null)
      {
        foreach (var particle in model.Particles)
        {
          DrawData drawData = particle.Tag as DrawData;
          if (drawData != null && drawData.Text != null)
          {
            drawData.Text.SetFontSize(textSize);
          }
        }
      }
    }

    protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
    {
      renderStopwatch.Restart();
      Pen forwardPen = new Pen(Brushes.Silver, 1);
      Pen backwardPen = new Pen(Brushes.Pink, 1);
      Pen fixedPen = new Pen(Brushes.Black, 1);
      foreach (var link in model.Links)
      {
        drawingContext.DrawLine(model.Particles[link.A].Position.X < model.Particles[link.B].Position.X ? forwardPen : backwardPen,
            ToScreenCoord(model.Particles[link.A].Position),
            ToScreenCoord(model.Particles[link.B].Position));
      }
      foreach (var particle in model.Particles)
      {
        DrawData drawData = particle.Tag as DrawData;
        if (drawData == null)
        {
          drawData = new DrawData();
          if (particle.FillColor.A != 0)
            drawData.Brush = new SolidColorBrush(particle.FillColor);
          if (particle.StrokeColor.A != 0)
            drawData.Pen = new Pen(new SolidColorBrush(particle.StrokeColor), 1);
          drawData.Pinned = particle.Fixed;
          if (particle.Name != null)
            drawData.Text = new FormattedText(particle.Name, CultureInfo.CurrentCulture,
                                              FlowDirection.LeftToRight, textTypeface, textSize, Brushes.Black);
          particle.Tag = drawData;
        }

        Point p = ToScreenCoord(particle.Position);
        drawingContext.DrawEllipse(drawData.Brush, drawData.Pen,
            p,
            ParticleSize,
            ParticleSize);
        if (drawData.Selected)
          drawingContext.DrawEllipse(null, fixedPen,
              p,
              ParticleSize * 1.3,
              ParticleSize * 1.3);


        if (drawData.Text != null)
          drawingContext.DrawText(drawData.Text, p);
        if (drawData.Pinned)
        {
          //                    drawingContext.DrawRectangle(Brushes.Black, fixedPen, new Rect(0, 0, 8, 8));
          drawingContext.PushTransform(new TranslateTransform(p.X, p.Y));
          drawingContext.PushTransform(new ScaleTransform(ParticleSize / 8, ParticleSize / 8));
          drawingContext.DrawGeometry(Brushes.LightCoral, fixedPen, pinImage);
          drawingContext.Pop();
          drawingContext.Pop();
        }
      }

      RenderElapsedTime = renderStopwatch.Elapsed.TotalSeconds;
      renderStopwatch.Restart();
    }


    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
      return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }


    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      switch (e.ChangedButton)
      {
        case MouseButton.Left:
          Point p = e.GetPosition(this);
          savedMousePosition = p;
          int particleIndex = indexOfPoint(p);
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            if (particleIndex != -1) // ткнули в частицу - переключаем Selected
            {
              toolKind = ToolKind.None;
              var drawData = model.Particles[particleIndex].Tag as DrawData;
              drawData.Selected = !drawData.Selected;
              InvalidateVisual();
            }
            else
              toolKind = ToolKind.SelectRectangle;
          }
          else // Ctrl не нажат
          {
            if (particleIndex != -1)  // ткнули в частицу
            {
              toolKind = ToolKind.MoveSelectedParticles;
              var drawData = model.Particles[particleIndex].Tag as DrawData;
              if (drawData.Selected)
              {
                foreach (var particle in model.Particles)
                {
                  DrawData dd = particle.Tag as DrawData;
                  if (dd.Selected)
                    particle.Fixed = true;
                }
              }
              else
              {
                foreach (var particle in model.Particles)
                  (particle.Tag as DrawData).Selected = false;
                (model.Particles[particleIndex].Tag as DrawData).Selected = true;
                model.Particles[particleIndex].Fixed = true;
              }
              InvalidateVisual();
            }
            else
              toolKind = ToolKind.ScrollView;
          }
          if (toolKind != ToolKind.None)
            CaptureMouse();
          break;
      }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      Point p = e.GetPosition(this);
      var moveBy = ToWorldCoord(p - savedMousePosition);
      switch (toolKind)
      {
        case ToolKind.MoveSelectedParticles:
          foreach (var particle in model.Particles)
          {
            if ((particle.Tag as DrawData).Selected)
            {
              particle.Position += moveBy;
              Utils.Zero(ref particle.Velocity);
              // particle.Velocity = v / (dragStopwatch.ElapsedMilliseconds * 0.001 * model.TimeScale);
            }
          }
          // dragStopwatch.Restart();
          model.Refresh();
          savedMousePosition = p;
          InvalidateVisual();
          break;
        case ToolKind.ScrollView:
          Offset += (p - savedMousePosition);
          savedMousePosition = p;
          InvalidateVisual();
          break;
        case ToolKind.SelectRectangle:
          break;
      }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      //if (!Keyboard.IsKeyDown(Key.LeftCtrl))
      //{
      //    foreach (var particle in model.Particles)
      //        particle.Fixed = false;
      //}
      switch (toolKind)
      {
        case ToolKind.MoveSelectedParticles:
          foreach (var particle in model.Particles)
          {
            var drawData = particle.Tag as DrawData;
            if (drawData.Selected && !drawData.Pinned)
              particle.Fixed = false;
          }
          break;
        case ToolKind.ScrollView:
          break;
        case ToolKind.SelectRectangle:
          var r = ToWorldCoord(new Rect(savedMousePosition, e.GetPosition(this)));
#if Model3D
                    r.Z = double.NegativeInfinity;
                    r.SizeZ = double.PositiveInfinity;
#endif
          foreach (var particle in model.Particles)
          {
            if (r.Contains(particle.Position))
              (particle.Tag as DrawData).Selected = true;
          }
          break;
      }
      InvalidateVisual();
      toolKind = ToolKind.None;
      ReleaseMouseCapture();
    }

    private void setViewScale(double newViewScale, Point p)
    {
      if (viewScale == newViewScale)
        return;
      offset.X = p.X - (p.X - offset.X) * newViewScale / viewScale;
      offset.Y = p.Y - (p.Y - offset.Y) * newViewScale / viewScale;
      viewScale = newViewScale;
      calcTransforms();
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs("ViewScale"));
      InvalidateVisual();
    }


    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
      setViewScale(e.Delta > 0 ? w2s.M11 * 1.1 : w2s.M11 / 1.1, e.GetPosition(this));
    }

#if Model3D
    public Rect3D getInitialRect()
#else
    public Rect getInitialRect()
#endif
    {
      Size size = RenderSize;
      Rect rect = new Rect(size);
      rect.Inflate(-size.Width / 4, -size.Height / 4);
      var result = ToWorldCoord(rect);
#if Model3D
      result.Z = result.X;
      result.SizeZ = result.SizeX;
#endif
      return result;

    }

    internal void RandomizePositions()
    {
      model.RandomizePositions(getInitialRect());
      InvalidateVisual();
    }
  }

}
