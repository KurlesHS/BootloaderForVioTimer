using System;
using BootLoader.Impl;

namespace ForTesting
{
    static class Program
    {
        static void Main(string[] args) {
            var timer = new Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Start(1000);
            Console.ReadKey();

        }

        static void timer_Elapsed(object sender, TimerEventArg e)
        {
            Console.WriteLine(e.when);
        }
    }
}
