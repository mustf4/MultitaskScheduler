using System;
using System.Threading;

namespace MultitaskScheduler.ConsoleTest
{
    internal class Program
    {
        private static readonly Scheduler _scheduler = Scheduler.Factory.CreateNew("Default Scheduler");

        static void Main(string[] args)
        {
            Console.WriteLine("Schedule Job 1 to run every 4 seconds: " + DateTime.Now.ToString("HH:mm:ss"));
            _scheduler.ScheduleJob("Job 1", 4, () => Console.WriteLine("Job 1: " + DateTime.Now.ToString("HH:mm:ss")));
            Thread.Sleep(9000);
            Console.WriteLine("Pause Job 1: " + DateTime.Now.ToString("HH:mm:ss"));
            _scheduler.IsPaused = true;
            Thread.Sleep(9000);
            Console.WriteLine("Continue Job 1: " + DateTime.Now.ToString("HH:mm:ss"));
            _scheduler.IsPaused = false;
            Console.ReadKey();
        }
    }
}
