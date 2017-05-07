using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Sandpiles3DWPF.ViewModel;
using static Sandpiles3DWPF.ViewModel.SandpilesViewModel;

namespace Sandpiles3DWPF.Model
{
    class BackgroundSandpilesWorker : INotifyPropertyChanged
    {
        public const string PROPERTY_CHANGED_ITERATION_FINISHED = "Property_changed_iteration_finished";
        public const string PROPERTY_CHANGED_CONTINUOUS_ITERATION_STOPPED = "Property_changed_continuous_cancelled";

        public event PropertyChangedEventHandler PropertyChanged;
        private SandpilesCalculator model;
        private BackgroundWorker bw;
        private VisualizationMode visualizationMode;

        public long lastIterationDuration { get; private set; }
        public SandpilesIterationData iterationData { get; private set; }

        public BackgroundSandpilesWorker(VisualizationMode visualizationMode, PropertyChangedEventHandler propertyChangedListener, SandpilesCalculator model)
        {
            this.model = model;
            this.visualizationMode = visualizationMode;
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            PropertyChanged += propertyChangedListener;
        }


        internal void Iterate()
        {
            bw.DoWork += PerformSingleIteration;
            bw.RunWorkerCompleted += IterationFinished;
            bw.RunWorkerAsync();
        }

        internal void StartIteration()
        {
            bw.DoWork += PerformContinousIteration;
            bw.ProgressChanged += IterationFinished;
            bw.RunWorkerAsync();
        }

        internal void StopIteration()
        {
            bw.CancelAsync();
        }

        internal void PerformSingleIteration(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            model.Iterate();
            watch.Stop();
            lastIterationDuration = watch.ElapsedMilliseconds;
            e.Result = getIterationReturnData();
        }

        private void PerformContinousIteration(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                model.Iterate();
                watch.Stop();
                lastIterationDuration = watch.ElapsedMilliseconds;
                worker.ReportProgress(0, getIterationReturnData());
                if (lastIterationDuration < 20 && model.IsStable())
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            OnPropertyChanged(PROPERTY_CHANGED_CONTINUOUS_ITERATION_STOPPED);
            workerStoppedClearCallbacks();
            e.Cancel = true; // what does this do?
        }

        private SandpilesIterationData getIterationReturnData()
        {
            switch (visualizationMode)
            {
                case VisualizationMode.Flatten:
                    return model.Get2DProjection();
                case VisualizationMode.CrossSection:
                    return model.GetCrossSection(model.getMidZ(), false, false, true);
                case VisualizationMode.ThreeDimensions:
                    return model.GetBinary3DRepresentation();
                default:
                    return null;
            }
        }

        private void workerStoppedClearCallbacks()
        {
            //Remove all possible callbacks
            bw.DoWork -= PerformSingleIteration;
            bw.RunWorkerCompleted -= IterationFinished;
            bw.DoWork -= PerformContinousIteration;
            bw.ProgressChanged -= IterationFinished;
        }

        private void IterationFinished(object sender, ProgressChangedEventArgs e)
        {
            iterationData = e.UserState as SandpilesIterationData;
            OnPropertyChanged(PROPERTY_CHANGED_ITERATION_FINISHED);
        }

        private void IterationFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            iterationData = e.Result as SandpilesIterationData;
            workerStoppedClearCallbacks();
            OnPropertyChanged(PROPERTY_CHANGED_ITERATION_FINISHED);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //invoke if not null
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetVisualizationMode(VisualizationMode mode)
        {
            visualizationMode = mode;
        }
    }
}
