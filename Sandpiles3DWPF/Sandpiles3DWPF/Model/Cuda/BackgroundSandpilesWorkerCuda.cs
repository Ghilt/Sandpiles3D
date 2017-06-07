using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandpiles3DWPF.ViewModel;

namespace Sandpiles3DWPF.Model.Cuda
{
    class BackgroundSandpilesWorkerCuda : BackgroundSandpilesWorker
    {

        SandpilesCalculatorCuda cudaModel;

        public BackgroundSandpilesWorkerCuda(SandpilesViewModel.VisualizationMode visualizationMode, PropertyChangedEventHandler propertyChangedListener, SandpilesCalculatorCuda model) 
            : base(visualizationMode, propertyChangedListener, model)
        {
            cudaModel = model;
        }

        public override void PrepareForWork() {
            cudaModel.loadKernel();
        }

        public override void TearDownAfterWorkPerformed() {
            cudaModel.disposeKernel();
        }

    }   
}
