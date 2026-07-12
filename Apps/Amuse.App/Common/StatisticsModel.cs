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
        private float _iterationsPerSecond;
        private float _secondsPerIteration;
        private long _timestamp;
        private TimeSpan _elapsed;
        private List<float> _perSecond;
        private List<float> _secondPer;

        public StatisticsModel(Dispatcher dispatcher)
        {
            _perSecond = new List<float>();
            _secondPer = new List<float>();
            _dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, UpdateProgress, dispatcher);
            _dispatcherTimer.Stop();
        }

        public void Start()
        {
            _timestamp = Stopwatch.GetTimestamp();
            _dispatcherTimer.Start();
        }

        public void Stop()
        {
            _dispatcherTimer.Stop();
            Elapsed = Stopwatch.GetElapsedTime(_timestamp);
        }

        public void Clear()
        {
            Stop();
            _perSecond.Clear();
            _secondPer.Clear();
            _timestamp = 0;
            IterationsPerSecond = 0;
            SecondsPerIteration = 0;
            Elapsed = TimeSpan.Zero;
        }

        public void Update(PipelineProgress progress)
        {
            _perSecond.Add(progress.IterationsPerSecond);
            _secondPer.Add(progress.SecondsPerIteration);
            IterationsPerSecond = AverageExcludingMinMax(_perSecond);
            SecondsPerIteration = AverageExcludingMinMax(_secondPer);
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


        private void UpdateProgress(object sender, EventArgs e)
        {
            if (_timestamp == 0)
                return; ;

            Elapsed = Stopwatch.GetElapsedTime(_timestamp);
        }


        static float AverageExcludingMinMax(List<float> values)
        {
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
