#define V161

using DoseRateEditor.Models;
using GalaSoft.MvvmLight.Command;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Talos.Models;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using static DoseRateEditor.Models.DRCalculator;

//using static System.Net.WebRequestMethods;

// TODO: Scale y axis to max DR
// TODO: Color underline cb's as legend
// TODO: Crop pictures to "Nose only" on coro/sag views
// TODO: Fix id of new plan

// TODO: add check to see if DR is modulated and warn user if this is the case
// TODO make buttons do something
// TODO: add credit pane for selected DR edit method
// Check machine if TB -> maxGS=6
// TODO: add not for clinical use warning sign
// TODO: add waiver key in config to switch off the date dialog box and the not validated for clinical use banner
// Add liscence .txt
// if constant do nothing

namespace DoseRateEditor.ViewModels
{
    public class MainViewModel : Prism.Mvvm.BindableBase
    {
        private VMS.TPS.Common.Model.API.Application _app;
        public DelegateCommand OpenPatientCommand { get; private set; } // 1) in dcmd
        public DelegateCommand ViewCourseCommand { get; private set; }
        public DelegateCommand EditDRCommand { get; private set; }
        public DelegateCommand OnPlanSelectCommand { get; private set; }
        public DelegateCommand OnMethodSelectCommand { get; private set; }
        public DelegateCommand OnBeamSelectCommand { get; private set; }
        public DelegateCommand PlotCurrentDRCommand { get; private set; }
        public DelegateCommand PlotCurrentGSCommand { get; private set; }
        public DelegateCommand PreviewGSCommand { get; private set; }
        public DelegateCommand PreviewDRCommand { get; private set; }
        public DelegateCommand PreviewdMUCommand { get; private set; }
        public DelegateCommand PlotCurrentdMUCommand { get; private set; }
        public DelegateCommand HyperlinkCmd { get; private set; }
        public IViewCommand<OxyMouseWheelEventArgs> TransScrollCommand { get; private set; }
        public ObservableCollection<CourseModel> Courses { get; private set; }
        public ObservableCollection<PlanningItem> Plans { get; private set; }
        public ObservableCollection<Beam> Beams { get; private set; }
        public ObservableCollection<Tuple<Nullable<DRMethod>, Nullable<bool>>> DRMethods { get; private set; }

        //settings file
        private GapSettings _gapSettings;

        public GapSettings GapSettings
        {
            get { return _gapSettings; }
            set { SetProperty(ref _gapSettings, value); }
        }

        // DR EDIT METHOD CREDIT TEXT
        private string _CreditText;

        public string CreditText
        {
            get { return _CreditText; }
            set { SetProperty(ref _CreditText, value); }
        }

        // *** Not validated *** text
        private string postText;

        public string PostText
        {
            get { return postText; }
            set { SetProperty(ref postText, value); }
        }

        // CHECKBOXES
        private bool _PreviewDR;

        public bool PreviewDR
        {
            get { return _PreviewDR; }
            set { SetProperty(ref _PreviewDR, value); }
        }

        private string _AppTitle;

        public string AppTitle
        {
            get { return _AppTitle; }
            set { SetProperty(ref _AppTitle, value); }
        }

        private bool _PreviewGS;

        public bool PreviewGS
        {
            get { return _PreviewGS; }
            set { SetProperty(ref _PreviewGS, value); }
        }

        private bool _CurrentDR;

        public bool CurrentDR
        {
            get { return _CurrentDR; }
            set { SetProperty(ref _CurrentDR, value); }
        }

        private bool _CurrentGS;

        public bool CurrentGS
        {
            get { return _CurrentGS; }
            set { SetProperty(ref _CurrentGS, value); }
        }

        private bool _CurrentdMU;

        public bool CurrentdMU
        {
            get { return _CurrentdMU; }
            set { SetProperty(ref _CurrentdMU, value); }
        }

        private bool _PreviewdMU;

        public bool PreviewdMU
        {
            get { return _PreviewdMU; }
            set { SetProperty(ref _PreviewdMU, value); }
        }

        // END CHECKBOXES

        private IPlotController _TransController;

        public IPlotController TransController
        {
            get => _TransController;
            set => SetProperty(ref _TransController, value);
        }

        private int _SelectedSlice;

        public int SelectedSlice
        {
            get { return _SelectedSlice; }
            set { SetProperty(ref _SelectedSlice, value); }
        }

        private PlotModel _TransPlot;

        public PlotModel TransPlot
        {
            get { return _TransPlot; }
            set { SetProperty(ref _TransPlot, value); }
        }

        private double[][,] _CT;

        public double[][,] CT
        {
            get { return _CT; }
            set { SetProperty(ref _CT, value); }
        }

        private Nullable<DRMethod> _SelectedMethod;

        public Nullable<DRMethod> SelectedMethod
        {
            get { return _SelectedMethod; }
            set
            {
                SetProperty(ref _SelectedMethod, value);
                EditDRCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
            }
        }

        private string _patientId;

        public string PatientId
        {
            get { return _patientId; }
            set { SetProperty(ref _patientId, value); }
        }

        private CourseModel _SelectedCourse;

