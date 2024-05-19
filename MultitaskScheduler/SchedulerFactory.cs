using System;
using System.Collections.Concurrent;

namespace MultitaskScheduler
{
    public class SchedulerFactory
    {
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<string, Scheduler> _instances = new ConcurrentDictionary<string, Scheduler>();

        public Scheduler CreateNew(string schedulerName)
        {
            if (schedulerName == null)
                throw new ArgumentNullException(nameof(schedulerName));

            lock (_lock)
            {
                if (_instances.ContainsKey(schedulerName))
                    throw new ArgumentException("Scheduler with the same name already exists", nameof(schedulerName));

                var scheduler = new Scheduler();
                _instances.TryAdd(schedulerName, scheduler);
                return scheduler;
            }
        }
    }
}
