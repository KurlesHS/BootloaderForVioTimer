﻿using System;
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
