using System;

namespace MultitaskScheduler.ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GlobalScheduler.Instance.ScheduleJob(4, () => Console.WriteLine(DateTime.Now.ToString("HH:mm:ss")));
            Console.ReadKey();
        }
    }
}
