using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using BootLoader.Impl;

namespace BootLoader.Interfaces
{
    public delegate void TimerEventHandler(object sender, TimerEventArg e);
    public interface ITimer : IDisposable
    {
        void Start(double interval);
        void Stop();
        event TimerEventHandler Elapsed;
    }
}
