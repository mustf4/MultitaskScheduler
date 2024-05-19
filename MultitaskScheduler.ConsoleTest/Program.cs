using System;
using System.Threading;

namespace MultitaskScheduler.ConsoleTest
{
    internal class Program
    {
        private static Scheduler _scheduler = Scheduler.Factory.CreateNew("Default Scheduler");

        static void Main(string[] args)
        {
            _scheduler.ScheduleJob("Job 1", 4, () => Console.WriteLine(DateTime.Now.ToString("HH:mm:ss")));
            Thread.Sleep(9000);
            _scheduler.IsPaused = true;
            Thread.Sleep(9000);
            _scheduler.IsPaused = false;
            Console.ReadKey();
        }
    }
}
