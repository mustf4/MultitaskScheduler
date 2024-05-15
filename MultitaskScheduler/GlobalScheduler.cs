using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultitaskScheduler
{
    public class GlobalScheduler
    {
        private static GlobalScheduler _instance;

        private readonly static object _lock = new object();
        private readonly static ConcurrentDictionary<int, List<ScheduleModel>> _dictionary = new ConcurrentDictionary<int, List<ScheduleModel>>();
        private readonly static ConcurrentDictionary<int, List<ScheduleModelWithArg>> _dictionaryWithParam = new ConcurrentDictionary<int, List<ScheduleModelWithArg>>();

        public static GlobalScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new GlobalScheduler();
                    }
                }

                return _instance;
            }
        }

        public bool IsCanceled { get; set; }

        private GlobalScheduler()
        {
            Task.Factory.StartNew(() => RunScheduler());
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger.</param>
        public void ScheduleJob(int seconds, Action action, bool handleOnce = false)
        {
            var model = new ScheduleModel
            {
                Action = action,
                HandleOnce = handleOnce
            };

            if (!_dictionary.ContainsKey(seconds))
                _dictionary.TryAdd(seconds, new List<ScheduleModel>());

            _dictionary[seconds].Add(model);
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger with input parameter.</param>
        /// <param name="arg">Input parameter for job action.</param>
        public void ScheduleJob(int seconds, Action<object> action, object arg, bool handleOnce = false)
        {
            var model = new ScheduleModelWithArg
            {
                Action = action,
                Argument = arg,
                HandleOnce = handleOnce
            };

            if (!_dictionaryWithParam.ContainsKey(seconds))
                _dictionaryWithParam.TryAdd(seconds, new List<ScheduleModelWithArg>());

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
                if (!IsCanceled)
                {
                    if (!_dictionary.IsEmpty)
                    {
                        foreach (var pair in _dictionary)
                        {
                            for (int i = pair.Value.Count - 1; i >= 0; i--)
                            {
                                ScheduleModel value = pair.Value[i];
                                value.Counter++;

                                if (value.Counter % pair.Key == 0)
                                {
                                    value.Action();
                                    if (value.HandleOnce)
                                        pair.Value.RemoveAt(i);
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
                                ScheduleModelWithArg value = pair.Value[i];
                                value.Counter++;

                                if (value.Counter % pair.Key == 0)
                                {
                                    value.Action(value.Argument);
                                    if (value.HandleOnce)
                                        pair.Value.RemoveAt(i);
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

        private class ScheduleModel
        {
            public Action Action { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }

        private class ScheduleModelWithArg
        {
            public Action<object> Action { get; set; }
            public object Argument { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }
    }
}
