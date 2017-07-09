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
#if Model3D
        private const string modelFileExt = ".hadronium3D";
#else
    private const string modelFileExt = ".hadronium";
#endif
    private Model model = new Model();
    private ModelControl modelControl;
    private StackPanel tunePanel = new StackPanel();

    public static RoutedCommand NewCmd = new RoutedCommand();
    public static RoutedCommand LoadCmd = new RoutedCommand();
    public static RoutedCommand RandomizeCmd = new RoutedCommand();
    public static RoutedCommand AddParticlesCmd = new RoutedCommand();
    public static RoutedCommand StartCmd = new RoutedCommand();
    public static RoutedCommand StopCmd = new RoutedCommand();
    public static RoutedCommand PinCmd = new RoutedCommand();
    public static RoutedCommand UnpinCmd = new RoutedCommand();

    private static PropertyDescription[] modelPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription("TimeScale"         ,   1.0, 0.01, 1000.0, new LogarithmicConverter(1), "RealTimeScale"),
            new PropertyDescription("ParticleAttraction", -30.0, -1E5, 1E5, new BiLogarithmicConverter(20)),
            new PropertyDescription("LinkAttraction"    ,  10.0, -1E5, 1E5, new BiLogarithmicConverter(20)),
            new PropertyDescription("StretchAttraction" ,   0.0, -1E5, 1E5, new BiLogarithmicConverter(20)),
            new PropertyDescription("Viscosity"         ,   0.1, 0.0, 1000.0, new LogarithmicConverter(30)),
            new PropertyDescription("Accuracy"          ,   5.0, 0.1,  1E5, new LogarithmicConverter(1)),
        };
    private static PropertyDescription[] controlPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription("ViewScale"         ,   1.0, 1E-5, 1E5, new LogarithmicConverter(1)),
            new PropertyDescription("ParticleSize"      ,   8.0, 0.1, 100.0, new LogarithmicConverter(1)),
            new PropertyDescription("TextSize"          ,   12.0, 0.1, 100.0, new LogarithmicConverter(1)),
            new PropertyDescription("RefreshPeriod"     ,   0.035, 1E-10, 10.0, new LogarithmicConverter(0.01), "RenderElapsedTime"),
        };

    private static PropertyDescription[] modelStatisticsDescriptions = new PropertyDescription[] { 
            new PropertyDescription("StepCount"   ,   1.0, 0.001, 1E5),
            new PropertyDescription("StepElapsedTime"   ,   1.0, 0.001, 1E5,  new LogarithmicConverter(10)),
            new PropertyDescription("RealTimeScale"         ,   1.0, 0.01, 1000.0, new LogarithmicConverter(10)),
        };
    private static PropertyDescription[] controlStatisticsDescriptions = new PropertyDescription[] { 
            new PropertyDescription("RenderElapsedTime"   ,   1.0, 1E-10, 1E5,  new LogarithmicConverter(10)),
        };

    private void addControls(object source, PropertyDescription[] propertyDescriptions, bool statistics)
    {
      foreach (var x in propertyDescriptions)
      {
        var stackPanel = new StackPanel();
        stackPanel.Orientation = Orientation.Horizontal;
        tunePanel.Children.Add(stackPanel);

        var label = new Label();
        label.Content = x.Name;
        stackPanel.Children.Add(label);

        if (statistics)
        {
          var label2 = new Label();
          Binding textBinding = new Binding(x.Name);
          textBinding.Source = source;
          label2.SetBinding(Label.ContentProperty, textBinding);
          stackPanel.Children.Add(label2);
        }
        else
        {
          var textBox = new TextBox();
          Binding textBinding = new Binding(x.Name);
          textBinding.Source = source;
          textBox.SetBinding(TextBox.TextProperty, textBinding);
          stackPanel.Children.Add(textBox);

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

          Binding sliderBinding = new Binding(x.Name);
          sliderBinding.Converter = x.Converter;
          sliderBinding.Source = source;
          sliderBinding.ConverterParameter = x;
          slider.SetBinding(Slider.ValueProperty, sliderBinding);
          tunePanel.Children.Add(slider);

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

            //                        model.PropertyChanged +=new System.ComponentModel.PropertyChangedEventHandler(
            //                            (Object sender, PropertyChangedEventArgs e) => 
            //                            {
            ////                                slider.SelectionEnd = x.Converter.Convert(e.
            //                            });                        
          }

        }
      }
    }

    private void createTunePanel()
    {
      if (tunePanel != null)
        controlPanel.Children.Remove(tunePanel);
      tunePanel = new StackPanel();
      tunePanel.Orientation = Orientation.Vertical;
      controlPanel.Children.Add(tunePanel);

      addControls(modelControl, controlPropertyDescriptions, false);
      addControls(model, modelPropertyDescriptions, false);
      addControls(model, modelStatisticsDescriptions, true);
      addControls(modelControl, controlStatisticsDescriptions, true);
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
        model = new Model();
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
      try
      {
        modelControl.Model.Clear();
        modelRecreated();
      }
      catch (Exception x)
      {
        MessageBox.Show(x.Message);
      }
    }
    private void NewCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !model.Active;
    }
    private void LoadCmdExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      modelControl.LoadProjectsXml();
      modelRecreated();
    }
    private void LoadCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
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

    private XmlNode write(XmlNode node, string name, Point value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "X", doubleToString(value.X));
      write(result, "Y", doubleToString(value.Y));
      return result;
    }

    private XmlNode write(XmlNode node, string name, Vector value)
    {
      return write(node, name, (Point)value);
    }

