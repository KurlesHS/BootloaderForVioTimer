using System;
using System.Timers;
using BootLoader.Interfaces;

namespace BootLoader.Impl
{
    public class Timer : ITimer
    {
        private System.Timers.Timer _timer = new System.Timers.Timer();
        private bool _disposed;

        public Timer() {
            _disposed = false;
            _timer.Elapsed += _timer_Elapsed;
        }

        ~Timer() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (_disposed) return;
            if (disposing) {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Dispose();    
            }
            _timer = null;    
            _disposed = true;
        }
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed(this, new TimerEventArg());
        }

        public void Start(double interval) {
            _timer.Interval = interval;
            _timer.Start();
        }

        public void Stop() {
            _timer.Stop();
        }

        public event TimerEventHandler Elapsed;

        public object Clone() {
            return MemberwiseClone();
        }
    }
}