
using System;
using System.Timers;

namespace BootLoader
{
    class TimeoutTimer : Timer
    {
        public TimeoutTimer() : base()
        {
        }

        public Action ErrorHandler { get; set; }
        public string ErrorMessage { get; set; }

    }
}