        public CourseModel SelectedCourse
        {
            get { return _SelectedCourse; }
            set { SetProperty(ref _SelectedCourse, value); EditDRCommand.RaiseCanExecuteChanged(); }
        }

        private ExternalPlanSetup _SelectedPlan;

        public ExternalPlanSetup SelectedPlan
        {
            get { return _SelectedPlan; }
            set
            {
                SetProperty(ref _SelectedPlan, value);
                EditDRCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PlotCurrentDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
                PlotCurrentGSCommand.RaiseCanExecuteChanged();
                PreviewdMUCommand.RaiseCanExecuteChanged();
                PlotCurrentdMUCommand.RaiseCanExecuteChanged();
            }
        }

        private Beam _SelectedBeam;

        public Beam SelectedBeam
        {
            get { return _SelectedBeam; }
            set
            {
                SetProperty(ref _SelectedBeam, value);
                PlotCurrentdMUCommand.RaiseCanExecuteChanged();
                PlotCurrentDRCommand.RaiseCanExecuteChanged();
                PlotCurrentGSCommand.RaiseCanExecuteChanged();
                PreviewdMUCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
            }
        }

        private PlotModel _DRPlot;

        public PlotModel DRPlot
        {
            get { return _DRPlot; }
            set { SetProperty(ref _DRPlot, value); }
        }

        // Cosmo views
        private CosmoTrans _View1;

        public CosmoTrans View1
        {
            get { return _View1; }
            set { SetProperty(ref _View1, value); }
        }

        private CosmoCoro _View2;

        public CosmoCoro View2
        {
            get { return _View2; }
            set { SetProperty(ref _View2, value); }
        }

        private CosmoSag _View3;

        public CosmoSag View3
        {
            get { return _View3; }
            set { SetProperty(ref _View3, value); }
        }

        private LinearAxis DRAxis;

        private LinearAxis dMUAxis;

        private LinearAxis GSAxis;

        private DRCalculator _DRCalc;

        public DRCalculator DRCalc
        {
            get { return _DRCalc; }
            set { SetProperty(ref _DRCalc, value); }
        }

        private LineSeries _DR0_series;

        public LineSeries DR0_series
        {
            get { return _DR0_series; }
            set { SetProperty(ref _DR0_series, value); }
        }

        private LineSeries _DRf_series;

        public LineSeries DRf_series
        {
            get { return _DRf_series; }
            set { SetProperty(ref _DRf_series, value); }
        }

        private LineSeries _GSf_series;

        public LineSeries GSf_series
        {
            get { return _GSf_series; }
            set { SetProperty(ref _GSf_series, value); }
        }

        private LineSeries _GS0_series;

        public LineSeries GS0_series
        {
            get { return _GS0_series; }
            set { SetProperty(ref _GS0_series, value); }
        }

        private LineSeries _dMU0_series;

        public LineSeries dMU0_series
        {
            get { return _dMU0_series; }
            set { SetProperty(ref _dMU0_series, value); }
        }

        private LineSeries _dMUf_series;

        public LineSeries dMUf_series
        {
            get { return _dMUf_series; }
            set { SetProperty(ref _dMUf_series, value); }
        }

        private Patient _selectedPatient;

        public Patient SelectedPatient
        {
            get { return _selectedPatient; }
            set { SetProperty(ref _selectedPatient, value); }
        }

