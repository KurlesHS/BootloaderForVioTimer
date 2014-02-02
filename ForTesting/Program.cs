using System;
using System.Threading;
using BootLoader.Impl;
using Timer = BootLoader.Impl.Timer;

namespace ForTesting
{
    static class Program
    {
        static void Main() {
            
            var timer = new Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Start(1000);
            var worker = new Worker();
            var thread = new Thread(worker.Run);
            
            thread.Start();
            Console.ReadKey();

        }

        static void timer_Elapsed(object sender, TimerEventArg e)
        {
            var threadName = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine(@"is main therad " + threadName);
        }
    }

    public class Worker
    {
        private Timer _timer;
        private readonly AutoResetEvent _waiter = new AutoResetEvent(false);
        private int _number;
        public void Run() {
            
            _timer = new Timer();
            _number = 0;
            _timer.Elapsed += delegate {
                var threadName = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine(@"Number " + threadName + @" " +_number);
                ++_number;
                if (_number >= 10) {
                    Stop();
                }
            };
            _timer.Start(1000);
            _waiter.WaitOne();

        }

        public void Stop() {
            _waiter.Set();
            _timer.Stop();
        }

       
    }
}
