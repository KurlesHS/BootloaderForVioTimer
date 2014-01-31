using System.Timers;
using BootLoader.Interfaces;

namespace BootLoader.Impl
{
    public class Timer : ITimer
    {
        private readonly System.Timers.Timer _timer = new System.Timers.Timer();

        public Timer() {
            _timer.Elapsed += _timer_Elapsed;    
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e) {
            Elapsed(this, new TimerEventArg());
        }   
        public void Dispose() {
            _timer.Dispose();
        }

        public void Start(double interval) {
            _timer.Interval = interval;
            _timer.Start();
        }

        public void Stop() {
            _timer.Stop();
        }

        public event TimerEventHandler Elapsed;

    }
}