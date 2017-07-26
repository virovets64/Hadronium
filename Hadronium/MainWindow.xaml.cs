using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private StackPanel tunePanel = new StackPanel();

        public static RoutedCommand NewCmd = new RoutedCommand();
        public static RoutedCommand ClearCmd = new RoutedCommand();
        public static RoutedCommand LoadCmd = new RoutedCommand();

        private static PropertyDescription[] modelPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription(SourceKind.Model, "TimeScale"         ,   1.0, 0.01, 1000.0, new LogarithmicConverter(), "RealTimeScale"),
            new PropertyDescription(SourceKind.Model, "ParticleAttraction",  -1.0, -1E3, 1E3, new BiLogarithmicConverter(1)),
            new PropertyDescription(SourceKind.Model, "LinkAttraction"    ,  10.0, -1E4, 1E4, new BiLogarithmicConverter(10)),
            new PropertyDescription(SourceKind.Model, "StretchAttraction" ,   0.0, -1E4, 1E4, new BiLogarithmicConverter(1)),
            new PropertyDescription(SourceKind.Model, "Gravity"           ,   0.0, -1E3, 1E3, new BiLogarithmicConverter(1)),
            new PropertyDescription(SourceKind.Model, "Viscosity"         ,  10.0,  0.0, 1000.0, new LogarithmicConverter()),
            new PropertyDescription(SourceKind.Model, "Accuracy"          ,  50.0,  0.1,  1E5, new LogarithmicConverter()),
        };
        private static PropertyDescription[] controlPropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription(SourceKind.View, "Rotation"          ,   0.0, -180.0, 180.0),
            new PropertyDescription(SourceKind.View, "ParticleSize"      ,   8.0, 0.8, 800.0, new LogarithmicConverter()),
            new PropertyDescription(SourceKind.View, "TextSize"          ,  12.0, 0.12, 1200.0, new LogarithmicConverter()),
            new PropertyDescription(SourceKind.View, "RefreshPeriod"     , 0.035, 0.0035, 0.35, new LogarithmicConverter(), "RenderElapsedTime"),
        };

        private static PropertyDescription[] performancePropertyDescriptions = new PropertyDescription[] { 
            new PropertyDescription(SourceKind.Model, "StepCount"         ,   1.0, 0.001, 1E5),
            new PropertyDescription(SourceKind.Model, "StepElapsedTime"   ,   1.0, 0.001, 1E5,  new LogarithmicConverter()),
            new PropertyDescription(SourceKind.Model, "RealTimeScale"     ,   1.0, 0.01, 1000.0, new LogarithmicConverter()),
            new PropertyDescription(SourceKind.View, "RenderElapsedTime" ,   1.0, 1E-5, 1E5,  new LogarithmicConverter())
        };

        private void AddControls(PropertyDescription[] propertyDescriptions, bool statistics, Color color, string header)
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
                object source = x.Kind == SourceKind.Model ? (object)model : (object)modelControl;
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
                    var textBox = new TextBox();
                    textBox.IsReadOnly = true;
                    textBox.VerticalAlignment = VerticalAlignment.Center;
                    Binding textBinding = new Binding(x.Name);
                    textBinding.Source = source;
                    textBinding.StringFormat = "{0:G4}";
                    textBox.SetBinding(TextBox.TextProperty, textBinding);
                    textPanel.Children.Add(textBox);
                }
                else
                {
                    var textBox = new TextBox();
                    textBox.VerticalAlignment = VerticalAlignment.Center;
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
            var converter = binding.Converter;
            if (converter != null)
                (sender as Slider).Value = (double)converter.Convert(propDescr.DefaultValue, typeof(double), propDescr, CultureInfo.InvariantCulture);
            else
                (sender as Slider).Value = (double)propDescr.DefaultValue;
        }

        private void CreateTunePanel()
        {
            if (tunePanel != null)
                controlPanel.Children.Remove(tunePanel);
            tunePanel = new StackPanel();
            tunePanel.Orientation = Orientation.Vertical;
            controlPanel.Children.Add(tunePanel);

            AddControls(modelPropertyDescriptions, false, Color.FromRgb(255, 240, 230), "Model");
            AddControls(controlPropertyDescriptions, false, Color.FromRgb(240, 230, 255), "View");
            AddControls(performancePropertyDescriptions, true, Color.FromRgb(230, 255, 240), "Performance");
        }

        public MainWindow()
        {
            InitializeComponent();
//            modelControl = new ModelControl();
            modelControl.ClipToBounds = true;
//            modelPlaceholder.Content = modelControl;
            try
            {
                LoadModelFromFile(autosaveFilename);
            }
            catch
            {
                model = new Model(2);
            }
            ModelRecreated();
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            model.Stop();
            SaveModelToFile(autosaveFilename);
        }

        private void NewCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            model = new Model(int.Parse(e.Parameter.ToString()));
            ModelRecreated();
        }

        private void NewCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !model.Active;
        }

        private void SaveCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            PrepareDialog(fileDialog, true);
            if (fileDialog.ShowDialog(this).Value)
            {
                switch (fileDialog.FilterIndex)
                {
                    case 1:
                        SaveModelToFile(fileDialog.FileName);
                        break;
                    case 2:
                        ExportModelToFile(fileDialog.FileName);
                        break;
                }
            }
        }

        private void OpenCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            PrepareDialog(fileDialog, false);
            if (fileDialog.ShowDialog(this).Value)
            {
                switch (fileDialog.FilterIndex)
                {
                    case 1:
                        LoadModelFromFile(fileDialog.FileName);
                        break;
                    case 2:
                        ImportModelFromTextFile(fileDialog.FileName);
                        break;
                }
                ModelRecreated();
            }
        }

        private void ClearCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            modelControl.Model.Clear();
            ModelRecreated();
        }

        private void ClearCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !model.Active;
        }

        private void SaveModelToFile(string fileName)
        {
            var properties = modelPropertyDescriptions.Select(x => new PropertyInstance(x, model))
              .Concat(controlPropertyDescriptions.Select(x => new PropertyInstance(x, modelControl)));

            XmlSerializer.WriteModel(fileName, model, properties);
        }

        private void ExportModelToFile(string fileName)
        {
            using (FileStream outStream = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(modelControl.RenderToBitmap()));
                encoder.Save(outStream);
            }
        }

        private void LoadModelFromFile(string fileName)
        {
            var properties = modelPropertyDescriptions.Select(x => new PropertyInstance(x, model))
              .Concat(controlPropertyDescriptions.Select(x => new PropertyInstance(x, modelControl)));

            model = XmlSerializer.ReadModel(fileName, properties);
        }

        private void ImportModelFromTextFile(string fileName)
        {
            var reader = new StreamReader(fileName);
            model = new Model(2);
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
                            model.AddParticle(particle);
                        }
                        if (items.Length > 1)
                        {
                            var link = new Link(particle, model.GetParticle(items[1]));
                            model.AddLink(link);
                        }
                    }

                }
            }
            model.RandomizePositions(modelControl.GetInitialRect());
        }

        private void ModelRecreated()
        {
            modelControl.Model = model;
            CreateTunePanel();
            modelControl.InvalidateVisual();
        }

        private void PrepareDialog(FileDialog fileDialog, bool forSave)
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
        }

        private void Wheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            rotating = false;
            Wheel.ReleaseMouseCapture();
        }
    }
}
