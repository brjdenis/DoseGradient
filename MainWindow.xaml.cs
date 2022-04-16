using EvilDICOM.Core;
using EvilDICOM.Core.Element;
using EvilDICOM.Core.Helpers;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DoseGradient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string DoseFilePath;
        public List<string> ImageFilesList = new List<string>() { };

        public uint[,,] DoseMatrix;
        public List<double[,]> DoseMatrixList = new List<double[,]>() { };
        public List<double[,]> ImageMatrixList = new List<double[,]>() { };

        public PlotModel PlotModelImageDose { get; set; }
        public HeatMapSeries DoseImageHeatMapSeries = new HeatMapSeries { };
        public LinearColorAxis DoseImageHeatMapAxis = new LinearColorAxis { Palette = OxyPalettes.Jet(1024), LowColor = OxyColors.Black, Position = AxisPosition.None };

        public PlotModel PlotModelProfile { get; set; }

        public int DoseCurrentSliceIndex = 0;
        public int ImageCurrentSliceIndex = 0;

        public List<double> ImagePixelSpacing;
        public List<double> DosePixelSpacing;
        public double DoseScaling;
        public int bits32_16;
        public List<double> ImageWindowLevel;
        public List<double> DoseImagePositionPatient;
        public List<List<double>> ImageImagePositionPatient;

        public List<double> gradientXPositions = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public List<double> gradientDoses = new List<double>() { 0, 0, 30.6, 30.6, 0, 0, 30.6, 30.6, 0, 0 };

        public double trackerXPos;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            InitializeComponent();

            this.SizeChanged += LayoutUpdate;

            this.DoseImagePlotTypeComboBox.ItemsSource = new List<string>() { "Dose", "Image" };

            AddPlotModelToDoseImagePlot();
            AddPlotModelToProfilePlot();

            this.PlotModelImageDose.TrackerChanged += SynchroniseTrackerLineImageDose;
            this.PlotModelProfile.TrackerChanged += SynchroniseTrackerLineProfile;

            this.PlotModelImageDose.KeyDown += CatchTrackerPosition;

        }

        private void SynchroniseTrackerLineImageDose(Object sender, TrackerEventArgs args)
        {
            if (args.HitResult != null)
            {
                var currentResult = args.HitResult;
                var currentPosition = currentResult.Position;

                LineSeries lineseries = this.PlotModelProfile.Series.First(x => (string)x.Tag == "LineSeries") as LineSeries;

                DataPoint dataPosition = this.DoseImageHeatMapSeries.InverseTransform(currentPosition);

                ScreenPoint screenPositionProfile = lineseries.Transform(new DataPoint(dataPosition.X, GetDoseFromProfileAtPosition(dataPosition.X)));
                TrackerHitResult result = lineseries.GetNearestPoint(screenPositionProfile, true);

                this.OxyPlotProfile.ShowTracker(result);
                this.trackerXPos = dataPosition.X;
            }
            else
            {
                this.OxyPlotProfile.HideTracker();
            }
        }


        private void SynchroniseTrackerLineProfile(Object sender, TrackerEventArgs args)
        {
            if (args.HitResult != null)
            {
                var currentResult = args.HitResult;
                var currentPosition = currentResult.Position;

                double middle = (this.DoseImageHeatMapSeries.Y0 + this.DoseImageHeatMapSeries.Y1) / 2.0;
                LineSeries lineseries = this.PlotModelProfile.Series.First(x => (string)x.Tag == "LineSeries") as LineSeries;

                DataPoint dataPosition = lineseries.InverseTransform(currentPosition);
                ScreenPoint screenPositionDoseImage = this.DoseImageHeatMapSeries.Transform(new DataPoint(dataPosition.X, middle));
                TrackerHitResult result = this.DoseImageHeatMapSeries.GetNearestPoint(screenPositionDoseImage, true);

                this.OxyPlotImageDose.ShowTracker(result);
                this.trackerXPos = dataPosition.X;
            }
            else
            {
                this.OxyPlotImageDose.HideTracker();
            }
        }

        private void CatchTrackerPosition(object sender, OxyKeyEventArgs e)
        {
            if (e.Key == OxyKey.D1)
            {
                this.TextBoxP1.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D2)
            {
                this.TextBoxP2.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D3)
            {
                this.TextBoxP3.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D4)
            {
                this.TextBoxP4.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D5)
            {
                this.TextBoxP5.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D6)
            {
                this.TextBoxP6.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D7)
            {
                this.TextBoxP7.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            else if (e.Key == OxyKey.D8)
            {
                this.TextBoxP8.Text = this.trackerXPos.ToString("F2", new CultureInfo("en-US"));
            }
            TextBoxLostFocus(null, null);
        }


        private void SetPlotTypeComboBox()
        {
            this.DoseImagePlotTypeComboBox.SelectedIndex = 0;
        }

        private void AddPlotModelToDoseImagePlot()
        {
            var plotModel = new PlotModel { PlotType = PlotType.Cartesian };
            plotModel.Axes.Add(this.DoseImageHeatMapAxis);
            //dummy axes for titles:
            LinearAxis axx = new LinearAxis { Title = "dicom Z [cm]", Position = AxisPosition.Bottom };
            LinearAxis axy = new LinearAxis { Title = "dicom Y [cm]", Position = AxisPosition.Left };
            plotModel.Axes.Add(axx);
            plotModel.Axes.Add(axy);
            CreateSeriesDose(plotModel);
            this.PlotModelImageDose = plotModel;
            this.OxyPlotImageDose.Model = this.PlotModelImageDose;

            this.PlotModelImageDose.Background = OxyColors.Black;
            this.PlotModelImageDose.TextColor = OxyColors.White;
            this.PlotModelImageDose.SelectionColor = OxyColors.White;
            this.PlotModelImageDose.PlotAreaBorderColor = OxyColors.White;
            this.DoseImageHeatMapAxis.AxislineColor = OxyColors.White;
            this.DoseImageHeatMapAxis.TextColor = OxyColors.White;
            this.DoseImageHeatMapAxis.TicklineColor = OxyColors.White;
        }


        public void CreateSeriesDose(PlotModel plotModel)
        {
            var series = new HeatMapSeries
            {
                X0 = 0,
                X1 = 1,
                Y0 = 0,
                Y1 = 1,
                Interpolate = false,
                RenderMethod = HeatMapRenderMethod.Bitmap,
                LabelFormatString = "0.0",
                TrackerFormatString = "{0}\n{1}: {2:0.##}\n{3}: {4:0.##}\n{5}: {6:0.##}",
                Data = new double[0, 0] { }
            };

            var myController = new PlotController();
            this.OxyPlotImageDose.Controller = myController;

            myController.UnbindMouseWheel();

            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.Control);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, 2);

            myController.BindMouseWheel(OxyModifierKeys.Control, OxyPlot.PlotCommands.ZoomWheelFine);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, OxyPlot.PlotCommands.ZoomRectangle);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2, OxyPlot.PlotCommands.ResetAt);
            myController.BindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, OxyPlot.PlotCommands.PanAt);

            this.DoseImageHeatMapSeries = series;
            plotModel.Series.Add(this.DoseImageHeatMapSeries);
        }

        private void SetAxisColors(LinearAxis axis)
        {
            axis.AxislineColor = OxyColors.White;
            axis.AxislineColor = OxyColors.White;
            axis.TextColor = OxyColors.White;
            axis.TextColor = OxyColors.White;
            axis.TicklineColor = OxyColors.White;
            axis.TicklineColor = OxyColors.White;
        }

        private void AddPlotModelToProfilePlot()
        {
            var plotModel = new PlotModel { PlotType = PlotType.XY };

            LinearAxis axisX = new LinearAxis { Title = "dicom Z [cm]", Position = AxisPosition.Bottom, Tag = "AxisX" };
            LinearAxis axisY = new LinearAxis { Title = "dose [Gy]", Position = AxisPosition.Left, Tag = "AxisY" };
            SetAxisColors(axisX);
            SetAxisColors(axisY);

            plotModel.Axes.Add(axisX);
            plotModel.Axes.Add(axisY);

            this.PlotModelProfile = plotModel;
            this.OxyPlotProfile.Model = this.PlotModelProfile;

            this.PlotModelProfile.Background = OxyColors.Black;
            this.PlotModelProfile.TextColor = OxyColors.White;
            this.PlotModelProfile.SelectionColor = OxyColors.White;
            this.PlotModelProfile.PlotAreaBorderColor = OxyColors.White;
            this.PlotModelProfile.TextColor = OxyColors.White;

            var myController = new PlotController();
            this.OxyPlotProfile.Controller = myController;

            myController.UnbindMouseWheel();

            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.Control);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None);
            myController.UnbindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, 2);

            myController.BindMouseWheel(OxyModifierKeys.Control, OxyPlot.PlotCommands.ZoomWheelFine);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, OxyPlot.PlotCommands.ZoomRectangle);
            myController.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.None, 2, OxyPlot.PlotCommands.ResetAt);
            myController.BindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, OxyPlot.PlotCommands.PanAt);
        }


        private void CreateLineSeriesProfilePlot()
        {
            this.PlotModelProfile.Series.Clear();
            this.PlotModelProfile.Annotations.Clear();
            LineSeries lineSeries = new LineSeries
            {
                Tag = "LineSeries",
                Color = OxyColors.White,
                TrackerFormatString = "{0}\n{1}: {2:0.##}\n{3}: {4:0.##}",
                CanTrackerInterpolatePoints = true,

            };

            for (int i = 0; i < this.gradientXPositions.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(this.gradientXPositions[i], this.gradientDoses[i]));

                var textAnnotation = new TextAnnotation
                {
                    Text = i.ToString(),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                    Stroke = OxyColors.Transparent,
                    TextPosition = new DataPoint(this.gradientXPositions[i], this.gradientDoses[i]),
                    Tag = "TextP" + i.ToString(),
                    FontSize = 12,
                    TextColor = OxyColors.White,
                    Offset = new ScreenVector(0, -5)
                };

                this.PlotModelProfile.Annotations.Add(textAnnotation);

                var pointAnnotation = new PointAnnotation
                {
                    X = this.gradientXPositions[i],
                    Y = this.gradientDoses[i],
                    Fill = OxyColors.Yellow
                };
                this.PlotModelProfile.Annotations.Add(pointAnnotation);

            }
            this.PlotModelProfile.Series.Add(lineSeries);

            // Add text for doses
            var textAnnotationD1 = new TextAnnotation
            {
                Text = "D1",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((this.gradientXPositions[0] + this.gradientXPositions[1]) / 2, this.gradientDoses[0]),
                Tag = "TextD1",
                TextColor = OxyColors.White
            };
            var textAnnotationD2 = new TextAnnotation
            {
                Text = "D2",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((this.gradientXPositions[2] + this.gradientXPositions[3]) / 2, this.gradientDoses[2]),
                Tag = "TextD2",
                TextColor = OxyColors.White
            };
            var textAnnotationD3 = new TextAnnotation
            {
                Text = "D3",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((this.gradientXPositions[4] + this.gradientXPositions[5]) / 2, this.gradientDoses[4]),
                Tag = "TextD3",
                TextColor = OxyColors.White
            };
            var textAnnotationD4 = new TextAnnotation
            {
                Text = "D4",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((this.gradientXPositions[6] + this.gradientXPositions[7]) / 2, this.gradientDoses[6]),
                Tag = "TextD4",
                TextColor = OxyColors.White
            };
            var textAnnotationD5 = new TextAnnotation
            {
                Text = "D5",
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextPosition = new DataPoint((this.gradientXPositions[8] + this.gradientXPositions[9]) / 2, this.gradientDoses[8]),
                Tag = "TextD5",
                TextColor = OxyColors.White
            };
            this.PlotModelProfile.Annotations.Add(textAnnotationD1);
            this.PlotModelProfile.Annotations.Add(textAnnotationD2);
            this.PlotModelProfile.Annotations.Add(textAnnotationD3);
            this.PlotModelProfile.Annotations.Add(textAnnotationD4);
            this.PlotModelProfile.Annotations.Add(textAnnotationD5);

            var axisx = this.PlotModelProfile.Axes.First(x => (string)x.Tag == "AxisX");
            axisx.Minimum = this.gradientXPositions.First();
            axisx.Maximum = this.gradientXPositions.Last();
            var axisy = this.PlotModelProfile.Axes.First(x => (string)x.Tag == "AxisY");
            axisy.Minimum = -2;
            axisy.Maximum = this.gradientDoses.Max() * 1.2;
            this.PlotModelProfile.InvalidatePlot(true);
            AddLineAnnotationsToDoseImage();
        }

        private void AddLineAnnotationsToDoseImage()
        {
            this.PlotModelImageDose.Annotations.Clear();
            for (int i = 1; i < this.gradientXPositions.Count - 1; i++)
            {
                var lineAnnotation = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    Color = OxyColors.Black,
                    X = this.gradientXPositions[i]
                };
                this.PlotModelImageDose.Annotations.Add(lineAnnotation);
            }
            this.PlotModelImageDose.InvalidatePlot(true);
        }

        private void SetGradientStartingValues()
        {
            int Nx = this.DoseMatrixList[0].GetLength(0);
            double x0 = this.DoseImagePositionPatient[2];
            double x1 = this.DoseImagePositionPatient[2] + this.DosePixelSpacing[2] * Nx;

            double dx = (x1 - x0) / 9.0;

            for (int i = 0; i < this.gradientXPositions.Count; i++)
            {
                this.gradientXPositions[i] = x0 + i * dx;
            }
        }

        private void SetTextBoxes()
        {
            this.TextBoxD1.Text = this.gradientDoses[0].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxD2.Text = this.gradientDoses[2].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxD3.Text = this.gradientDoses[4].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxD4.Text = this.gradientDoses[6].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxD5.Text = this.gradientDoses[8].ToString("F2", new CultureInfo("en-US"));

            this.TextBoxP1.Text = this.gradientXPositions[1].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP2.Text = this.gradientXPositions[2].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP3.Text = this.gradientXPositions[3].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP4.Text = this.gradientXPositions[4].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP5.Text = this.gradientXPositions[5].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP6.Text = this.gradientXPositions[6].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP7.Text = this.gradientXPositions[7].ToString("F2", new CultureInfo("en-US"));
            this.TextBoxP8.Text = this.gradientXPositions[8].ToString("F2", new CultureInfo("en-US"));
        }

        private double ConvertTextToDouble(string text)
        {
            if (Double.TryParse(text, out double result))
            {
                return result;
            }
            else
            {
                return Double.NaN;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", ".");
            txt.CaretIndex = ind;
        }


        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            this.gradientDoses[0] = ConvertTextToDouble(this.TextBoxD1.Text);
            this.gradientDoses[1] = ConvertTextToDouble(this.TextBoxD1.Text);
            this.gradientDoses[2] = ConvertTextToDouble(this.TextBoxD2.Text);
            this.gradientDoses[3] = ConvertTextToDouble(this.TextBoxD2.Text);
            this.gradientDoses[4] = ConvertTextToDouble(this.TextBoxD3.Text);
            this.gradientDoses[5] = ConvertTextToDouble(this.TextBoxD3.Text);
            this.gradientDoses[6] = ConvertTextToDouble(this.TextBoxD4.Text);
            this.gradientDoses[7] = ConvertTextToDouble(this.TextBoxD4.Text);
            this.gradientDoses[8] = ConvertTextToDouble(this.TextBoxD5.Text);
            this.gradientDoses[9] = ConvertTextToDouble(this.TextBoxD5.Text);

            this.gradientXPositions[1] = ConvertTextToDouble(this.TextBoxP1.Text);
            this.gradientXPositions[2] = ConvertTextToDouble(this.TextBoxP2.Text);
            this.gradientXPositions[3] = ConvertTextToDouble(this.TextBoxP3.Text);
            this.gradientXPositions[4] = ConvertTextToDouble(this.TextBoxP4.Text);
            this.gradientXPositions[5] = ConvertTextToDouble(this.TextBoxP5.Text);
            this.gradientXPositions[6] = ConvertTextToDouble(this.TextBoxP6.Text);
            this.gradientXPositions[7] = ConvertTextToDouble(this.TextBoxP7.Text);
            this.gradientXPositions[8] = ConvertTextToDouble(this.TextBoxP8.Text);
            CreateLineSeriesProfilePlot();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // default
            if (this.DoseMatrix == null)
            {
                return;
            }
            this.gradientXPositions = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this.gradientDoses = new List<double>() { 0, 0, 30.6, 30.6, 0, 0, 30.6, 30.6, 0, 0 };
            SetGradientStartingValues();
            CreateLineSeriesProfilePlot();
            SetTextBoxes();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // 0 gradient
            if (this.DoseMatrix == null)
            {
                return;
            }
            this.gradientXPositions = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this.gradientDoses = new List<double>() { 30.6, 30.6, 30.6, 30.6, 30.6, 30.6, 30.6, 30.6, 30.6, 30.6 };
            SetGradientStartingValues();
            CreateLineSeriesProfilePlot();
            SetTextBoxes();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // 1 gradient
            if (this.DoseMatrix == null)
            {
                return;
            }
            double xmin = this.DoseImageHeatMapSeries.X0;
            double xmax = this.DoseImageHeatMapSeries.X1;
            SetGradientStartingValues();

            this.gradientXPositions[0] = xmin;
            this.gradientXPositions[1] = xmin;
            this.gradientXPositions[2] = xmin;
            this.gradientXPositions[3] = (xmin + xmax) / 2.0 - 0.5;
            this.gradientXPositions[4] = (xmin + xmax) / 2.0 + 0.5;
            this.gradientXPositions[5] = xmax;
            this.gradientXPositions[6] = xmax;
            this.gradientXPositions[7] = xmax;
            this.gradientXPositions[8] = xmax;
            this.gradientXPositions[9] = xmax;

            this.gradientDoses = new List<double>() { 0, 0, 0, 0, 30.6, 30.6, 30.6, 30.6, 30.6, 30.6 };

            CreateLineSeriesProfilePlot();
            SetTextBoxes();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // 2 gradients
            if (this.DoseMatrix == null)
            {
                return;
            }
            double xmin = this.DoseImageHeatMapSeries.X0;
            double xmax = this.DoseImageHeatMapSeries.X1;
            SetGradientStartingValues();
            this.gradientXPositions[0] = xmin;
            this.gradientXPositions[1] = xmin;
            this.gradientXPositions[2] = xmin;
            this.gradientXPositions[3] = xmin + (xmax - xmin) / 3.0 - 0.5;
            this.gradientXPositions[4] = xmin + (xmax - xmin) / 3.0 + 0.5;
            this.gradientXPositions[5] = xmin + 2 * (xmax - xmin) / 3.0 - 0.5;
            this.gradientXPositions[6] = xmin + 2 * (xmax - xmin) / 3.0 + 0.5;
            this.gradientXPositions[7] = xmax;
            this.gradientXPositions[8] = xmax;
            this.gradientXPositions[9] = xmax;

            this.gradientDoses = new List<double>() { 0, 0, 0, 0, 30.6, 30.6, 0, 0, 0, 0 };

            CreateLineSeriesProfilePlot();
            SetTextBoxes();
        }

        private bool CheckProfileValidity()
        {
            // All X must be increasing. Dose must be non-negative.
            for (int i = 1; i < this.gradientXPositions.Count; i++)
            {
                if (this.gradientXPositions[i - 1] > this.gradientXPositions[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < this.gradientDoses.Count; i++)
            {
                if (this.gradientDoses[i] < 0)
                {
                    return false;
                }
            }
            return true;
        }

        private void LayoutUpdate(object sender, EventArgs e)
        {
            this.OxyPlotImageDose.Width = this.PlotDoseImageColumn.ActualWidth;
            this.OxyPlotImageDose.Height = this.PlotDoseImageRow.ActualHeight;
            this.OxyPlotProfile.Width = this.PlotDoseImageColumn.ActualWidth;
            this.OxyPlotProfile.Height = this.PlotProfileRow.ActualHeight;
        }

        private void PlotAperture_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.DoseImagePlotTypeComboBox.SelectedItem == null)
            {
                return;
            }
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Dose")
                    {
                        if (this.DoseMatrixList == null)
                        {
                            return;
                        }
                        if (this.DoseCurrentSliceIndex < this.DoseMatrixList.Count - 1)
                        {
                            this.DoseCurrentSliceIndex += 1;
                            ShowDosePlot();
                        }
                    }
                    else if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Image")
                    {
                        if (this.ImageMatrixList == null)
                        {
                            return;
                        }
                        if (this.ImageCurrentSliceIndex < this.ImageMatrixList.Count - 1)
                        {
                            this.ImageCurrentSliceIndex += 1;
                            ShowImagePlot();
                        }
                    }

                }
                else if (e.Delta < 0)
                {
                    if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Dose")
                    {
                        if (this.DoseMatrixList == null)
                        {
                            return;
                        }
                        if (this.DoseCurrentSliceIndex > 1)
                        {
                            this.DoseCurrentSliceIndex -= 1;
                            ShowDosePlot();
                        }
                    }
                    else if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Image")
                    {
                        if (this.ImageMatrixList == null)
                        {
                            return;
                        }
                        if (this.ImageCurrentSliceIndex > 1)
                        {
                            this.ImageCurrentSliceIndex -= 1;
                            ShowImagePlot();
                        }
                    }
                }
            }
        }

        private void ShowImagePlot()
        {
            if (this.ImageMatrixList.Count > 1)
            {
                int N = this.ImageMatrixList[this.ImageCurrentSliceIndex].GetLength(1);

                double x0 = this.ImageImagePositionPatient.First()[2];
                double x1 = this.ImageImagePositionPatient.Last()[2];
                double y0 = -(this.ImageImagePositionPatient.First()[1] + this.ImagePixelSpacing[1] * N);
                double y1 = -this.ImageImagePositionPatient.First()[1];
                this.DoseImageHeatMapSeries.Data = this.ImageMatrixList[this.ImageCurrentSliceIndex];

                this.DoseImageHeatMapSeries.X0 = x0;
                this.DoseImageHeatMapSeries.X1 = x1;
                this.DoseImageHeatMapSeries.Y0 = y0;
                this.DoseImageHeatMapSeries.Y1 = y1;

                this.DoseImageHeatMapAxis.Palette = OxyPalettes.Gray(1024);
                this.PlotModelImageDose.InvalidatePlot(true);
            }
        }

        private void ShowDosePlot()
        {
            double[,] img = this.DoseMatrixList[this.DoseCurrentSliceIndex];
            int Nx = img.GetLength(0);
            int Ny = img.GetLength(1);

            double x0 = this.DoseImagePositionPatient[2];
            double x1 = this.DoseImagePositionPatient[2] + this.DosePixelSpacing[2] * Nx;
            double y0 = -(this.DoseImagePositionPatient[1] + this.DosePixelSpacing[1] * Ny);
            double y1 = -this.DoseImagePositionPatient[1];
            this.DoseImageHeatMapSeries.Data = this.DoseMatrixList[this.DoseCurrentSliceIndex];

            this.DoseImageHeatMapSeries.X0 = x0;
            this.DoseImageHeatMapSeries.X1 = x1;
            this.DoseImageHeatMapSeries.Y0 = y0;
            this.DoseImageHeatMapSeries.Y1 = y1;

            this.DoseImageHeatMapAxis.Palette = OxyPalettes.Jet(1024);
            this.PlotModelImageDose.InvalidatePlot(true);
        }


        private void DoseImagePlotTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Dose")
            {
                if (this.DoseMatrix == null)
                {
                    return;
                }
                int N = this.DoseMatrixList.Count;
                this.DoseCurrentSliceIndex = (int)(N / 2);
                ShowDosePlot();
                //this.PlotModelImageDose.ResetAllAxes();
            }
            else if (this.DoseImagePlotTypeComboBox.SelectedValue.ToString() == "Image")
            {
                if (this.ImageMatrixList.Count > 1)
                {
                    int N = this.ImageMatrixList[this.ImageCurrentSliceIndex].GetLength(1);
                    this.ImageCurrentSliceIndex = (int)(N / 2);
                    ShowImagePlot();
                    //this.PlotModelImageDose.ResetAllAxes();
                }
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Multiselect = true;
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                try
                {
                    SortDicomFiles(dlg.FileNames.ToList());
                    BuildDoseMatrix(this.DoseFilePath);
                    this.DoseMatrixList = Convert3DArrayToListInt(this.DoseMatrix);

                    if (this.ImageFilesList.Count > 0)
                    {
                        BuildImageMatrix(this.ImageFilesList);
                    }
                }
                catch (Exception f)
                {
                    MessageBox.Show(f.Message + "\n" + f.StackTrace, "Error");
                    return;
                }
                SetPlotTypeComboBox();
                ShowDosePlot();
                this.PlotModelImageDose.ResetAllAxes();
                this.PlotModelImageDose.InvalidatePlot(true);

                SetGradientStartingValues();
                SetTextBoxes();
                CreateLineSeriesProfilePlot();

            }
            else
            {
                MessageBox.Show("No dicom selected.", "Error");
            }
        }

        private double GetDoseFromProfileAtPosition(double t)
        {
            List<double> x = this.gradientXPositions;
            List<double> y = this.gradientDoses;

            if (t <= x.First())
            {
                return y.First();
            }
            else if (t >= y.Last())
            {
                return y.Last();
            }
            else
            {
                int i;
                for (i = 0; i < x.Count - 2; i++)
                {
                    if (x[i] <= t && t < x[i + 1])
                    {
                        break;
                    }
                }
                if (Math.Abs(x[i + 1] - x[i]) < 1e-8)
                {
                    return (y[i] + y[i + 1]) / 2.0;
                }
                else
                {
                    return y[i] + ((y[i + 1] - y[i]) / (x[i + 1] - x[i])) * (t - x[i]);
                }
            }
        }


        public void SortDicomFiles(List<string> filenames)
        {
            List<string> imageList = new List<string>() { };
            List<string> doseList = new List<string>() { };
            List<string> imageStudyUID = new List<string>() { };
            List<string> imageSeriesUID = new List<string>() { };
            List<double> zIndex = new List<double>() { };

            foreach (var file in filenames)
            {
                var dcm = DICOMObject.Read(file);
                string SOPclassUID = (string)dcm.FindFirst(TagHelper.SOPClassUID).DData;
                if (SOPclassUID == "1.2.840.10008.5.1.4.1.1.2")
                {
                    imageList.Add(file);

                    string StudyInstanceUID = (string)dcm.FindFirst(TagHelper.StudyInstanceUID).DData;
                    string SeriesInstanceUID = (string)dcm.FindFirst(TagHelper.SeriesInstanceUID).DData;
                    imageStudyUID.Add(StudyInstanceUID);
                    imageSeriesUID.Add(SeriesInstanceUID);
                    double zCoord = ((List<double>)dcm.FindFirst(TagHelper.ImagePositionPatient).DData_)[2];

                    zIndex.Add(zCoord);
                }
                else if (SOPclassUID == "1.2.840.10008.5.1.4.1.1.481.2")
                {
                    var DoseSumType = dcm.FindFirst(TagHelper.DoseSummationType) as AbstractElement<String>;
                    if (DoseSumType != null)
                    {
                        if ((string)DoseSumType.DData == "PLAN")
                        {
                            doseList.Add(file);
                        }
                    }
                }
            }

            if (imageStudyUID.Distinct().ToList().Count > 1 || imageSeriesUID.Distinct().ToList().Count > 1 || doseList.Count > 1)
            {
                throw new NullReferenceException("Selected data may contain only one dose matrix of type 'PLAN' and the images must belong to the same series.\n");
            }
            if (doseList.Count < 1)
            {
                throw new NullReferenceException("A dose matrix of type 'PLAN' must be selected for loading.\n");
            }
            else
            {
                this.DoseFilePath = doseList.First();

                // Order image planes in order of Z coordinate
                var sorted = zIndex
                            .Select((x, i) => new KeyValuePair<double, int>(x, i))
                            .OrderBy(x => x.Key)
                            .ToList();
                List<int> idx = sorted.Select(x => x.Value).ToList();
                List<string> tempList = new List<string>() { };
                List<double> tempList2 = new List<double>() { };

                foreach (var z in idx)
                {
                    tempList.Add(imageList.ElementAt(z));
                    tempList2.Add(zIndex.ElementAt(z));
                }
                this.ImageFilesList = tempList;
            }
        }


        public void BuildDoseMatrix(string doseFilePath)
        {
            var dcm = DICOMObject.Read(doseFilePath);

            string patientName = (string)dcm.FindFirst(TagHelper.PatientName).DData;
            ushort bit = (ushort)dcm.FindFirst(TagHelper.BitsAllocated).DData;
            this.bits32_16 = (int)bit;

            ushort rows = (ushort)dcm.FindFirst(TagHelper.Rows).DData;
            ushort columns = (ushort)dcm.FindFirst(TagHelper.Columns).DData;
            int frames = (int)dcm.FindFirst(TagHelper.NumberOfFrames).DData;
            this.DoseImagePositionPatient = (List<double>)dcm.FindFirst(TagHelper.ImagePositionPatient).DData_;

            for (int i = 0; i < this.DoseImagePositionPatient.Count; i++)
            {   // Convert mm into cm
                this.DoseImagePositionPatient[i] = this.DoseImagePositionPatient[i] / 10.0;
            }

            List<double> offsetVector = (List<double>)dcm.FindFirst(TagHelper.GridFrameOffsetVector).DData_;
            double spacingZ = Math.Abs(offsetVector[1] - offsetVector[0]);

            List<double> pixelSpacing = (List<double>)dcm.FindFirst(TagHelper.PixelSpacing).DData_;
            this.DosePixelSpacing = new List<double>() { pixelSpacing[0] / 10.0, pixelSpacing[1] / 10.0, spacingZ / 10.0 };

            double scaling = 1.0;
            var _scaling = dcm.FindFirst(TagHelper.DoseGridScaling) as AbstractElement<double>;
            if (_scaling != null)
            {
                scaling = _scaling.Data;
            }
            this.DoseScaling = scaling;

            uint smallestimagepixelvalue = 0;
            uint largestimagepixelvalue = 0;
            var _smallestImagePixelValue = dcm.FindFirst(TagHelper.SmallestImagePixelValue) as AbstractElement<ushort>;
            if (_smallestImagePixelValue != null)
            {
                smallestimagepixelvalue = _smallestImagePixelValue.Data;
            }
            var _largestImagePixelValue = dcm.FindFirst(TagHelper.LargestImagePixelValue) as AbstractElement<ushort>;
            if (_largestImagePixelValue != null)
            {
                largestimagepixelvalue = _largestImagePixelValue.Data;
            }

            this.Title = "DoseGradient (" + patientName + ") (" + bit.ToString() + " bit) (" +
                frames.ToString() + "x" + columns.ToString() + "x" + rows.ToString() + ") (slice: " + spacingZ.ToString("F2") + " mm) (scaling: " + scaling.ToString("E6") +
                ") (min: " + smallestimagepixelvalue.ToString() + ", max: " + largestimagepixelvalue.ToString() + ")";

            List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;

            uint[,,] doseMatrix = new uint[frames, columns, rows];

            int indexColumn = 0;
            int indexRow = 0;
            int indexFrame = 0;

            if (bits32_16 == 32)
            {
                for (int i = 0; i < pixelData.Count(); i += 4)
                {
                    // taken from https://stackoverflow.com/questions/55883798/wrong-output-pixel-colors-grayscale-using-evildicom
                    // 32 bit

                    uint pixel = (uint)(pixelData[i + 3] * 256 * 256 * 256 + pixelData[i + 2] * 256 * 256 + pixelData[i + 1] * 256 + pixelData[i]);

                    doseMatrix[indexFrame, indexColumn, indexRow] = pixel;

                    indexColumn += 1;
                    if (indexColumn > columns - 1)
                    {
                        indexColumn = 0;
                        indexRow += 1;
                    }
                    if (indexRow > rows - 1)
                    {
                        indexFrame += 1;
                        indexColumn = 0;
                        indexRow = 0;
                    }
                }
            }
            else if (bits32_16 == 16)
            {
                for (int i = 0; i < pixelData.Count(); i += 2)
                {
                    // taken from https://stackoverflow.com/questions/55883798/wrong-output-pixel-colors-grayscale-using-evildicom
                    // 16 bit
                    ushort pixel = (ushort)(pixelData[i + 1] * 256 + pixelData[i]);
                    doseMatrix[indexFrame, indexColumn, indexRow] = pixel;

                    indexColumn += 1;
                    if (indexColumn > columns - 1)
                    {
                        indexColumn = 0;
                        indexRow += 1;
                    }
                    if (indexRow > rows - 1)
                    {
                        indexFrame += 1;
                        indexColumn = 0;
                        indexRow = 0;
                    }

                }
            }

            this.DoseMatrix = doseMatrix;
        }

        public void BuildImageMatrix(List<string> doseFilePath)
        {
            List<double[,]> imageTemp = new List<double[,]>() { };
            List<List<double>> imgPos = new List<List<double>>() { };

            foreach (string file in doseFilePath)
            {
                var dcm = DICOMObject.Read(file);
                ushort rows = (ushort)dcm.FindFirst(TagHelper.Rows).DData;
                ushort columns = (ushort)dcm.FindFirst(TagHelper.Columns).DData;

                List<double> pixelSpacing = (List<double>)dcm.FindFirst(TagHelper.PixelSpacing).DData_;
                double sliceThickness = (double)dcm.FindFirst(TagHelper.SliceThickness).DData;
                this.ImagePixelSpacing = new List<double>() { pixelSpacing[0] / 10.0, pixelSpacing[1] / 10.0, sliceThickness / 10.0 };

                List<double> imgposTemp = (List<double>)dcm.FindFirst(TagHelper.ImagePositionPatient).DData_;
                for (int i = 0; i < imgposTemp.Count; i++)
                {   // Convert mm into cm
                    imgposTemp[i] = imgposTemp[i] / 10.0;
                }
                imgPos.Add(imgposTemp);

                List<byte> pixelData = (List<byte>)dcm.FindFirst(TagHelper.PixelData).DData_;

                double rescaleIntercept = -1000.0;
                var _rescaleIntercept = dcm.FindFirst(TagHelper.RescaleIntercept) as AbstractElement<double>;
                if (_rescaleIntercept != null)
                {
                    rescaleIntercept = _rescaleIntercept.Data;
                }

                double rescaleSlope = 1.0;
                var _rescaleSlope = dcm.FindFirst(TagHelper.RescaleSlope) as AbstractElement<double>;
                if (_rescaleSlope != null)
                {
                    rescaleSlope = _rescaleSlope.Data;
                }

                double window = 0;
                var _window = dcm.FindFirst(TagHelper.WindowWidth) as AbstractElement<double>;
                if (_window != null)
                {
                    window = _window.Data;
                }
                window = window / rescaleSlope; // window and level are not in raw pixel values, but take into accout slope/intercept

                double level = 0;
                var _level = dcm.FindFirst(TagHelper.WindowCenter) as AbstractElement<double>;
                if (_level != null)
                {
                    level = _level.Data;
                }
                level = (level - rescaleIntercept) / rescaleSlope; // window and level are not in raw pixel values, but take into accout slope/intercept

                this.ImageWindowLevel = new List<double>() { window, level };

                double[,] image = new double[columns, rows];

                int indexColumn = 0;
                int indexRow = 0;

                for (int i = 0; i < pixelData.Count(); i += 2)
                {
                    // taken from https://stackoverflow.com/questions/55883798/wrong-output-pixel-colors-grayscale-using-evildicom
                    //original data - 16 bits unsigned
                    ushort pixel = (ushort)(pixelData[i + 1] * 256 + pixelData[i]);

                    int valgray = pixel;

                    image[indexColumn, rows - 1 - indexRow] = (double)valgray * rescaleSlope + rescaleIntercept;

                    indexColumn += 1;

                    if (indexColumn > columns - 1)
                    {
                        indexColumn = 0;
                        indexRow += 1;
                    }
                }
                imageTemp.Add(image);
            }
            this.ImageImagePositionPatient = imgPos;
            this.ImageMatrixList = ConvertList2DArray2SagitalList(imageTemp);
        }


        public List<double[,]> Convert3DArrayToListInt(uint[,,] array)
        {
            // each element of list is one sagital slice
            // and convert to double!
            List<double[,]> temp = new List<double[,]>() { };
            int Zsize = array.GetLength(2);
            for (int j = 0; j < array.GetLength(1); j++)
            {
                double[,] image = new double[array.GetLength(0), array.GetLength(2)];

                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int k = 0; k < array.GetLength(2); k++)
                    {
                        image[i, Zsize - k - 1] = (double)array[i, j, k] * this.DoseScaling;
                    }
                }
                temp.Add(image);
            }
            return temp;
        }

        public List<double[,]> ConvertList2DArray2SagitalList(List<double[,]> array)
        {
            // each element of list is one sagital slice
            // and convert to double!
            List<double[,]> temp = new List<double[,]>() { };

            int N = array.Count;

            for (int i = 0; i < array.First().GetLength(0); i++)
            {
                double[,] img = new double[N, array.First().GetLength(1)];
                for (int j = 0; j < N; j++)
                {
                    for (int k = 0; k < array.First().GetLength(1); k++)
                    {
                        img[j, k] = array.ElementAt(j)[i, k];
                    }
                }
                temp.Add(img);
            }
            return temp;
        }

        public Tuple<uint, uint> GetMinMaxValues(uint[,,] array)
        {
            uint min = UInt32.MaxValue;
            uint max = 0;

            for (int i = 0; i < array.GetLength(1); i++)
            {
                for (int j = 0; j < array.GetLength(2); j++)
                {
                    for (int k = 0; k < array.GetLength(0); k++)
                    {
                        uint temp = array[k, i, j];

                        if (temp > max)
                        {
                            max = temp;
                        }
                        else if (temp < min)
                        {
                            min = temp;
                        }
                    }
                }
            }
            return Tuple.Create(min, max);
        }

        public void RescaleMatrix(uint[,,] array, double factor)
        {
            for (int i = 0; i < array.GetLength(1); i++)
            {
                for (int j = 0; j < array.GetLength(2); j++)
                {
                    for (int k = 0; k < array.GetLength(0); k++)
                    {
                        array[k, i, j] = (uint)((double)array[k, i, j] * factor);
                    }
                }
            }
        }

        public List<byte> Convert32bitToByte(Tuple<uint[,,], uint, uint, double> newData)
        {
            // This function converts int[,,] into 32-bit byte array so that it can be written to dicom
            uint[,,] array = newData.Item1;
            List<byte> byteArray = new List<byte>() { };
            int frames = array.GetLength(0);
            int rows = array.GetLength(2);
            int columns = array.GetLength(1);
            for (int i = 0; i < frames; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    for (int k = 0; k < columns; k++)
                    {
                        byte[] intBytes = BitConverter.GetBytes(array[i, k, j]);
                        byteArray.Add(intBytes[0]);
                        byteArray.Add(intBytes[1]);
                        byteArray.Add(intBytes[2]);
                        byteArray.Add(intBytes[3]);
                    }
                }
            }
            return byteArray;
        }

        public List<byte> Convert16bitToByte(Tuple<uint[,,], uint, uint, double> newData)
        {
            // This function converts int[,,] into 16-bit byte array so that it can be written to dicom
            uint[,,] array = newData.Item1;
            List<byte> byteArray = new List<byte>() { };

            int frames = array.GetLength(0);
            int rows = array.GetLength(2);
            int columns = array.GetLength(1);
            for (int i = 0; i < frames; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    for (int k = 0; k < columns; k++)
                    {
                        ushort pix = (ushort)array[i, k, j];
                        byte[] intBytes = BitConverter.GetBytes(pix);
                        byteArray.Add(intBytes[0]);
                        byteArray.Add(intBytes[1]);
                    }
                }
            }
            return byteArray;
        }


        private Tuple<uint[,,], uint, uint, double> ChangeMatrix(uint[,,] array, double scaling)
        {
            int frames = array.GetLength(0);
            int rows = array.GetLength(2);
            int columns = array.GetLength(1);

            uint[,,] newArray = new uint[frames, columns, rows];
            Tuple<uint, uint> minmax2 = GetMinMaxValues(array);

            double X0 = this.DoseImagePositionPatient[2];
            double xmin = this.gradientXPositions.Min();
            double xmax = this.gradientXPositions.Max();

            bool overwriteZero = (bool)this.CheckBoxOverwriteZeroVoxels.IsChecked;

            for (int i = 0; i < frames; i++)
            {
                double x = X0 + i * this.DosePixelSpacing[2];
                double doseInSlice = 1.0;

                if (x >= xmin && x <= xmax)
                {
                    doseInSlice = GetDoseFromProfileAtPosition(x); // Gy
                }

                for (int j = 0; j < rows; j++)
                {
                    for (int k = 0; k < columns; k++)
                    {
                        if (overwriteZero)
                        {
                            newArray[i, k, j] = (uint)(doseInSlice / scaling);
                        }
                        else if (this.DoseMatrix[i, k, j] != 0)
                        {
                            newArray[i, k, j] = (uint)(doseInSlice / scaling);
                        }
                    }
                }
            }

            // Find min and max and rescale, and define new scaling factor 
            // Apparently, Monaco has to have true min and max voxel values in the matrix. But not always. What is going on...
            Tuple<uint, uint> minmax = GetMinMaxValues(newArray);
            if (this.bits32_16 == 16)
            {
                if (minmax.Item2 > UInt16.MaxValue)
                {
                    double factor = (double)UInt16.MaxValue / (double)minmax.Item2;

                    RescaleMatrix(newArray, factor);
                    minmax = GetMinMaxValues(newArray);
                    scaling = scaling / factor;
                }
            }
            else if (this.bits32_16 == 32)
            {
                if (minmax.Item2 > UInt32.MaxValue)
                {
                    double factor = (double)UInt32.MaxValue / (double)minmax.Item2;
                    RescaleMatrix(newArray, factor);
                    minmax = GetMinMaxValues(newArray);
                    scaling = scaling / factor;
                }
            }

            return new Tuple<uint[,,], uint, uint, double>(newArray, minmax.Item1, minmax.Item2, scaling);
        }


        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (this.DoseMatrix == null)
            {
                MessageBox.Show("Dose matrix does not exist in memory.", "Error");
                return;
            }
            if (!CheckProfileValidity())
            {
                MessageBox.Show("Invalid profile! Make sure that the point coordinates are increasing and that the dose is non-negative.", "Error");
                return;
            }
            // save to dicom
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = System.IO.Path.GetFileNameWithoutExtension(this.DoseFilePath) + "_Changed.dcm";
            dialog.DefaultExt = ".dcm";
            dialog.Filter = "DICOM (.dcm)|*.DCM";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string filename = dialog.FileName;
                Tuple<uint[,,], uint, uint, double> newData = ChangeMatrix(this.DoseMatrix, this.DoseScaling);
                SaveToDicom(this.DoseFilePath, filename, newData);
                MessageBox.Show("Matrix saved to " + filename, "Info");
            }
        }

        private void SaveToDicom(string oldfilePath, string filePath, Tuple<uint[,,], uint, uint, double> newData)
        {
            List<byte> byteArray;
            if (this.bits32_16 == 32)
            {
                byteArray = Convert32bitToByte(newData);
            }
            else
            {
                byteArray = Convert16bitToByte(newData);
            }
            // First copy dcm file (didn't find a way to clone dicom)
            try
            {
                File.Copy(oldfilePath, filePath, true);
            }
            catch (IOException iox)
            {
                MessageBox.Show(iox.Message, "Error");
            }

            var dcm = DICOMObject.Read(filePath);

            dcm.FindFirst(TagHelper.PixelData).DData_ = byteArray;

            dcm.FindFirst(TagHelper.DoseGridScaling).DData = Double.Parse(newData.Item4.ToString("E6"));

            var _smallestImagePixelValue = dcm.FindFirst(TagHelper.SmallestImagePixelValue) as AbstractElement<ushort>;
            if (_smallestImagePixelValue != null)
            {
                dcm.FindFirst(TagHelper.SmallestImagePixelValue).DData = (ushort)newData.Item2;
            }
            var _largestImagePixelValue = dcm.FindFirst(TagHelper.LargestImagePixelValue) as AbstractElement<ushort>;
            if (_largestImagePixelValue != null)
            {
                dcm.FindFirst(TagHelper.LargestImagePixelValue).DData = (ushort)newData.Item3;
            }
            // Since Eclipse is using 32 bit voxels, setting the true largest pixel value would not work because only unsigned short is allowed.
            // However, Eclipse is not writing this tag to dicom. Ha ha ...
            dcm.FindFirst(TagHelper.SOPInstanceUID).DData = UIDHelper.GenerateUID();
            dcm.WriteAddMeta(filePath);
            // using dcm.Write() doesn't work with Monaco, it works only with Eclipse
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            // help
            HelpWindow helpwindow = new HelpWindow();
            helpwindow.Show();
        }
    }
}
