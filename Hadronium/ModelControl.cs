using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
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
            get
            {
                return model;
            }
            set
            {
                model = value;
                switch (model.Dimension)
                {
                    case 1:
                        transform = new Transform1D();
                        break;
                    case 2:
                        transform = new Transform2D();
                        break;
                    case 3:
                        transform = new Transform3D();
                        break;
                }
                transform.RenderSize = RenderSize;
            }
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

        public double Rotation
        {
            get
            {
                return transform.Rotation * 180 / Math.PI;
            }
            set
            {
                transform.Rotation = value / 180 * Math.PI;
                InvalidateVisual();
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Rotation"));
            }
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
                UpdateFontSize();
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

        public void Link(bool value)
        {
            var selectedPartices = model.Particles.Where(x => (x.Tag as DrawData).Selected).ToList();
            foreach (var p1 in selectedPartices)
                foreach (var p2 in selectedPartices)
                    if (p1 != p2)
                        if (value)
                            model.AddLink(p1, p2);
                        else
                            model.RemoveLink(p1, p2);
            InvalidateVisual();
        }

        public bool CanLink(bool value)
        {
            return !model.Active;
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

        Random random = new Random();

        private Color getRandomColor()
        {
            Color color = new Color();
            Byte[] bytes = new Byte[3];
            random.NextBytes(bytes);
            color.R = bytes[0];
            color.G = bytes[1];
            color.B = bytes[2];
            color.A = 0xFF;
            return color;
        }

        public void NewRandomModel(int particleCount, int linkCount)
        {
            model.AddRandomParticles(particleCount, linkCount, GetInitialRect());

            foreach (var p in model.Particles)
            {
                p.FillColor = getRandomColor();
            }
            InvalidateVisual();
        }


        private double refreshPeriod = 0.035;
        private const double RotationSpeed = 1;

        private double particleSize = 8;
        private double textSize = 12;

        private Transform transform = new Transform2D();

        private enum ToolKind
        {
            None,
            ScrollView,
            MoveSelectedParticles,
            SelectRectangle
        }

        private ToolKind toolKind = ToolKind.None;
        private Point mouseDownPosition;
        private Point mouseCurrentPosition;
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
            myTimer = new DispatcherTimer(new TimeSpan((long)(refreshPeriod * 10000)), DispatcherPriority.SystemIdle, TimerProc, Dispatcher);
            createPinImage();

        }

        SelectionAdorner selectionAdorner;


        private void TimerProc(Object state, EventArgs e)
        {
            if (model.ActualStepCount != modelStepCount)
            {
                model.Refresh();
                modelStepCount = model.ActualStepCount;
                InvalidateVisual();
            }
        }

        private Particle ParticleAtPoint(Point p)
        {
            for (int i = model.Particles.Count - 1; i >= 0; i--)
            {
                Particle particle = model.Particles[i];
                Vector v = transform.ToScreen(particle.Position) - p;
                if (v.Length <= ParticleSize / 2 + 4)
                    return particle;
            }
            return null;
        }

        private void UpdateFontSize()
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
                drawingContext.DrawLine(link.A.Position[0] < link.B.Position[0] ? forwardPen : backwardPen,
                    transform.ToScreen(link.A.Position),
                    transform.ToScreen(link.B.Position));
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

                Point p = transform.ToScreen(particle.Position);
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
                    mouseDownPosition = p;
                    var hitParticle = ParticleAtPoint(p);
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (hitParticle == null)
                        {
                            var newParticle = new Particle(model.Dimension);
                            newParticle.Position = transform.ToWorld(mouseDownPosition);
                            newParticle.FillColor = getRandomColor();
                            model.AddParticle(newParticle);
                            InvalidateVisual();
                        }
                        else
                        {
                            model.RemoveParticle(hitParticle);
                            InvalidateVisual();
                        }
                    }
                    else if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        if (hitParticle != null)
                        {
                            toolKind = ToolKind.None;
                            var drawData = hitParticle.Tag as DrawData;
                            drawData.Selected = !drawData.Selected;
                            InvalidateVisual();
                        }
                        else
                        {
                            toolKind = ToolKind.SelectRectangle;
                            selectionAdorner = SelectionAdorner.Create(this);
                            selectionAdorner.From = mouseDownPosition;
                            selectionAdorner.To = mouseDownPosition;
                        }
                    }
                    else
                    {
                        if (hitParticle != null)
                        {
                            toolKind = ToolKind.MoveSelectedParticles;
                            var drawData = hitParticle.Tag as DrawData;
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
                                (hitParticle.Tag as DrawData).Selected = true;
                                hitParticle.Fixed = true;
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
            mouseCurrentPosition = p;
            var moveBy = transform.ToWorld(p - mouseDownPosition);
            switch (toolKind)
            {
                case ToolKind.MoveSelectedParticles:
                    foreach (var particle in model.Particles)
                    {
                        if ((particle.Tag as DrawData).Selected)
                        {
                            for (int i = 0; i < moveBy.Length; i++)
                                particle.Position[i] += moveBy[i];
                            Array.Clear(particle.Velocity, 0, particle.Velocity.Length);
                            // particle.Velocity = v / (dragStopwatch.ElapsedMilliseconds * 0.001 * model.TimeScale);
                        }
                    }
                    // dragStopwatch.Restart();
                    model.Refresh();
                    mouseDownPosition = p;
                    InvalidateVisual();
                    break;
                case ToolKind.ScrollView:
                    transform.Offset += (p - mouseDownPosition);
                    mouseDownPosition = p;
                    InvalidateVisual();
                    break;
                case ToolKind.SelectRectangle:
                    selectionAdorner.To = mouseCurrentPosition;
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
                    var r = new Rect(mouseDownPosition, e.GetPosition(this));
                    foreach (var particle in model.Particles)
                    {
                        if (r.Contains(transform.ToScreen(particle.Position)))
                            (particle.Tag as DrawData).Selected = true;
                    }
                    selectionAdorner.Destroy();
                    selectionAdorner = null;
                    break;
            }
            InvalidateVisual();
            toolKind = ToolKind.None;
            ReleaseMouseCapture();
        }

        private void ChangeScale(double newViewScale, Point p)
        {
            if (transform.ChangeScale(newViewScale, p))
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ViewScale"));
                InvalidateVisual();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double coef = 1.1;
            if (e.Delta < 0)
                coef = 1.0 / coef;
            ChangeScale(transform.ViewScale * coef, e.GetPosition(this));
        }

        public Box GetInitialRect()
        {
            Size size = RenderSize;
            Rect rect = new Rect(size);
            rect.Inflate(-size.Width / 4, -size.Height / 4);
            var result = transform.ToWorld(rect);
            return result;
        }

        internal void RandomizePositions()
        {
            model.RandomizePositions(GetInitialRect());
            InvalidateVisual();
        }
    }

}
