using Sandpiles3DWPF.Extensions;
using Sandpiles3DWPF.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Sandpiles3DWPF.ViewModel
{
    //http://stackoverflow.com/questions/1315621/implementing-inotifypropertychanged-does-a-better-way-exist/1316566#1316566
    class SandpilesViewModel : INotifyPropertyChanged
    {

        private SandpilesCalculator model;
        private BackgroundSandpilesWorker worker;

        #region Visualization control

        public enum VisualizationMode
        {
            Flatten, CrossSection, ThreeDimensions
        }

        private VisualizationMode visualization;
        public VisualizationMode Visualization
        {
            get { return visualization; }
            set { visualization = value; OnPropertyChanged(); }
        }

        private ICommand visualizationChangedCommand;
        public ICommand VisualizationChangedCommand
        {
            get
            {
                return visualizationChangedCommand = visualizationChangedCommand ?? new RelayCommand(p => worker.SetVisualizationMode(visualization));
            }
        }

        #endregion //Visualization

        #region Size control

        public ObservableStringAccessor<int>[] SizeDim { get; set; }

        private ICommand setSizeCommand;
        public ICommand SetSizeCommand
        {
            get
            {
                return setSizeCommand = setSizeCommand ?? new RelayCommand(p => ReSize(SizeDim[0].Value, SizeDim[1].Value, SizeDim[2].Value));
            }
        }

        #endregion //Size

        #region Quick menu control

        private ICommand[] quickMenuCommand; // make this data fill a data grid later google datagrid
        public ICommand[] QuickMenuCommand
        {
            get
            {
                return quickMenuCommand = quickMenuCommand ?? CreateQuickCommandList();
            }
        }

        private ICommand[] CreateQuickCommandList()
        {
            return new ICommand[]{
             new RelayCommand("Fill max",p => model.FillMax()),
             new RelayCommand("Fill 7",p => model.Fill(7)),
             new RelayCommand("Mid 7", p => model.SetPosition(model.getMidX(), model.getMidY(), model.getMidZ(), 7)),
             new RelayCommand("Mid 100", p => model.SetPosition(model.getMidX(), model.getMidY(), model.getMidZ(), 100)),
             new RelayCommand("RGB 7", p => {
                    model.FillValues(new bool[] { false, true, true }, new int[] { 0, (int)(model.height * 0.1), (int)(model.depth * 0.1) }, 7);
                    model.FillValues(new bool[] { false, true, true }, new int[] { 0, (int)(model.height * 0.5), (int)(model.depth * 0.5) }, 7);
                    model.FillValues(new bool[] { false, true, true }, new int[] { 0, (int)(model.height * 0.9), (int)(model.depth * 0.9) }, 7);
             }),
            new RelayCommand("Grid 7", p => {
                    model.FillValues(new bool[] { false, true, true }, new int[] { 0, (int)(model.height * 0.1), (int)(model.depth * 0.1) }, 7);
                    model.FillValues(new bool[] { false, true, true }, new int[] { 0, (int)(model.height * 0.9), (int)(model.depth * 0.3) }, 7);
                    model.FillValues(new bool[] { true, false, true }, new int[] { (int)(model.width * 0.9), 0, (int)(model.depth * 0.7) }, 7);
                    model.FillValues(new bool[] { true, false, true }, new int[] { (int)(model.width * 0.1), 0, (int)(model.depth * 0.9) }, 7);
             })};
        }

        #endregion //Quick

        #region Advanced set value control

        public ObservableField<bool>[] CoordEnabled { get; set; }

        public ObservableStringAccessor<int>[] CoordPosition { get; set; }

        private int setCoordValue;
        public string SetCoordValue
        {
            get { return "" + setCoordValue; }
            set { setCoordValue = Int32.Parse(value); OnPropertyChanged(); }
        }

        private ICommand setCoordValueCommand;
        public ICommand SetCoordValueCommand
        {
            get
            {
                return setCoordValueCommand = setCoordValueCommand ?? new RelayCommand(p => model.FillValues(
                    new bool[] { CoordEnabled[0].Value, CoordEnabled[1].Value, CoordEnabled[2].Value },
                    new int[] { CoordPosition[0].Value, CoordPosition[1].Value, CoordPosition[2].Value },
                    setCoordValue));
            }
        }

        #endregion //Advanced

        #region Iteration control

        private ICommand startIterationCommand;
        public ICommand StartIterationCommand
        {
            get
            {
                return startIterationCommand = startIterationCommand ?? new RelayCommand(p => { worker.StartIteration(); IsIterating = true; });
            }
        }

        private ICommand stopIterationCommand;
        public ICommand StopIterationCommand
        {
            get
            {
                return stopIterationCommand = stopIterationCommand ?? new RelayCommand(p => worker.StopIteration());
            }
        }

        private ICommand iterateOneCommand;
        public ICommand IterateOneCommand
        {
            get
            {
                return iterateOneCommand = iterateOneCommand ?? new RelayCommand(p => worker.Iterate());
            }
        }

        private bool isIterating;
        public bool IsIterating
        {
            get { return isIterating; }
            set { isIterating = value; OnPropertyChanged(); }
        }

        #endregion // iteration

        #region Render control

        private WriteableBitmap image2D;
        public WriteableBitmap Image2D
        {
            get
            {
                return image2D = image2D ?? BitmapFactory.New(model.width, model.height);
            }
            set { image2D = value; OnPropertyChanged(); }
        }

        private int iterationDuration;
        public string IterationDuration
        {
            get { return "" + iterationDuration; }
            set { iterationDuration = Int32.Parse(value); OnPropertyChanged(); }
        }

        private int numberOfIterations;
        public string NumberOfIterations
        {
            get { return "" + numberOfIterations; }
            set { numberOfIterations = Int32.Parse(value); OnPropertyChanged(); }
        }

        private int rotation3DRender;
        public int Rotation3DRender
        {
            get { return rotation3DRender; }
            set { rotation3DRender = value; OnPropertyChanged(); rotate3DRepresentaition(Render3D, value); }
        }

        private CubeGrid render3D;

        public CubeGrid Render3D
        {
            get
            {
                return render3D;
            }
            set { render3D = value; OnPropertyChanged(); }
        }

        private void rotate3DRepresentaition(CubeGrid grid, int value)
        {
            RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            myAxisAngleRotation3d.Axis = new Vector3D(1, 2, 3); //temporary debug 
            myAxisAngleRotation3d.Angle = value;
            myRotateTransform3D.Rotation = myAxisAngleRotation3d;
            grid.Grid.Transform = myRotateTransform3D;
        }

        #endregion // render

        public SandpilesViewModel()
        {
            SizeDim = ObservableStringAccessor<int>.CreateFields(x => Int32.Parse(x), 3);
            CoordPosition = new ObservableStringAccessor<int>[3];
            for (int i = 0; i < CoordPosition.Length; i++)
            {
                var localVariable = i; // Asked a question about this https://stackoverflow.com/questions/43388174/c-sharp-value-sent-to-func-not-peristant-c-sharp-iife-equivalent
                CoordPosition[i] = new ObservableStringAccessor<int>(x => CapStringNumber(x, localVariable));
            }
            string notifyListOnItemChanged = nameof(CoordEnabled);
            CoordEnabled = ObservableField<bool>.CreateFields(3, () => OnPropertyChanged(notifyListOnItemChanged));

        }

        public void LoadSandpiles(int width, int height, int depth)
        {
            System.Diagnostics.Debug.WriteLine("Loading sandpiles: width(" + width + "), height(" + height + "), depth(" + depth + ")");
            model = new SandpilesCalculator(ModelPropertyChanged, width, height, depth);
            worker = new BackgroundSandpilesWorker(visualization, ModelPropertyChanged, model);
            Create3DGridInBackground(width, height, depth);
        }

        public void ReSize(int width, int height, int depth) // Find better way to reset to initial state
        {
            //Process.Start(Application.ResourceAssembly.Location, width + "," + height + "," + depth);
            //System.Windows.Application.Current.Shutdown();
            visualizationChangedCommand = null;
            setSizeCommand = null;
            startIterationCommand = null;
            stopIterationCommand = null;
            setCoordValueCommand = null;
            iterateOneCommand = null;
            worker.StopIteration();
            worker.PropertyChanged -= ModelPropertyChanged;
            model.PropertyChanged -= ModelPropertyChanged;
            IsIterating = false;
            Image2D = BitmapFactory.New(width, height);
            LoadSandpiles(width, height, depth);
        }

        private void Create3DGridInBackground(int width, int height, int depth)
        {
            BackgroundWorker w = new BackgroundWorker();
            w.DoWork += SetUp3DGrid;
            w.RunWorkerCompleted += SetUp3DGridComplete;
            w.RunWorkerAsync(Tuple.Create(width, height, depth));
        }

        private void SetUp3DGrid(object sender, DoWorkEventArgs e)
        {
            Tuple<Int32, Int32, Int32> dimensions = e.Argument as Tuple<Int32, Int32, Int32>;
            CubeGrid g = new CubeGrid(dimensions.Item1, dimensions.Item2, dimensions.Item3);
            g.Initiate();
            g.Freeze();
            e.Result = g;
        }

        private void SetUp3DGridComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            CubeGrid g = e.Result as CubeGrid;
            g.Unfreeze();
            Render3D = g;
        }

        private int CapStringNumber(string numberString, int sizePosition)
        {
            int number;
            Int32.TryParse(numberString, out number);
            int maxNumber = SizeDim[sizePosition].Value;
            return number >= maxNumber ? maxNumber - 1 : number;
        }

        private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) // TODO not sure if this is correct, may be missing all the value of mvvm. Something in me wanna connect the bound fields more directly to the fields in the model, with no backing fields for the accessors
        {
            string triggerMethod = e.PropertyName;
            if (null != sender as SandpilesCalculator)
            {
                SandpilesCalculator m = sender as SandpilesCalculator;
                SizeDim[0].Value = m.width;
                SizeDim[1].Value = m.height;
                SizeDim[2].Value = m.depth;
            }
            else if (triggerMethod == BackgroundSandpilesWorker.PROPERTY_CHANGED_ITERATION_FINISHED)
            {
                BackgroundSandpilesWorker w = sender as BackgroundSandpilesWorker;
                IterationDuration = "" + w.lastIterationDuration;
                NumberOfIterations = "" + w.iterationData.iteration;
                DrawSandpiles(w.iterationData);
            }
            else if (triggerMethod == BackgroundSandpilesWorker.PROPERTY_CHANGED_CONTINUOUS_ITERATION_STOPPED)
            {
                IsIterating = false;
            }
        }

        public void DrawSandpiles(SandpilesIterationData data)
        {
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread); // run on UI-thread
            if (data.data3D != null)
            {
                if (dispatcher != null && dispatcher.CheckAccess())
                {
                    Update3DRender(data.data3D);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => Update3DRender(data.data3D));
                }
            }
            else if (data.dim2Projection != null)
            {
                if (dispatcher != null && dispatcher.CheckAccess())
                {
                    Update2DRender(data.dim2Projection);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => Update2DRender(data.dim2Projection));
                }
            }
        }

        private void Update3DRender(int[,,] data3D)
        {
            render3D.Update(data3D);
        }

        private void Update2DRender(Color[,] projection)
        {
            using (Image2D.GetBitmapContext()) //Is this using doing something?
            {
                for (int x = 0; x < Image2D.Width; x++)
                {
                    for (int y = 0; y < Image2D.Height; y++)
                    {
                        Image2D.SetPixel(x, y, projection[x, y]);
                    }
                }
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
