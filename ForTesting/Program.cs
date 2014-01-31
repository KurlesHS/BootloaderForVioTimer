using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
