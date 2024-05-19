using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultitaskScheduler
{
    public class Scheduler : IDisposable
    {
        private static SchedulerFactory _schedulerFactory;

        private bool _disposedValue;

        private readonly static object _lock = new object();
        private readonly ConcurrentDictionary<int, List<Job>> _dictionary = new ConcurrentDictionary<int, List<Job>>();
        private readonly ConcurrentDictionary<int, List<JobWithParam>> _dictionaryWithParam = new ConcurrentDictionary<int, List<JobWithParam>>();

        /// <summary>
        /// Gets singleton instance of <see cref="SchedulerFactory"/>.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the whether that current <see cref="Scheduler"/> is in paused state or not.
        /// </summary>
        public bool IsPaused { get; set; }
        /// <summary>
        /// Gets the whether that current <see cref="Scheduler"/> is in running state or not.
        /// </summary>
        public bool IsRunning { get; private set; }

        internal Scheduler()
        {
            Task.Factory.StartNew(() => RunScheduler());
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="jobName">The name of the Job.</param>
        /// <param name="interval">Range in TimeSpan in which new scheduled job should trigger. Minimum value is 1 second.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger.</param>
        /// <param name="handleOnce">Defines the whether that current job should be run only once or infinitely.</param>
        public void ScheduleJob(string jobName, TimeSpan interval, Action action, bool handleOnce = false) => ScheduleJob(jobName, (int)interval.TotalSeconds, action, handleOnce);

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="jobName">The name of the Job.</param>
        /// <param name="interval">Range in TimeSpan in which new scheduled job should trigger. Minimum value is 1 second.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger with input parameter.</param>
        /// <param name="parameter">Input parameter for job action.</param>
        /// <param name="handleOnce">Defines the whether that current job should be run only once or infinitely.</param>
        public void ScheduleJob(string jobName, TimeSpan interval, Action<object> action, object parameter, bool handleOnce = false) => ScheduleJob(jobName, (int)interval.TotalSeconds, action, parameter, handleOnce);

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="jobName">The name of the Job.</param>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger.</param>
        /// <param name="handleOnce">Defines the whether that current job should be run only once or infinitely.</param>
        /// <exception cref="InvalidOperationException">Throws, when creating job name is already exist in the Scheduler, but was passed with some parameter.</exception>
        public void ScheduleJob(string jobName, int seconds, Action action, bool handleOnce = false)
        {
            var model = new Job
            {
                Name = jobName,
                Action = action,
                HandleOnce = handleOnce
            };

            if (_dictionaryWithParam.SelectMany(d => d.Value).Any(j => j.Name == jobName))
                throw new InvalidOperationException("There is already job in scheduler with such name, but with parameter.");

            if (!_dictionary.ContainsKey(seconds))
                _dictionary.TryAdd(seconds, new List<Job>());

            _dictionary[seconds].Add(model);
        }

        /// <summary>
        /// Schedules new job in the scheduler.
        /// </summary>
        /// <param name="jobName">The name of the Job.</param>
        /// <param name="seconds">Range in seconds in which new scheduled job should trigger.</param>
        /// <param name="action">Job <see cref="Action"/> to trigger with input parameter.</param>
        /// <param name="parameter">Input parameter for job action.</param>
        /// <param name="handleOnce">Defines the whether that current job should be run only once or infinitely.</param>
        /// <exception cref="InvalidOperationException">Throws, when creating job name is already exist in the Scheduler,but was passed without any parameters.</exception>
        public void ScheduleJob(string jobName, int seconds, Action<object> action, object parameter, bool handleOnce = false)
        {
            var model = new JobWithParam
            {
                Name = jobName,
                Action = action,
                Parameter = parameter,
                HandleOnce = handleOnce
            };

            if (_dictionary.SelectMany(d => d.Value).Any(j => j.Name == jobName))
                throw new InvalidOperationException("There is already job in scheduler with such name, but without any parameters.");

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

        /// <summary>
        /// Goes through all scheduled jobs and tries to find the one by specified name.
        /// Checks if found job interval was changed, if so, then reschedules the job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="interval">Range in TimeSpan in which new scheduled job should trigger. Minimum value is 1 second.</param>
        public bool TryChangeInterval(string jobName, TimeSpan interval) => TryChangeInterval(jobName, (int)interval.TotalSeconds);

        /// <summary>
        /// Goes through all scheduled jobs and tries to find the one by specified name.
        /// Checks if found job interval was changed, if so, then reschedules the job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="seconds">Range in seconds in which scheduled job should trigger.</param>
        public bool TryChangeInterval(string jobName, int seconds)
        {
            Job job = null;
            JobWithParam jobWithArg = null;

            foreach (var kvp in _dictionary)
            {
                job = kvp.Value.FirstOrDefault(j => j.Name == jobName);
                if (job != null && kvp.Key != seconds)
                {
                    RemoveJob(job, kvp);

                    ScheduleJob(jobName, seconds, job.Action, job.HandleOnce);
                    return true;
                }
            }

            foreach (var kvp in _dictionaryWithParam)
            {
                jobWithArg = kvp.Value.FirstOrDefault(j => j.Name == jobName);
                if (jobWithArg != null && kvp.Key != seconds)
                {
                    RemoveJobWithArg(jobWithArg, kvp);

                    ScheduleJob(jobName, seconds, jobWithArg.Action, jobWithArg.Parameter, jobWithArg.HandleOnce);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes job by specified name makes cleanup in scheduler.
        /// </summary>
        /// <param name="jobName">Specified name of the job.</param>
        /// <returns></returns>
        public bool TryRemoveJob(string jobName)
        {
            foreach (var kvp in _dictionary)
            {
                Job job = kvp.Value.FirstOrDefault(j => j.Name == jobName);
                if (job != null)
                {
                    RemoveJob(job, kvp);
                    return true;
                }
            }

            foreach (var kvp in _dictionaryWithParam)
            {
                JobWithParam jobWithArg = kvp.Value.FirstOrDefault(j => j.Name == jobName);
                if (jobWithArg != null)
                {
                    RemoveJobWithArg(jobWithArg, kvp);
                    return true;
                }
            }

            return false;
        }

        private void RunScheduler()
        {
            while (true)
            {
                if (!IsPaused)
                {
                    IsRunning = true;
                    try
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
                                            value.Action();

                                            if (value.HandleOnce)
                                                pair.Value.RemoveAt(i);
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
                                            value.Action(value.Parameter);

                                            if (value.HandleOnce)
                                                pair.Value.RemoveAt(i);
                                        });
                                    }
                                }

                                if (pair.Value.Count == 0)
                                    _dictionaryWithParam.TryRemove(pair.Key, out _);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DictionaryCount: {_dictionary.Count}; DictionaryWithParamCount: {_dictionaryWithParam.Count}. {ex}");
                    }
                }

                Thread.Sleep(1000);
                IsRunning = false;
            }
        }

        private void RemoveJobWithArg(JobWithParam jobWithParam, KeyValuePair<int, List<JobWithParam>> kvp)
        {
            kvp.Value.Remove(jobWithParam);
            if (kvp.Value.Count == 0
                && _dictionaryWithParam.TryRemove(kvp.Key, out _)
                && !_dictionaryWithParam.Values.SelectMany(jl => jl).Any())
            {
                _dictionaryWithParam.Clear();
            }
        }

        private void RemoveJob(Job job, KeyValuePair<int, List<Job>> kvp)
        {
            kvp.Value.Remove(job);
            if (kvp.Value.Count == 0
                && _dictionary.TryRemove(kvp.Key, out _)
                && !_dictionary.Values.SelectMany(jl => jl).Any())
            {
                _dictionary.Clear();
            }
        }

        private class Job
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }

        private class JobWithParam
        {
            public string Name { get; set; }
            public Action<object> Action { get; set; }
            public object Parameter { get; set; }
            public bool HandleOnce { get; set; }
            public int Counter { get; set; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                Reset();
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /// </summary>
        ~Scheduler()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
