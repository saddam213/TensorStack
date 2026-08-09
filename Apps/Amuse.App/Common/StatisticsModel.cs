using Amuse.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using TensorStack.WPF;

namespace Amuse.App.Common
{
    public sealed class StatisticsModel : BaseModel
    {
        private readonly DispatcherTimer _dispatcherTimer;
        private readonly List<float> _perSecond = [];
        private readonly List<float> _secondPer = [];
        private float _iterationsPerSecond;
        private float _secondsPerIteration;
        private long _timestamp;
        private TimeSpan _elapsed;
        private TimeSpan _firstAction;
        private float _prefillPerSecond;

        public StatisticsModel(Dispatcher dispatcher)
        {
            _dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, UpdateProgress, dispatcher);
            _dispatcherTimer.Stop();
        }

        public TimeSpan Elapsed
        {
            get { return _elapsed; }
            set { SetProperty(ref _elapsed, value); }
        }

        public float IterationsPerSecond
        {
            get { return _iterationsPerSecond; }
            set { SetProperty(ref _iterationsPerSecond, value); }
        }

        public float SecondsPerIteration
        {
            get { return _secondsPerIteration; }
            set { SetProperty(ref _secondsPerIteration, value); }
        }

        public TimeSpan FirstAction
        {
            get { return _firstAction; }
            set { SetProperty(ref _firstAction, value); }
        }

        public float PrefillPerSecond
        {
            get { return _prefillPerSecond; }
            set { SetProperty(ref _prefillPerSecond, value); }
        }

        public void Start()
        {
            _timestamp = Stopwatch.GetTimestamp();
            _dispatcherTimer.Start();
        }

        public void Stop()
        {
            _dispatcherTimer.Stop();
            UpdateProgress(default, default);
        }

        public void Clear()
        {
            Stop();
            _timestamp = 0;
            _perSecond.Clear();
            _secondPer.Clear();
            IterationsPerSecond = 0;
            SecondsPerIteration = 0;
            Elapsed = TimeSpan.Zero;
            FirstAction = TimeSpan.Zero;
            PrefillPerSecond = 0;
        }

        public void UpdateStep(PipelineProgress progress)
        {
            _perSecond.Add(progress.IterationsPerSecond);
            _secondPer.Add(progress.SecondsPerIteration);
        }

        public void UpdateToken(PipelineProgress progress)
        {
            if (FirstAction == TimeSpan.Zero)
            {
                FirstAction = TimeSpan.FromMilliseconds(progress.Elapsed);
                PrefillPerSecond = (progress.Elapsed / progress.Value) * 1000;
                return;
            }

            _perSecond.Add(progress.IterationsPerSecond);
            _secondPer.Add(progress.SecondsPerIteration);
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (_timestamp == 0)
                return;

            Elapsed = Stopwatch.GetElapsedTime(_timestamp);
            IterationsPerSecond = AverageExcludingMinMax(_perSecond);
            SecondsPerIteration = AverageExcludingMinMax(_secondPer);
        }


        static float AverageExcludingMinMax(List<float> values)
        {
            if (values.Count == 0)
                return 0;
            if (values.Count <= 2)
                return values.Average();

            float min = values[0];
            float max = values[0];
            float sum = values[0];

            for (int i = 1; i < values.Count; i++)
            {
                float v = values[i];
                sum += v;

                if (v < min)
                    min = v;

                if (v > max)
                    max = v;
            }

            return (sum - min - max) / (values.Count - 2);
        }
    }
}