        public MainViewModel(VMS.TPS.Common.Model.API.Application app, Patient patient, Course course, PlanSetup plan)
        {
            _app = app;
            _selectedPatient = patient;

            LoadBeamTemplates();

            // Create delegate cmd
            //OpenPatientCommand = new DelegateCommand(OnOpenPatient); // DELETE!
            OpenPatientCommand = new DelegateCommand(OnOpenPatient, CanOpenPatient);
            ViewCourseCommand = new DelegateCommand(OnSelectCourse, CanSelectCourse);
            EditDRCommand = new DelegateCommand(OnEditDR, CanEditDR);
            OnPlanSelectCommand = new DelegateCommand(OnPlanSelect, CanPlanSelect);
            OnMethodSelectCommand = new DelegateCommand(OnMethodSelect, CanMethodSelect);
            TransScrollCommand = new DelegatePlotCommand<OxyMouseWheelEventArgs>(OnScroll);
            PlotCurrentDRCommand = new DelegateCommand(OnCurrentDR, CanCurrentDR);
            PreviewDRCommand = new DelegateCommand(OnPreviewDR, CanPreviewDR);
            PreviewGSCommand = new DelegateCommand(OnPreviewGS, CanPreviewGS);
            PlotCurrentGSCommand = new DelegateCommand(OnCurrentGS, CanCurrentGS);
            PreviewdMUCommand = new DelegateCommand(OnPreviewdMU, CanPreviewdMU);
            PlotCurrentdMUCommand = new DelegateCommand(OnCurrentdMU, CanCurrentdMU);
            OnBeamSelectCommand = new DelegateCommand(OnBeamSelect, CanBeamSelect);

            HyperlinkCmd = new DelegateCommand(OnHyperlink, CanHyperlink);

            AppTitle = "Doserate Editor";
            if ((ConfigurationManager.AppSettings["Validated"] == "false"))
            {
                AppTitle += " *** NOT VALIDATED FOR CLINICAL USE ***";
                PostText = " *** NOT VALIDATED FOR CLINICAL USE ***";
            }
            var ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AppTitle += " ";
            AppTitle += ver;

            Courses = new ObservableCollection<CourseModel>();
            Plans = new ObservableCollection<PlanningItem>();
            Beams = new ObservableCollection<Beam>();

            CreditText = "";

            // DR PLOT
            DRPlot = new PlotModel { Title = "Doserate and Gantry Speed" };
            //DRPlot.Legends.Add(new
            //{
            //    LegendTitle = "Legend",
            //    LegendPosition = LegendPosition.RightBottom,
            //});

            // Instantiate cosmo views
            View1 = new CosmoTrans();
            View2 = new CosmoCoro();
            View3 = new CosmoSag();

            // Create the different axes with respect/ive keys
            DRAxis = new LinearAxis
            {
                IsAxisVisible = true,
                Title = "Doserate (MU/min)",
                IsPanEnabled = false,
                IsZoomEnabled = false,
                Position = AxisPosition.Left,
                Key = "DRAxis",
                TitleColor = OxyColor.Parse("#e60909"),
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 2500
            };
            DRPlot.Axes.Add(DRAxis);

            GSAxis = new LinearAxis
            {
                Title = "Gantry Speed (deg/sec)",
                Position = AxisPosition.Right,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                Key = "GSAxis",
                TitleColor = OxyColor.Parse("#3283a8"),
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 6,
                Minimum = 0,
                Maximum = 6
            };
            DRPlot.Axes.Add(GSAxis);

            dMUAxis = new LinearAxis
            {
                //Title = "Delta MU",
                IsAxisVisible = false,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                Position = AxisPosition.Left,
                TickStyle = TickStyle.None,
                Key = "dMUAxis"
            };
            DRPlot.Axes.Add(dMUAxis);

            // Add the series
            DR0_series = new LineSeries
            {
                YAxisKey = "DRAxis",
                Color = OxyColors.Red
            };
            DRPlot.Series.Add(DR0_series);

            GS0_series = new LineSeries
            {
                YAxisKey = "GSAxis",
                Color = OxyColors.Blue
            };
            DRPlot.Series.Add(GS0_series);

            DRf_series = new LineSeries
            {
                YAxisKey = "DRAxis",
                Color = OxyColors.OrangeRed,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4
            };
            DRPlot.Series.Add(DRf_series);

            GSf_series = new LineSeries
            {
                YAxisKey = "GSAxis",
                Color = OxyColors.DeepSkyBlue,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4,
            };
            DRPlot.Series.Add(GSf_series);

            dMU0_series = new LineSeries
            {
                YAxisKey = "dMUAxis",
                Color = OxyColors.Gold
            };
            DRPlot.Series.Add(dMU0_series);

            dMUf_series = new LineSeries
            {
                YAxisKey = "dMU Axis",
                Color = OxyColors.LightGoldenrodYellow,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4
            };

            // TRANS PLOT
            TransPlot = new PlotModel { Title = "CT Slice View" };
            AddAxes(TransPlot);
            TransController = new PlotController();

            TransController.UnbindAll();
            TransController.BindMouseWheel(TransScrollCommand);
            CT = new double[0][,];

            // Set the DR EDIT methods

            DRMethods = new ObservableCollection<Tuple<Nullable<DRMethod>, Nullable<bool>>>
            {
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Sin, true),
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Cosmic, true),
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Bhaskara, true)
               //new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Juha, false)
            };

            PatientId = patient.Id;
            OpenPatient(patient);
            SelectedCourse = Courses.FirstOrDefault(x => x.Id == course.Id);
            OnSelectCourse();
            SelectedPlan = Plans.FirstOrDefault(x => x.Id == plan.Id) as ExternalPlanSetup;
            OnPlanSelect();

            ImportSettings();
        }

        public void ImportSettings()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Settings.xml");

            if (!File.Exists(filePath))
            {
                GapSettings defaultSettings = new GapSettings();
                defaultSettings.EnableSlidingLeaf = true;
                defaultSettings.GapSize = 2.1;
                defaultSettings.X = 150;
                defaultSettings.Y = 150;
                defaultSettings.SlidingLeafGapSize = 2;

                var serializer = new XmlSerializer(typeof(GapSettings));

                try
                {
                    using (var writer = new StreamWriter(filePath))
                    {
                        serializer.Serialize(writer, defaultSettings);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (File.Exists(filePath))
            {
                GapSettings = new GapSettings();
                var serializer = new XmlSerializer(typeof(GapSettings));
                using (var reader = new StreamReader(filePath))
                {
                    GapSettings = (GapSettings)serializer.Deserialize(reader);
                }
            }
            else
            {
                MessageBox.Show("The settings.xml file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnHyperlink()
        {
            var url = "http://medicalaffairs.varian.com/download/VarianLUSLA.pdf";
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url)
                );
        }

        private bool CanHyperlink()
        {
            return true;
        }

        private void OnBeamSelect()
        {
            if (SelectedBeam == null)
            {
                return;
            }

            // Clear all DR related plots (3 of them!)
            ResetPlot();

            // Cosmoplot stuff ...
            // View1 = Trans, View2 = Coro, View3 = Sag
            View1.ClearPlot();
            View2.ClearPlot();
            View3.ClearPlot();

            var dMU = DRCalc.InitialdMU[SelectedBeam.Id];
            var angles = Utils.GetBeamAngles(SelectedBeam);

            // Check all current
            CurrentdMU = true;
            CurrentDR = true;
            CurrentGS = true;

            // Check what is checked and Replot if needed
            OnCurrentdMU();
            OnCurrentDR();
            OnCurrentGS();

            var deltaMU = dMU.Select(x => x.Y).ToList();
            View3.DrawRects(deltaMU, angles.Item1.First(), angles.Item1.Last(), angles.Item3[0], angles.Item5);
            View2.DrawAngle(angles.Item3[0]);
            View1.DrawRects(deltaMU, angles.Item1.First(), angles.Item1.Last(), angles.Item3[0], angles.Item5);

            // Update all DR, GS, dMU plots
            OnPreviewGS();
            OnPreviewDR();
            //OnPreviewdMU();
        }

        private bool CanBeamSelect()
        {
            return true;
        }

        private void OnScroll(IPlotView arg1, IController arg2, OxyMouseWheelEventArgs arg3)
        {
            if (arg3.Delta > 0)
            {
                if (SelectedSlice + 1 >= SelectedPlan.StructureSet.Image.ZSize)
                {
                    return;
                }
                SelectedSlice++;
            }

            if (arg3.Delta < 0)
            {
                if (SelectedSlice < 1)
                {
                    return;
                }
                SelectedSlice--;
            }

            TransPlot.InvalidatePlot(false);
            UpdateSeries(TransPlot);
        }

        private void AddAxes(PlotModel plotModel) // Lifted from CJA blog "Working with various plot types"
        {
            var xAxis = new LinearAxis
            {
                Title = "X",
                Position = AxisPosition.Bottom,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            };
            plotModel.Axes.Add(xAxis);

            var yAxis = new LinearAxis
            {
                Title = "Y",
                Position = AxisPosition.Left,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            };
            plotModel.Axes.Add(yAxis);

            var zAxis = new LinearColorAxis
            {
                Title = "CT", // TODO - Change!
                Position = AxisPosition.Top,
                Palette = OxyPalettes.Gray(256),
                Maximum = 1000, // TODO - Change!
                Minimum = 0
            };
            plotModel.Axes.Add(zAxis);
        }

        private void UpdateSeries(PlotModel plotModel)
        {
            plotModel.Series.Clear();
            plotModel.Series.Add(CreateHeatMap(SelectedSlice));
            plotModel.InvalidatePlot(true);
        }

        private HeatMapSeries CreateHeatMap(int slice)
        {
            var data = GetCTData(slice);
            double[,] data_double = new double[data.GetLength(0), data.GetLength(1)];
            Array.Copy(data, data_double, data.Length);
            //Array.Reverse(data_double);
            return new HeatMapSeries
            {
                X0 = data.GetLength(1),
                X1 = 0,
                Y0 = data.GetLength(0),
                Y1 = 0,
                Data = data_double
            };
        }

        private int[,] GetCTData(int slice)
        {
            var imgs = SelectedPlan.StructureSet.Image.Series.Images;
            var data = new int[imgs.First().XSize, imgs.First().YSize];
            imgs.First().GetVoxels((int)slice, data);
            //var newArray = Array.ConvertAll(array, item => (NewType)item);
            return data;
        }

        // function to set axis range to avoid small fluctuations
        private void PlotWithScale(LinearAxis axis, LineSeries series, PlotModel plot, List<DataPoint> pts, double tolerance)
        {
            var yvals = pts.Select(pt => pt.Y);
            var range = Math.Abs(yvals.Max() - yvals.Min());

            var axismin = yvals.Min();
            var axismax = yvals.Max();
            if (range < tolerance)
            {
                axismin = yvals.Average() - 50;
                axismax = yvals.Average() + 50;
            }
            axis.Minimum = axismin;
            axis.Maximum = axismax;
            series.Points.AddRange(pts);
            plot.InvalidatePlot(true);
        }

        private void OnCurrentdMU()
        {
            // Clear line plot of dMU
            dMU0_series.Points.Clear();

            if (CurrentdMU)
            {
                var dMU = DRCalc.InitialdMU[SelectedBeam.Id];
                PlotWithScale(dMUAxis, dMU0_series, DRPlot, dMU, 3);
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentdMU()
        { return CanCurrentDR(); }

        private void OnPreviewdMU()
        { return; }

        private bool CanPreviewdMU()
        { return false; }

        private void OnMethodSelect()
        {
            // Clear final gs and dr series
            GSf_series.Points.Clear();
            DRf_series.Points.Clear();

            // Calculate final dr and gs
            DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);

            // Update Credit Text
            //var func_string = DRCalc.DelegateDictionary
            CreditText = DRCalc.DRCreditsString;

            DRPlot.InvalidatePlot(true);
        }

        private bool CanMethodSelect()
        {
            return CanEditDR();
        }

        // Delegate methods for plotting ...
        private void OnCurrentGS()
        {
            if (CurrentGS) // If checkbox is checked
            {
                //GS0_series.Points.AddRange(DRCalc.InitialGSs[SelectedBeam.Id]);
                PlotWithScale(GSAxis, GS0_series, DRPlot, DRCalc.InitialGSs[SelectedBeam.Id], 3);
            }
            else
            {
                GS0_series.Points.Clear();
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentGS()
        {
            return CanCurrentDR();
        }

        private void OnPreviewDR()
        {
            if (PreviewDR) // See if check button is checked
            {
                if (!DRCalc.LastMethodCalculated.HasValue) // if the DRCalc dosen't already have the DRf calculated
                {
                    if (DRCalc.LastMethodCalculated != SelectedMethod.Value)
                    {
                        // Calculate final DR
                        DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);
                    }
                }

                // Plot final DR
                var DRf = DRCalc.FinalDRs[SelectedBeam.Id];
                PlotWithScale(DRAxis, DRf_series, DRPlot, DRf, 3);
            }
            else
            {
                // Else clear DRf
                DRf_series.Points.Clear();
            }

            DRPlot.InvalidatePlot(true);
        }

        private bool CanPreviewDR()
        {
            return SelectedMethod.HasValue && (SelectedPlan != null);
        }

        private void OnPreviewGS()
        {
            if (PreviewGS) // See if check button is checked, else delete GSf plot
            {
                if (!DRCalc.LastMethodCalculated.HasValue)
                {
                    if (DRCalc.LastMethodCalculated != SelectedMethod.Value)
                    {
                        // Calculate final DR
                        DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);
                    }
                }
                var GSf = DRCalc.FinalGSs[SelectedBeam.Id];
                PlotWithScale(GSAxis, GSf_series, DRPlot, GSf, 3);
            }
            else
            {
                GSf_series.Points.Clear();
            }

            DRPlot.InvalidatePlot(true);
        }

        private bool CanPreviewGS()
        {
            var res = SelectedMethod.HasValue && (SelectedPlan != null);
            return res;
        }

        private void OnCurrentDR()
        {
            if (CurrentDR) // If checkbox is checked
            {
                var DR0 = DRCalc.InitialDRs[SelectedBeam.Id];
                PlotWithScale(DRAxis, DR0_series, DRPlot, DR0, 3);
            }
            else
            {
                DR0_series.Points.Clear();
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentDR()
        { return SelectedPlan != null && SelectedBeam != null; }

        private void OnPlanSelect()
        {
            // Unselect Beam
            _SelectedBeam = null;

            // Create a DR calculator for selected plan
            DRCalc = new DRCalculator(SelectedPlan, _app);

            // Clear the previous initial and final dr/gs plots
            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GSf_series.Points.Clear();

            // Clear prev list of beams
            Beams.Clear();
            bool isEmpty = true;

            // Add all beams
            foreach (var bm in SelectedPlan.Beams)
            {
                if (bm.Technique.Id.ToLower().Contains("arc"))
                {
                    Beams.Add(bm);
                    isEmpty = false;
                }
            }

            ResetPlot();

            if (isEmpty)
            {
                System.Windows.MessageBox.Show($"Plan {SelectedPlan.Id} does not contain any fields with SRS ARC or ARC technique");
                UncheckAll();
            }
            else
            {
                // If we have beams to select
                // Select the first one
                SelectedBeam = Beams.FirstOrDefault();
                OnBeamSelect();
            }

            // Reset transaxial plot
            //int z_mid = _CT.Length/2;
            //SelectedSlice = z_mid;
            //var isoSlice = SelectedPlan.StructureSet.Image.Series.Images.
            SelectedSlice = SelectedPlan.StructureSet.Image.Series.Images.Count() / 2;
            UpdateSeries(TransPlot);

            /*
            if (SelectedPlan.Dose == null)
            {
                // Warn user that beam.Meterset will be null
                System.Windows.MessageBox.Show("Warning: Plan metersets are null. Plots will not display.");
            }*/
        }

        private bool CanPlanSelect()
        {
            return (SelectedCourse != null) && (SelectedPlan != null);
        }

        private void OnEditDR()
        {
            bool isConstant = true;
            foreach (var key in DRCalc.InitialDRs.Keys)
            {
                var DRs = DRCalc.InitialDRs[key].Select(pt => pt.Y);
                if (Math.Abs(DRs.Max() - DRs.Min()) > 1)
                {
                    isConstant = false;
                    break;
                }
            }

            if (!isConstant)
            {
                var msg = "Warning: plan already contains non-constant dose rate. Are you sure you want to apply this function? Results may be unexpected.";
                var res = System.Windows.MessageBox.Show(msg, "Warning", System.Windows.MessageBoxButton.YesNo);
                if (res == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }

            DRCalc.CreateNewPlanWithMethod(SelectedMethod.Value, GapSettings);
            _app.SaveModifications();

            OnPlanSelect(); // Calling this so that the selected plan isn't disposed...
        }

        private void UncheckAll()
        {
            CurrentdMU = false;
            CurrentDR = false;
            CurrentGS = false;
            PreviewdMU = false;
            PreviewDR = false;
            PreviewGS = false;
        }

        private bool CanEditDR()
        {
            return (SelectedMethod != null) && (SelectedCourse != null) && (SelectedPlan != null);
        }

        private void OnOpenPatient() // 2) in dcmd
        {
            _app.ClosePatient();
            var pat = _app.OpenPatientById(PatientId);

            if (pat == null)
            {
                System.Windows.MessageBox.Show($"Could not find: {PatientId}. Please select another patient.");
                return;
            }

            // List all courses
            OpenPatient(pat);
        }

        private void OpenPatient(Patient pat)
        {
            Courses.Clear();
            Plans.Clear();
            Beams.Clear();
            SelectedCourse = null;
            SelectedPlan = null;
            SelectedMethod = null;

            foreach (Course crs in pat.Courses)
            {
                Courses.Add(new CourseModel(crs));
            }
        }

        private bool CanOpenPatient()
        {
            return true; // For now just say we can and handle consequences
        }

        private void OnSelectCourse()
        {
            // To be called by dcmd when course is selected
            //MessageBox.Show($@"Calling OnSelect course for {SelectedCourse.Id}");

            Plans.Clear();
            if (SelectedCourse != null)
            {
                foreach (var p in SelectedCourse.Plans.Values)
                {
                    Plans.Add(p);
                }
            }

            ResetPlot();

            Beams.Clear();
        }

        private bool CanSelectCourse()
        {
            return true;
        }

        private void ResetPlot()
        {
            // Clear plot
            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GS0_series.Points.Clear();
            GSf_series.Points.Clear();
            dMU0_series.Points.Clear();
            dMUf_series.Points.Clear();

            DRPlot.InvalidatePlot(true);
        }

        #region <---MCB

        private static string defaultTemplateId = "Template Id";

        private string _newBeamTemplateId = defaultTemplateId;

        public string NewBeamTemplateId
        {
            get { return _newBeamTemplateId; }
            set { SetProperty(ref _newBeamTemplateId, value); }
        }

        private BeamTemplate _selectedBeamTemplate = new BeamTemplate();

        public BeamTemplate SelectedBeamTemplate
        {
            get { return _selectedBeamTemplate; }
            set { SetProperty(ref _selectedBeamTemplate, value); }
        }

        private ObservableCollection<BeamTemplate> _beamTemplates = new ObservableCollection<BeamTemplate>();

        public ObservableCollection<BeamTemplate> BeamTemplates
        {
            get { return _beamTemplates; }
            set
            {
                SetProperty(ref _beamTemplates, value);
            }
        }

        public int n = 0;
        public ICommand InsertBeamsCommand => new RelayCommand(InsertBeams);

        public ICommand CreateTemplateCommand => new RelayCommand(CreateBeamTemplate);

        public ICommand DeleteTemplateCommand => new RelayCommand(DeleteBeamTemplate);

        private void DeleteBeamTemplate()
        {
            if (MessageBox.Show("Are you sure you wish to delete the Beam Template?", "Delete?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No
                )
            {
                return;
            }

            if (SelectedBeamTemplate != null)
            {
                if (BeamTemplates.Contains(SelectedBeamTemplate))
                {
                    BeamTemplates.Remove(SelectedBeamTemplate);
                }
            }

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();

            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);

            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");
        }

        private void InsertBeams()
        {



            if(SelectedBeamTemplate.BeamInfos.Count()==0)
            {
                MessageBox.Show("There are no beams in the chosen template. Choose another and try again.",
                    "Error!",MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string fieldErrorMessage = "The plan in Eclipse must contain a field, with:\n" +
                    "*MLC of model type: Varian High Definition 120\n" +
                    "*Energy\n" +
                    "*Dose Rate\n" +
                    "*Isocenter\n" +
                    "*Machine";

            if (SelectedPlan.Beams.Where(x=>!x.IsSetupField).Count()==0) 
            {
                MessageBox.Show(fieldErrorMessage,"Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var firstBeam = SelectedPlan.Beams.First(x => !x.IsSetupField);

            if(firstBeam.MLC == null || firstBeam.MLC.Model != "Varian High Definition 120")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (firstBeam.EnergyModeDisplayName == null || firstBeam.EnergyModeDisplayName == "")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (firstBeam.TreatmentUnit == null || firstBeam.TreatmentUnit.Id == "")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var origBeams = SelectedPlan.Beams.ToList();
            var machine = firstBeam.TreatmentUnit.Id;
            var mlcId = firstBeam.MLC.Id;
            var isocenter = firstBeam.IsocenterPosition;
            var energyDisp = firstBeam.EnergyModeDisplayName;
            var doseRate = firstBeam.DoseRate;

            SelectedPlan.Course.Patient.BeginModifications();
            var removeBeamList = SelectedPlan.Beams.ToList();

            foreach(var rb in removeBeamList)
            {
                SelectedPlan.RemoveBeam(rb);
            }

            string fluence = "";
            if (energyDisp.ToUpper().Contains("FFF"))
                fluence = "FFF";
            if (energyDisp.ToUpper().Contains("SRS"))
                fluence = "SRS";

            var setEnergy = energyDisp;

            if (fluence == "FFF")
                setEnergy = setEnergy.Replace("-FFF", "");

            foreach (var beam in SelectedBeamTemplate.BeamInfos)
            {
                GantryDirection direction = GantryDirection.None;
                if (beam.GantryRotation == GantryRotation.CCW)
                    direction = GantryDirection.CounterClockwise;
                if (beam.GantryRotation == GantryRotation.CW)
                    direction = GantryDirection.Clockwise;

                string technique = "STATIC";
                if (beam.TreatmentTechnique == TreatmentTechnique.SRS_ARC || beam.TreatmentTechnique == TreatmentTechnique.ARC)
                    technique = "ARC";

                var externalParams = new ExternalBeamMachineParameters(machine,
                    setEnergy, doseRate, technique, fluence);

                Beam field = null;

                // add sample amount of control points for the beam before fitting the MLC
                List<double> cps = new List<double>();

                try
                {
                    if (!beam.IsSetup)
                    {
                        // VMAT
                        if (beam.TreatmentTechnique == TreatmentTechnique.ARC)
                        {
                            for (int i = 0; i < 72; i++)
                            {
                                cps.Add(i);
                            }

                            double tableAngle = beam.Table ?? 0.0;

                            field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value,
                                beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);

                            if (field.TreatmentUnit.MachineScaleDisplayName.ToUpper().Equals("VARIAN IEC"))
                            {
                                tableAngle = ConvertBetweenIEC61217andVarianIEC(tableAngle);
                                SelectedPlan.RemoveBeam(field);
                                field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value,
                                    beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);
                            }

                            double JawX = GapSettings.X / 2;
                            double JawY = GapSettings.Y / 2;

                            var editParams = field.GetEditableParameters();
                            editParams.SetJawPositions(new VRect<double>(-JawX,-JawY,JawX,JawY));
                            field.ApplyParameters(editParams);



                            if (field.MLC.Model == "Varian High Definition 120")
                            {
                                // Get the cps from beam
                                var edits = field.GetEditableParameters();
                                edits.SetAllLeafPositions(GetMLCLeafPositions(field.MLC.Model));
                                field.ApplyParameters(edits);
                            }
                        }

                        // SRS ARC
                        if (beam.TreatmentTechnique == TreatmentTechnique.SRS_ARC)
                        {
                            for (int i = 0; i < 72; i++)
                            {
                                cps.Add(i);
                            }

                            double tableAngle = beam.Table ?? 0.0;
                            field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value, beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);
                            if (field.TreatmentUnit.MachineScaleDisplayName.ToUpper().Equals("VARIAN IEC"))
                            {
                                tableAngle = ConvertBetweenIEC61217andVarianIEC(tableAngle);
                                SelectedPlan.RemoveBeam(field);
                                field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value, beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);
                            }



                            double JawX = GapSettings.X / 2;
                            double JawY = GapSettings.Y / 2;



                            var editParams = field.GetEditableParameters();
                            editParams.SetJawPositions(new VRect<double>(-JawX, -JawY, JawX, JawY));
                            field.ApplyParameters(editParams);



                            // Get the cps from beam
                            var edits = field.GetEditableParameters();
                            edits.SetAllLeafPositions(GetMLCLeafPositions(field.MLC.Model));
                            field.ApplyParameters(edits);
                        }
                        try
                        {
                            field.Id = beam.BeamID;
                        }
                        catch (Exception ex)
                        { MessageBox.Show(ex.Message);
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    //exception messagebox
                    MessageBox.Show($"1254:{ex.Message}");

                }

                _app.SaveModifications();
            }

            _app.SaveModifications();

            OnPlanSelect();
            MessageBox.Show("The fields have been inserted.", "Complete!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private float[,] GetMLCLeafPositions(string MLC_Model)
        {
            float negSide = 0;
            float posSide = 0;

            double halfGap = GapSettings.GapSize / 2;
            negSide =(float)-halfGap;
            posSide = (float)halfGap;

            if (MLC_Model == "Varian High Definition 120")
            {
                // Build leaf bank
                var leaves = new float[2, 60];
                for (int i = 0; i < 60; i++)
                {
                    leaves[0, i] = -37.5F;
                    leaves[1, i] = -37.5F;
                }
                leaves[0, 30] = negSide;
                leaves[1, 30] = posSide;
                leaves[0, 29] = negSide;
                leaves[1, 29] = posSide;

                return leaves;
            }

            return null;
        }

        private void CreateBeamTemplate()
        {
            if (NewBeamTemplateId == "")
            {
                MessageBox.Show($"The candidate name is blank.  Pleaes enter an Id and try again.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (BeamTemplates.Select(x => x.BeamTemplateId.ToUpper()).Contains(NewBeamTemplateId.ToUpper()))
            {
                MessageBox.Show($"That Id is already in use.  Please choose a unique Id and try again.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NewBeamTemplateId.ToUpper().Equals(defaultTemplateId.ToUpper()))
            {
                MessageBox.Show($"'Template Id' cannot be used.  Please choose a unique Id and try again.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Extract beam information from selected plan
            BeamTemplate beamTemplate = new BeamTemplate();
            beamTemplate.BeamInfos = GetBeamInfos();
            beamTemplate.BeamTemplateId = NewBeamTemplateId;

            BeamTemplates.Add(beamTemplate);

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();

            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);

            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");
        }

        private void SerializeToXmlFile(BeamTemplatesCollection beamTemplatesCollection, string fileName)
        {
            try
            {
                string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(currentDirectory, fileName);

                XmlSerializer serializer = new XmlSerializer(typeof(BeamTemplatesCollection));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, beamTemplatesCollection);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception if any during the serialization process
                MessageBox.Show($"An error occurred while trying to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadBeamTemplates()
        {
            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(currentDirectory, "BeamTemplateCollections.xml");

            XmlSerializer serializer = new XmlSerializer(typeof(BeamTemplatesCollection));

            BeamTemplatesCollection beamTemplateCollection = new BeamTemplatesCollection();

            using (StreamReader reader = new StreamReader(filePath))
            {
                beamTemplateCollection = (BeamTemplatesCollection)serializer.Deserialize(reader);
            }

            foreach (var b in beamTemplateCollection.Templates)
            {
                BeamTemplates.Add(FastDeepCloner.DeepCloner.Clone(b));
            }
        }

        public void DebugMessageBox(string Message)
        {
            MessageBox.Show(Message);
        }

        public List<BeamInfo> GetBeamInfos()
        {
            if (SelectedPlan != null)
            {
                var bis = new List<BeamInfo>();
                foreach (var b in SelectedPlan.Beams)
                {
                    BeamInfo beam = new BeamInfo()
                    {
                        IsSetup = b.IsSetupField,
                        BeamID = b.Id,
                        Collimator = b.CollimatorAngleToUser(b.ControlPoints.First().CollimatorAngle),
                        DoseRate = b.DoseRate,

                        EnergyValue = BeamInfo.SetEnergyValue(b.EnergyModeDisplayName),
                        EnergyDisplay = b.EnergyModeDisplayName,

                        GantryRotation = BeamInfo.GantryRotationSelector(b.GantryDirection.ToString()),
                        GantryStart = b.GantryAngleToUser(b.ControlPoints.FirstOrDefault().GantryAngle),
                        GantryStop = b.GantryAngleToUser(b.ControlPoints.LastOrDefault().GantryAngle),

                        NumberOfControlPoints = b.ControlPoints.Count(),

                        IsocenterCoordinates = new IsocenterCoordinates()
                        {
                            X = b.IsocenterPosition.x,
                            Y = b.IsocenterPosition.y,
                            Z = b.IsocenterPosition.z,
                        },

                        Table = b.PatientSupportAngleToUser(b.ControlPoints.First().PatientSupportAngle),
                        ToleranceTable = b.ToleranceTableLabel,
                        X1 = b.ControlPoints.FirstOrDefault().JawPositions.X1,
                        Y1 = b.ControlPoints.FirstOrDefault().JawPositions.Y1,
                        X2 = b.ControlPoints.FirstOrDefault().JawPositions.X2,
                        Y2 = b.ControlPoints.FirstOrDefault().JawPositions.Y2,

                        // set some defaults for the user. Can change in the XML
                        StructureFitting = new StructureFitting()
                        {
                            JawFittingMode = "1",
                            OpenMLCMeetingPoint = OpenMLCMeetingPoint.Inside,
                            CloseMLCMeetingPoint = CloseMLCMeetingPoint.BankB,
                            Left = 5.0,
                            Right = 5.0,
                            Top = 5.0,
                            Bottom = 5.0,
                            OptimizeCollimator = false,
                            SymmetricMargin = true
                            //,TargetVolume = pp.TargetVolume
                        },

                        TreatmentTechnique = BeamInfo.TechniqueSelector(b.Technique.ToString()),
                    };

                    bis.Add(beam);
                }

                return bis;
            }

            return null;
        }

        public void WriteToFullLog(string message)
        {
            string callingMethodName = "NA";
            try
            {
                // Get the stack trace for the current thread
                StackTrace stackTrace = new StackTrace();
                // Get the calling method from the stack trace (the method at index 1, since the method at index 0 is WriteToFullLog itself)
                StackFrame callingFrame = stackTrace.GetFrame(1);
                MethodBase callingMethod = callingFrame.GetMethod();
                callingMethodName = callingMethod.Name;
                //// Combine the calling method information with the message
                //string fullMessage = $"{callingMethod.DeclaringType.FullName}.{callingMethod.Name}: {message}";
            }
            catch
            {
            }

            try
            {
                string executablePath = AppDomain.CurrentDomain.BaseDirectory;
                string FullProgressLogPath = Path.Combine(executablePath, "FullLog.txt");
                string line = $"{DateTime.Now},{callingMethodName}: {message}";

                using (StreamWriter writer = new StreamWriter(FullProgressLogPath, true))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }

        #endregion <---MCB

        #region <---Scale Conversions

        public static double ConvertBetweenIEC61217andVarianIEC(double originalAngle)
        {
            double resultAngle = 360 - originalAngle;

            if (resultAngle == 360)
            {
                return 0;
            }

            return resultAngle;
        }

        public static double ConvertBetweenIEC61217andVarianStandard(double originalAngle)
        {
            // Subtract the original angle from 180 to mirror it
            double mirroredAngle = 180 - originalAngle;

            // If the result is negative, add 360 to bring it within the 0-360 range
            if (mirroredAngle < 0)
            {
                mirroredAngle += 360;
            }

            return mirroredAngle;
        }

        #endregion <---Scale Conversions
    }
}