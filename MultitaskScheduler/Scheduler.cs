using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultitaskScheduler
{
    public class Scheduler
    {
        private static SchedulerFactory _schedulerFactory;

        private readonly static object _lock = new object();
        private readonly ConcurrentDictionary<int, List<Job>> _dictionary = new ConcurrentDictionary<int, List<Job>>();
        private readonly ConcurrentDictionary<int, List<JobWithParam>> _dictionaryWithParam = new ConcurrentDictionary<int, List<JobWithParam>>();

        public static SchedulerFactory Factory
        {
            get
            {
                if (_schedulerFactory == null)
                {
                    lock (_lock)
                    {
                        if (_schedulerFactory == null)
                            _schedulerFactory = new SchedulerFactory();
                    }
                }

                return _schedulerFactory;
            }
        }

        public string Name { get; }
        public bool IsPaused { get; set; }

        internal Scheduler()
        {
            Task.Factory.StartNew(() => RunScheduler());
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger.</param>
        /// <param name="handleOnce">Defines the whether that scheduled job should be triggered only once or infinitely.</param>
        public void ScheduleJob(int seconds, Action action, bool handleOnce = false)
        {
            var model = new Job
            {
                Action = action,
                HandleOnce = handleOnce
            };

            if (!_dictionary.ContainsKey(seconds))
                _dictionary.TryAdd(seconds, new List<Job>());

            _dictionary[seconds].Add(model);
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger with input parameter.</param>
        /// <param name="parameter">Input parameter for job action.</param>
        /// <param name="handleOnce">Defines the whether that scheduled job should be triggered only once or infinitely.</param>
        public void ScheduleJob(int seconds, Action<object> action, object parameter, bool handleOnce = false)
        {
            var model = new JobWithParam
            {
                Action = action,
                Parameter = parameter,
                HandleOnce = handleOnce
            };

            if (!_dictionaryWithParam.ContainsKey(seconds))
                _dictionaryWithParam.TryAdd(seconds, new List<JobWithParam>());

            _dictionaryWithParam[seconds].Add(model);
        }

        /// <summary>
        /// Resets scheduler and cleans up all internal storages.
        /// </summary>
        public void Reset()
        {
            _dictionary.Clear();
            _dictionaryWithParam.Clear();
        }

        private void RunScheduler()
        {
            while (true)
            {
                if (!IsPaused)
                {
                    if (!_dictionary.IsEmpty)
                    {
                        foreach (var pair in _dictionary)
                        {
                            for (int i = pair.Value.Count - 1; i >= 0; i--)
                            {
                                Job value = pair.Value[i];
                                value.Counter++;

                                if (value.Counter % pair.Key == 0)
                                {
                                    Task.Run(() =>
                                    {
                                        try
                                        {
                                            value.Action();
                                            if (value.HandleOnce)
                                                pair.Value.RemoveAt(i);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.ToString());
                                        }
                                    });
                                }
                            }

                            if (pair.Value.Count == 0)
                                _dictionary.TryRemove(pair.Key, out _);
                        }
                    }

                    if (!_dictionaryWithParam.IsEmpty)
                    {
                        foreach (var pair in _dictionaryWithParam)
                        {
                            for (int i = pair.Value.Count - 1; i >= 0; i--)
                            {
                                JobWithParam value = pair.Value[i];
                                value.Counter++;

                                if (value.Counter % pair.Key == 0)
                                {
                                    Task.Run(() =>
                                    {
                                        try
                                        {
                                            value.Action(value.Parameter);
                                            if (value.HandleOnce)
                                                pair.Value.RemoveAt(i);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.ToString());
                                        }
                                    });
                                }
                            }

                            if (pair.Value.Count == 0)
                                _dictionaryWithParam.TryRemove(pair.Key, out _);
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private class Job
        {
            public Action Action { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }

        private class JobWithParam
        {
            public Action<object> Action { get; set; }
            public object Parameter { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }
    }
}
