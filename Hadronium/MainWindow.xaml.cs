using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
//using System.Runtime.Serialization.Formatters.Soap;
#if Model3D
using System.Windows.Media.Media3D;
#endif
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System;

namespace Hadronium
{


  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private const string modelFileExt = ".hadronium";
    private Model model = new Model(2);
    private ModelControl modelControl;
    private StackPanel tunePanel = new StackPanel();

    public static RoutedCommand NewCmd = new RoutedCommand();
    public static RoutedCommand ClearCmd = new RoutedCommand();
    public static RoutedCommand LoadCmd = new RoutedCommand();
    public static RoutedCommand RandomizeCmd = new RoutedCommand();
    public static RoutedCommand AddParticlesCmd = new RoutedCommand();
    public static RoutedCommand StartCmd = new RoutedCommand();
    public static RoutedCommand StopCmd = new RoutedCommand();
    public static RoutedCommand PinCmd = new RoutedCommand();
    public static RoutedCommand UnpinCmd = new RoutedCommand();
    public static RoutedCommand LinkCmd = new RoutedCommand();
    public static RoutedCommand UnlinkCmd = new RoutedCommand();

    private static PropertyDescription[] modelPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription("TimeScale"         ,   1.0, 0.01, 1000.0, new LogarithmicConverter(), "RealTimeScale"),
            new PropertyDescription("ParticleAttraction",  -1.0, -1E3, 1E3, new BiLogarithmicConverter(1)),
            new PropertyDescription("LinkAttraction"    ,  10.0, -1E4, 1E4, new BiLogarithmicConverter(10)),
            new PropertyDescription("StretchAttraction" ,   0.0, -1E4, 1E4, new BiLogarithmicConverter(1)),
            new PropertyDescription("Gravity"           ,   0.0, -1E3, 1E3, new BiLogarithmicConverter(1)),
            new PropertyDescription("Viscosity"         ,  10.0,  0.0, 1000.0, new LogarithmicConverter()),
            new PropertyDescription("Accuracy"          ,  50.0,  0.1,  1E5, new LogarithmicConverter()),
        };
    private static PropertyDescription[] controlPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription("ParticleSize"      ,   8.0, 0.8, 800.0, new LogarithmicConverter()),
            new PropertyDescription("TextSize"          ,  12.0, 0.12, 1200.0, new LogarithmicConverter()),
            new PropertyDescription("RefreshPeriod"     , 0.035, 0.0035, 0.35, new LogarithmicConverter(), "RenderElapsedTime"),
        };

    private static PropertyDescription[] performancePropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription("StepCount"         ,   1.0, 0.001, 1E5),
            new PropertyDescription("StepElapsedTime"   ,   1.0, 0.001, 1E5,  new LogarithmicConverter()),
            new PropertyDescription("RealTimeScale"     ,   1.0, 0.01, 1000.0, new LogarithmicConverter()),
            new PropertyDescription("RenderElapsedTime"  ,   1.0, 1E-10, 1E5,  new LogarithmicConverter())
        };

    private void addControls(object source, PropertyDescription[] propertyDescriptions, bool statistics, Color color, string header)
    {
      var expander = new Expander();
      expander.Header = header;
      expander.IsExpanded = true;
      tunePanel.Children.Add(expander);
      var expanderContent = new StackPanel();
      expander.Content = expanderContent;
      expander.Background = new SolidColorBrush(color);

      foreach (var x in propertyDescriptions)
      {
        var propertyPanel = new StackPanel();
//        propertyPanel.Margin = new Thickness(0, 0, 0, 4);
        propertyPanel.Orientation = Orientation.Vertical;
        propertyPanel.Background = new SolidColorBrush(Colors.White);
        expanderContent.Children.Add(propertyPanel);

        var textPanel = new StackPanel();
        textPanel.Orientation = Orientation.Horizontal;
        propertyPanel.Children.Add(textPanel);

        var label = new Label();
        label.Content = x.Name;
        textPanel.Children.Add(label);

        if (statistics)
        {
          var label2 = new Label();
          Binding textBinding = new Binding(x.Name);
          textBinding.Source = source;
          label2.SetBinding(Label.ContentProperty, textBinding);
          textPanel.Children.Add(label2);
        }
        else
        {
          var textBox = new TextBox();
          Binding textBinding = new Binding(x.Name);
          textBinding.Source = source;
          textBinding.StringFormat = "{0:0.000}";
          textBox.SetBinding(TextBox.TextProperty, textBinding);
          textPanel.Children.Add(textBox);

          var slider = new Slider();
          slider.Tag = x;
          if (x.Converter == null)
          {
            slider.Minimum = (double)x.Minimum;
            slider.Maximum = (double)x.Maximum;
          }
          else
          {
            slider.Minimum = 0;
            slider.Maximum = 1;
          }
          slider.Orientation = Orientation.Horizontal;
//          slider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
          //slider.TickFrequency = 0.5;
          //slider.IsSnapToTickEnabled = false;

          Binding sliderBinding = new Binding(x.Name);
          sliderBinding.Converter = x.Converter;
          sliderBinding.Source = source;
          sliderBinding.ConverterParameter = x;
          slider.SetBinding(Slider.ValueProperty, sliderBinding);

          slider.MouseDoubleClick += slider_MouseDoubleClick;

          propertyPanel.Children.Add(slider);

          if (x.FeedbackPropertyName != null)
          {
            slider.IsSelectionRangeEnabled = true;
            slider.SelectionStart = 0;
            slider.SelectionEnd = 0;

            Binding sliderBinding2 = new Binding(x.FeedbackPropertyName);
            sliderBinding2.Converter = x.Converter;
            sliderBinding2.Source = source;
            sliderBinding2.ConverterParameter = x;
            slider.SetBinding(Slider.SelectionEndProperty, sliderBinding2);
          }
        }
      }
    }

    private void slider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var slider = sender as Slider;
      Binding binding = slider.GetBindingExpression(Slider.ValueProperty).ParentBinding;
      var propDescr = binding.ConverterParameter as PropertyDescription;
      var value = binding.Converter.Convert(propDescr.DefaultValue, typeof(double), propDescr, CultureInfo.InvariantCulture);
      (sender as Slider).Value = (double)value;
    }

    private void createTunePanel()
    {
      if (tunePanel != null)
        controlPanel.Children.Remove(tunePanel);
      tunePanel = new StackPanel();
      tunePanel.Orientation = Orientation.Vertical;
      controlPanel.Children.Add(tunePanel);

      addControls(model, modelPropertyDescriptions, false, Color.FromRgb(255, 240, 230), "Model");
      addControls(modelControl, controlPropertyDescriptions, false, Color.FromRgb(240, 230, 255), "View");
      addControls(model, performancePropertyDescriptions, true, Color.FromRgb(230, 255, 240), "Performance");
    }

    public MainWindow()
    {
      InitializeComponent();
      modelControl = new ModelControl();
      modelControl.ClipToBounds = true;
      modelPlaceholder.Content = modelControl;
#if NativeEngine
            Title += " (native engine)";
#else
      Title += " (managed engine)";
#endif
      try
      {
        loadModelFromFile(autosaveFilename);
      }
      catch
      {
        model = new Model(2);
      }
      modelRecreated();
    }


    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      model.Stop();
      saveModelToFile(autosaveFilename);
    }

    private void NewCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      model = new Model(int.Parse(e.Parameter.ToString()));
      modelRecreated();
    }
    private void NewCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !model.Active;
    }

    private void ClearCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.Model.Clear();
      modelRecreated();
    }

    private void ClearCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !model.Active;
    }
    
    private void AddParticlesCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      var dialog = new ParticleGenerationDialog();
      dialog.ParticleCount = 5;
      dialog.LinkCount = 10;
      if(dialog.ShowDialog() == true)
      {
        modelControl.NewRandomModel(dialog.ParticleCount, dialog.LinkCount);
        modelRecreated();
      }
    }
    private void AddParticlesCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !model.Active;
    }
    private void RandomizeCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.RandomizePositions();
    }
    private void RandomizeCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
    }
    private void StartCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      model.Start();
    }
    private void StartCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !model.Active;
    }
    private void StopCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      model.Stop();
    }
    private void StopCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = model.Active;
    }

    private XmlNode write(XmlNode node, string name, string value, string defValue = null)
    {
      if (value == defValue)
        return null;
      var result = node.OwnerDocument.CreateAttribute(name);
      result.Value = value;
      return node.Attributes.Append(result);
    }
    private string doubleToString(double x)
    {
      return x.ToString(CultureInfo.InvariantCulture);
    }

    private static String[] CoordNames = new String[] { "X", "Y", "Z" };

    private XmlNode write(XmlNode node, string name, double[] value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      for (int i = 0; i < value.Length; i++)
        write(result, CoordNames[i], doubleToString(value[i]));
      return result;
    }


    private XmlNode write(XmlNode node, string name, Color value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "R", value.R.ToString());
      write(result, "G", value.G.ToString());
      write(result, "B", value.B.ToString());
      write(result, "A", value.A.ToString(), "255");
      return result;
    }
    private XmlNode write(XmlNode node, string name, Particle value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "Id", value.Name, null);
      write(result, "Mass", doubleToString(value.Mass), "1");
      write(result, "Position", value.Position);
      write(result, "Velocity", value.Velocity);
      write(result, "FillColor", value.FillColor);
      write(result, "StrokeColor", value.StrokeColor);
      return result;
    }
    private void writeParticleRef(XmlNode node, string name, Model model, Particle particle)
    {
      if (!String.IsNullOrEmpty(particle.Name))
        write(node, name + ".Name", particle.Name);
      else
        write(node, name + ".Number", model.GetParticleIndex(particle).ToString());
    }
    private XmlNode write(XmlNode node, PropertyDescription prop, object target)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement("Property"));
      write(result, "Name", prop.Name);
      write(result, "Value", doubleToString((double)prop.GetValue(target)));
      return result;
    }

    private void saveModelToFile(string fileName)
    {
      var doc = new XmlDocument();
      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
      var rootNode = doc.AppendChild(doc.CreateElement("Hadronium"));
      foreach (PropertyDescription prop in modelPropertyDescriptions)
        write(rootNode, prop, model);
      foreach (PropertyDescription prop in controlPropertyDescriptions)
        write(rootNode, prop, modelControl);

      foreach (var p in model.Particles)
        write(rootNode, "Particle", p);
      foreach (var l in model.Links)
      {
        var linkNode = rootNode.AppendChild(doc.CreateElement("Link"));
        writeParticleRef(linkNode, "A", model, l.A);
        writeParticleRef(linkNode, "B", model, l.B);
        write(linkNode, "Strength", l.Strength.ToString(), "1");
      }
      doc.Save(fileName);
    }


    private void exportModelToFile(string fileName)
    {
      using (FileStream outStream = new FileStream(fileName, FileMode.Create))
      {
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(modelControl.RenderToBitmap()));
        encoder.Save(outStream);
      }
    }

    private string read(XmlNode node, string attrName, string defValue = null)
    {
      var attr = node.Attributes[attrName];
      if (attr == null)
        return defValue;
      return attr.Value;
    }

    private double stringToDouble(string s)
    {
      return double.Parse(s, CultureInfo.InvariantCulture);
    }
    private void read(XmlNode node, ref double[] result)
    {
      if (node == null)
        return;
      for (int i = 0; i < result.Length; i++)
        result[i] = stringToDouble(read(node, CoordNames[i]));
    }
    
    private void read(XmlNode node, ref Color result)
    {
      if (node == null)
        return;
      result.R = byte.Parse(read(node, "R"));
      result.G = byte.Parse(read(node, "G"));
      result.B = byte.Parse(read(node, "B"));
      result.A = byte.Parse(read(node, "A", "255"));
    }

    private Particle readParticle(XmlNode node)
    {
      var result = new Particle(model.Dimension);
      result.Name = read(node, "Id");
      result.Mass = stringToDouble(read(node, "Mass", "1"));
      read(node.SelectSingleNode("Position"), ref result.Position);
      read(node.SelectSingleNode("Velocity"), ref result.Velocity);
      read(node.SelectSingleNode("FillColor"), ref result.FillColor);
      read(node.SelectSingleNode("StrokeColor"), ref result.StrokeColor);
      return result;
    }
    private Particle readParticleRef(XmlNode node, string name, Model model)
    {
      var attr = node.Attributes[name + ".Name"];
      if (attr != null)
        return model.GetParticle(attr.Value);
      attr = node.Attributes[name + ".Number"];
      if (attr != null)
        return model.Particles[int.Parse(attr.Value)];
      throw new Exception("Link read error");
    }
    private void read(XmlNode node, PropertyDescription prop, object target)
    {
      XmlNode propNode = node.SelectSingleNode(string.Format("Property[@Name='{0}']", prop.Name));
      if (propNode != null)
        prop.SetValue(target, stringToDouble(propNode.Attributes["Value"].Value));
    }

    private void loadModelFromFile(string fileName)
    {
      var doc = new XmlDocument();
      doc.Load(fileName);
      model = new Model(2);
      var rootNode = doc.DocumentElement;
      foreach (XmlNode particleNode in rootNode.SelectNodes("Particle"))
      {
        model.Particles.Add(readParticle(particleNode));
      }
      foreach (PropertyDescription prop in modelPropertyDescriptions)
        read(rootNode, prop, model);
      foreach (PropertyDescription prop in controlPropertyDescriptions)
        read(rootNode, prop, modelControl);
      foreach (XmlNode linkNode in rootNode.SelectNodes("Link"))
      {
        var link = new Link(
          readParticleRef(linkNode, "A", model), 
          readParticleRef(linkNode, "B", model), 
          double.Parse(read(linkNode, "Strength", "1")));
        model.Links.Add(link);
      }
    }

    private void importModelFromTextFile(string fileName)
    {
      var reader = new StreamReader(fileName);
      model = new Model(2);
      //			string content = reader.ReadToEnd();
      //            string[] lines = content.Split('\n');
      //            foreach(var line in lines)
      //            {
      //                items[
      //            }
      while (!reader.EndOfStream)
      {
        string line = reader.ReadLine();
        if (line.Trim() != "")
        {
          string[] items = line.Split('\t');
          if (items.Length > 0)
          {
            var particle = model.FindParticle(items[0]);
            if (particle == null)
            {
              particle = new Particle(model.Dimension);
              particle.Name = items[0];
              model.Particles.Add(particle);
            }
            if (items.Length > 1)
            {
              var link = new Link(particle, model.GetParticle(items[1]));
              model.Links.Add(link);
            }
          }

        }
      }
      model.RandomizePositions(modelControl.getInitialRect());
    }

    private void modelRecreated()
    {
      modelControl.Model = model;
      createTunePanel();
      modelControl.InvalidateVisual();
    }

    private void prepareDialog(FileDialog fileDialog, bool forSave)
    {
      fileDialog.DefaultExt = modelFileExt;
      fileDialog.Filter = string.Format("Hadronium files ({0})|*{0}", modelFileExt);
      if (forSave)
      {
        fileDialog.Filter += "|Png bitmaps (.png)|*.png";
      }
      else
      {
        fileDialog.Filter += "|Txt files (.txt)|*.txt";
      }
    }

    private string autosaveFilename
    {
      get
      {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hadronium");
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, "Autosave" + modelFileExt);

      }
    }

    private void SaveCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      SaveFileDialog fileDialog = new SaveFileDialog();
      prepareDialog(fileDialog, true);
      if (fileDialog.ShowDialog(this).Value)
      {
        switch (fileDialog.FilterIndex)
        {
          case 1:
            saveModelToFile(fileDialog.FileName);
            break;
          case 2:
            exportModelToFile(fileDialog.FileName);
            break;
        }
      }
    }

    private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      OpenFileDialog fileDialog = new OpenFileDialog();
      prepareDialog(fileDialog, false);
      if (fileDialog.ShowDialog(this).Value)
      {
        switch (fileDialog.FilterIndex)
        {
          case 1:
            loadModelFromFile(fileDialog.FileName);
            break;
          case 2:
            importModelFromTextFile(fileDialog.FileName);
            break;
        }
        modelRecreated();
      }
    }


    private bool rotating = false;
    private Point rotationStart;
    private void Wheel_MouseDown(object sender, MouseButtonEventArgs e)
    {
      rotating = true;
      rotationStart = e.GetPosition(Wheel);
      Wheel.CaptureMouse();
    }

    private void Wheel_MouseMove(object sender, MouseEventArgs e)
    {
#if Model3D
            if (rotating)
            {
                Vector3D r = modelControl.Rotation;
                Vector delta = e.GetPosition(Wheel) - rotationStart;
                r.X += delta.Y;
                r.Y += -delta.X;
                modelControl.Rotation = r;
                rotationStart = e.GetPosition(Wheel);
            }
#endif
    }

    private void Wheel_MouseUp(object sender, MouseButtonEventArgs e)
    {
      rotating = false;
      Wheel.ReleaseMouseCapture();
    }

    private void PinCmd_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.Pin(true);
    }

    private void PinCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = modelControl != null && modelControl.CanPin(true);
    }
    private void UnpinCmd_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.Pin(false);
    }

    private void UnpinCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = modelControl != null && modelControl.CanPin(false);
    }

    private void LinkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.Link(true);
    }

    private void LinkCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = modelControl != null && modelControl.CanLink(true);
    }

    private void UnlinkCmd_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.Link(false);
    }

    private void UnlinkCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = modelControl != null && modelControl.CanLink(false);
    }

  }
}
