using System;
using System.Windows.Data;
using System.Reflection;

namespace Hadronium
{
    public enum SourceKind { Model, View };
    public class PropertyDescription
    {
        public PropertyDescription(SourceKind kind, string name, object defaultValue, object minimum, object maximum,
            IValueConverter converter = null, string feedbackPropertyName = null)
        {
            Kind = kind;
            Name = name;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            Converter = converter;
            FeedbackPropertyName = feedbackPropertyName;
            //            ReadOnly = readOnly;
        }

        public SourceKind Kind;
        public string Name;
        public object DefaultValue;
        public object Minimum;
        public object Maximum;
        public IValueConverter Converter;
        public string FeedbackPropertyName;
        
        public object GetValue(object target)
        {
            return target.GetType().GetProperty(Name).GetValue(target, null);
        }
        
        public void SetValue(object target, object value)
        {
            target.GetType().GetProperty(Name).SetValue(target, value, null);
        }
    }

    class PropertyInstance
    {
        public PropertyDescription Description;
        public object Target;

        public PropertyInstance(PropertyDescription description, object target)
        {
            this.Description = description;
            this.Target = target;
        }
    }


    public class Exponent
    {
        double a;
        double b;
        double k;
        
        public Exponent(double min, double max, double mid)
        {
            a = (mid - min) * (mid - min) / (min + max - 2 * mid);
            b = min - a;
            k = 2 * Math.Log((mid - min) / a + 1);
        }
        
        public double Direct(double x)
        {
            return a * Math.Exp(k * x) + b;
        }
        
        public double Inverse(double y)
        {
            return Math.Log((y - b) / a) / k;
        }
    }


    [ValueConversion(typeof(double), typeof(double))]
    // Converts by formula: f(y) = a*exp(k*y) + b, wherein
    // f(0) = min, f(1) = max, f(0.5) = default
    public class LogarithmicConverter : IValueConverter
    {
        Exponent exponent = null;

        void Initialize(object parameter)
        {
            if (exponent == null)
            {
                var pd = parameter as PropertyDescription;
                exponent = new Exponent((double)pd.Minimum, (double)pd.Maximum, (double)pd.DefaultValue);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Initialize(parameter);
            return exponent.Inverse((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Initialize(parameter);
            return exponent.Direct((double)value);
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class BiLogarithmicConverter : IValueConverter
    {
        Exponent exponent = null;
        double quarter;
        double y0;

        public BiLogarithmicConverter(double quarter)
        {
            this.quarter = quarter;
        }

        void Initialize(object parameter)
        {
            if (exponent == null)
            {
                var pd = parameter as PropertyDescription;
                y0 = ((double)pd.Maximum + (double)pd.Minimum) / 2;
                exponent = new Exponent(y0, (double)pd.Maximum, quarter);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Initialize(parameter);

            double y = (double)value;
            if (y > y0)
                return (exponent.Inverse(y) + 1) / 2;
            else
                return (-exponent.Inverse(2 * y0 - y) + 1) / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Initialize(parameter);
            double x = (double)value * 2 - 1;
            return x >= 0 ? exponent.Direct(x) : 2 * y0 - exponent.Direct(-x);
        }
    }
}
