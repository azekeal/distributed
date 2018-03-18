using System;

namespace Common
{
    public interface ITaskProvider
    {
        /// <summary>
        /// The configuration object to pass to the TaskExecutor Initialization function
        /// Note that this object must be able to be serialized/deserialized.
        /// </summary>
        object Config { get; }

        int TaskCount { get; }

        /// <summary>
        /// Returns true if a task was returned from the TaskProvider
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        bool TryGetTask(out TaskItem task);

        /// <summary>
        /// Returns true if tasks are available to get
        /// </summary>
        bool CompleteTask(TaskItem task, TaskResult result);

        /// <summary>
        /// Should fire if tasks are added dynamically to the task provider after the job has begun
        /// </summary>
        event Action TasksAdded;
    }
}