#if Model3D
    private XmlNode write(XmlNode node, string name, Point3D value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "X", doubleToString(value.X));
      write(result, "Y", doubleToString(value.Y));
      write(result, "Z", doubleToString(value.Z));
      return result;
    }

    private XmlNode write(XmlNode node, string name, Vector3D value)
    {
      return write(node, name, (Point3D)value);
    }
#endif    
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
    private void writeParticleRef(XmlNode node, string name, Model model, int index)
    {
      if (model.Particles[index].Name != null && model.Particles[index].Name != "")
        write(node, name + ".Name", model.Particles[index].Name);
      else
        write(node, name + ".Number", index.ToString());
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
    private void read(XmlNode node, ref Point result)
    {
      if (node == null)
        return;
      result.X = stringToDouble(read(node, "X"));
      result.Y = stringToDouble(read(node, "Y"));
    }
    private void read(XmlNode node, ref Vector result)
    {
      if (node == null)
        return;
      result.X = stringToDouble(read(node, "X"));
      result.Y = stringToDouble(read(node, "Y"));
    }
#if Model3D
    private void read(XmlNode node, ref Point3D result)
    {
      if (node == null)
        return;
      result.X = stringToDouble(read(node, "X"));
      result.Y = stringToDouble(read(node, "Y"));
      result.Z = stringToDouble(read(node, "Z"));
    }
    private void read(XmlNode node, ref Vector3D result)
    {
      if (node == null)
        return;
      result.X = stringToDouble(read(node, "X"));
      result.Y = stringToDouble(read(node, "Y"));
      result.Z = stringToDouble(read(node, "Z"));
    }
#endif
    
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
      var result = new Particle();
      result.Name = read(node, "Id");
      result.Mass = stringToDouble(read(node, "Mass", "1"));
      read(node.SelectSingleNode("Position"), ref result.Position);
      read(node.SelectSingleNode("Velocity"), ref result.Velocity);
      read(node.SelectSingleNode("FillColor"), ref result.FillColor);
      read(node.SelectSingleNode("StrokeColor"), ref result.StrokeColor);
      return result;
    }
    private int readParticleRef(XmlNode node, string name, Model model)
    {
      var attr = node.Attributes[name + ".Name"];
      if (attr != null)
        return model.GetParticleIndex(attr.Value);
      attr = node.Attributes[name + ".Number"];
      if (attr != null)
        return int.Parse(attr.Value);
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
      model = new Model();
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
        var link = new Link();
        link.A = readParticleRef(linkNode, "A", model);
        link.B = readParticleRef(linkNode, "B", model);
        link.Strength = double.Parse(read(linkNode, "Strength", "1"));
        model.Links.Add(link);
      }
    }

    private void importModelFromTextFile(string fileName)
    {
      var reader = new StreamReader(fileName);
      model = new Model();
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
            int index = model.FindParticleIndex(items[0]);
            if (index == -1)
            {
              var p = new Particle();
              p.Name = items[0];
              model.Particles.Add(p);
              index = model.Particles.Count - 1;
            }
            if (items.Length > 1)
            {
              var link = new Link(index, model.GetParticleIndex(items[1]));
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

    //[Serializable]
    //private class ApplicationState
    //{
    //  public Model Model;
    //  public double ViewScale;
    //  public Vector Offset;
    //  public double RefreshPeriod;
    //  public double ParticleSize;
    //  public double TextSize;
    //}

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

  }
}
