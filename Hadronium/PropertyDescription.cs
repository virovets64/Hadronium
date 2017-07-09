using System;
using System.Windows.Data;
using System.Reflection;

namespace Hadronium
{
    public class PropertyDescription
    {
        public PropertyDescription(string name, object defaultValue, object minimum, object maximum,
            IValueConverter converter = null, string feedbackPropertyName = null)
        {
            Name = name;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            Converter = converter;
            FeedbackPropertyName = feedbackPropertyName;
            //            ReadOnly = readOnly;
        }
        public string Name;
        //        public bool ReadOnly;
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

    [ValueConversion(typeof(double), typeof(double))]
    public class LogarithmicConverter : IValueConverter
    {
        protected double medium;
        protected double a;
        protected double b;
        protected double k;
        protected void calcCoeffs(PropertyDescription pd)
        {
            calcCoeffs((double)pd.Minimum, (double)pd.Maximum);
        }
        protected void calcCoeffs(double v0, double v1)
        {
            a = (medium - v0) * (medium - v0) / (v0 + v1 - 2 * medium);
            b = v0 - a;
            k = 2 * Math.Log((medium - v0) / a + 1);
        }

        public LogarithmicConverter(double medium)
        {
            this.medium = medium;
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            calcCoeffs(parameter as PropertyDescription);
            double x = (double)value;
            return Math.Log((x - b) / a) / k;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            calcCoeffs(parameter as PropertyDescription);
            double y = (double)value;
            return a * Math.Exp(k * y) + b;
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class BiLogarithmicConverter : LogarithmicConverter
    {
        private double z;
        public BiLogarithmicConverter(double medium)
            : base(medium)
        {
        }
        protected new void calcCoeffs(PropertyDescription pd)
        {
            z = ((double)pd.Minimum + (double)pd.Maximum) / 2;
            base.calcCoeffs(z, (double)pd.Maximum);
        }

        public new object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) 
        {
            calcCoeffs(parameter as PropertyDescription);
            double x = (double)value;
            if (x > z)
                return (Math.Log((x - b) / a) / k + 1) / 2;
            else
                return (-Math.Log(((z - x) - b) / a) / k + 1) / 2;
        }

        public new object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            calcCoeffs(parameter as PropertyDescription);
            double y = (double)value * 2 - 1;
            return y >= 0 ? a * Math.Exp(k * y) + b : z - (a * Math.Exp(-k * y) + b);
        }
    }
}
