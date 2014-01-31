using System;

namespace BootLoader.Impl
{
    public class TimerEventArg : EventArgs
    {
        public DateTime when = DateTime.Now;
        public TimerEventArg() {
            when = DateTime.Now;
        }
    }
}