using System;
using BootLoader.Impl;
using BootLoader.Interfaces;


namespace BootLoaderUnitTestProject
{
    public class FakeTimer : ITimer
    {
        private double _interval;
        private double _currentTime;
        private bool _isStarted;
        public void Dispose() {
            
        }

        public void Start(double interval) {
            _interval = interval;
            _currentTime = 0.0;
            _isStarted = true;
        }

        public void Advance(double interval) {
            var prevCount = _currentTime/_interval;
            _currentTime += interval;
            var lastCount = _currentTime/_interval;
            var count = (int)(lastCount - prevCount);
            if (!_isStarted) return;
            for (var x = 0; x < count; ++x) {
                Elapsed(this, new TimerEventArg());
            }
        }

        public void Stop() {
            _isStarted = false;
        }

        public event TimerEventHandler Elapsed;
        public object Clone() {
            return this.MemberwiseClone();
        }
    }
}