using System;
using System.Collections.Concurrent;

namespace MultitaskScheduler
{
    public class SchedulerFactory
    {
        private readonly ConcurrentDictionary<string, Scheduler> _schedulerInstances = new ConcurrentDictionary<string, Scheduler>();

        /// <summary>
        /// Creates new instance of <see cref="Scheduler"/> and returns.
        /// </summary>
        /// <param name="schedulerName">The name for <see cref="Scheduler"/> instance.</param>
        /// <exception cref="ArgumentNullException">Throws when proposed name of scheduler is null or empty string.</exception>
        /// <exception cref="ArgumentException">Throws when Factory has already contain proposed name of scheduler.</exception>
        public Scheduler CreateNew(string schedulerName)
        {
            if (string.IsNullOrEmpty(schedulerName))
                throw new ArgumentNullException(nameof(schedulerName));

            if (_schedulerInstances.ContainsKey(schedulerName))
                throw new ArgumentException($"Scheduler Factory already contains Scheduler with the name: '{schedulerName}'", nameof(schedulerName));

            var scheduler = new Scheduler();
            _schedulerInstances.TryAdd(schedulerName, scheduler);
            return scheduler;
        }

        /// <summary>
        /// Returns <see cref="Scheduler"/> from the Factory.
        /// </summary>
        /// <param name="schedulerName">The name for <see cref="Scheduler"/> instance.</param>
        /// <exception cref="ArgumentNullException">Throws when scheduler name is null or empty string.</exception>
        /// <exception cref="ArgumentException">Throws when Factory does not contain scheduler name.</exception>
        public Scheduler Get(string schedulerName)
        {
            if (string.IsNullOrEmpty(schedulerName))
                throw new ArgumentNullException(nameof(schedulerName));

            if (!_schedulerInstances.ContainsKey(schedulerName))
                throw new ArgumentException($"Scheduler Factory does not contain Scheduler with the name: '{schedulerName}'", nameof(schedulerName));

            return _schedulerInstances[schedulerName];
        }

        /// <summary>
        /// Cancels running scheduler by it's name.
        /// </summary>
        /// <param name="schedulerName">The name for <see cref="Scheduler"/> instance.</param>
        /// <exception cref="ArgumentNullException">Throws when scheduler name is null or empty string.</exception>
        public bool Pause(string schedulerName)
        {
            if (string.IsNullOrEmpty(schedulerName))
                throw new ArgumentNullException(nameof(schedulerName));

            if (!_schedulerInstances.ContainsKey(schedulerName) || _schedulerInstances[schedulerName].IsPaused)
                return false;

            _schedulerInstances[schedulerName].IsPaused = true;
            return true;
        }

        /// <summary>
        /// Cancels all running schedulers.
        /// </summary>
        public bool PauseAll()
        {
            if (_schedulerInstances.Count == 0)
                return false;

            foreach (var scheduler in _schedulerInstances.Values)
                scheduler.IsPaused = true;

            return true;
        }

        /// <summary>
        /// Removes and disposes <see cref="Scheduler"/> from Factory.
        /// </summary>
        /// <param name="schedulerName">The name for <see cref="Scheduler"/> instance.</param>
        /// <exception cref="ArgumentNullException">Throws when scheduler name is null or empty string.</exception>
        public bool Remove(string schedulerName)
        {
            if (string.IsNullOrEmpty(schedulerName))
                throw new ArgumentNullException(nameof(schedulerName));

            if (!_schedulerInstances.ContainsKey(schedulerName))
                return false;

            _schedulerInstances[schedulerName].Dispose();
            _schedulerInstances.TryRemove(schedulerName, out _);
            return true;
        }

        /// <summary>
        /// Removes and disposes all <see cref="Scheduler"/> from Factory.
        /// </summary>
        public bool RemoveAll()
        {
            if (_schedulerInstances.Count == 0)
                return false;

            foreach (var scheduler in _schedulerInstances.Values)
                scheduler.Dispose();

            _schedulerInstances.Clear();
            return true;
        }

        /// <summary>
        /// Checks the whether that specified scheduler is in running state or not.
        /// </summary>
        /// <param name="schedulerName">The name for <see cref="Scheduler"/> instance.</param>
        /// <exception cref="ArgumentNullException">Throws when scheduler name is null or empty string.</exception>
        public bool CheckIfRunning(string schedulerName)
        {
            if (string.IsNullOrEmpty(schedulerName))
                throw new ArgumentNullException(nameof(schedulerName));

            return _schedulerInstances.ContainsKey(schedulerName) && !_schedulerInstances[schedulerName].IsRunning;
        }
    }
}
