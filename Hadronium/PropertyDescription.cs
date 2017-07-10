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
    
    void initialize(object parameter)
    {
      if (exponent == null)
      {
        var pd = parameter as PropertyDescription;
        exponent = new Exponent((double)pd.Minimum, (double)pd.Maximum, (double)pd.DefaultValue);
      }
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      initialize(parameter);
      return exponent.Inverse((double)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      initialize(parameter);
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

    void initialize(object parameter)
    {
      if (exponent == null)
      {
        var pd = parameter as PropertyDescription;
        exponent = new Exponent((double)pd.DefaultValue, (double)pd.Maximum, quarter);
        y0 = (double)pd.DefaultValue;
      }
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      initialize(parameter);

      double y = (double)value;
      if (y > y0)
        return (exponent.Inverse(y) + 1) / 2;
      else
        return (-exponent.Inverse(2 * y0 - y) + 1) / 2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      initialize(parameter);
      double x = (double)value * 2 - 1;
      return x >= 0 ? exponent.Direct(x) : 2 * y0 - exponent.Direct(-x);
    }
  }
}
