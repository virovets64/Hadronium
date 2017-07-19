using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Hadronium
{
    class SelectionAdorner : Adorner
    {
        public static SelectionAdorner Create(UIElement element)
        {
            var result = new SelectionAdorner(element);
            AdornerLayer.GetAdornerLayer(element).Add(result);
            return result;
        }

        public void Destroy()
        {
            AdornerLayer.GetAdornerLayer(AdornedElement).Remove(this);
        }

        private SelectionAdorner(UIElement adornedElement)
            : base(adornedElement)
        { }

        public Point From
        {
            get
            {
                return from;
            }
            set
            {
                from = value;
                InvalidateVisual();
            }
        }

        public Point To
        {
            get
            {
                return to;
            }
            set
            {
                to = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var pen = new Pen(Brushes.Black, 1);
            pen.DashStyle = DashStyles.Dash;
            var rect = new Rect(from, to);
            drawingContext.DrawRectangle(null, pen, rect);
        }

        Point from;
        Point to;
    }
}
