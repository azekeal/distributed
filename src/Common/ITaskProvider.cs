namespace Common
{
    public interface ITaskProvider
    {
        int TaskCount { get; }

        bool TryGetTask(out TaskItem task);

        /// <summary>
        /// Returns true if tasks are available to get
        /// </summary>
        bool CompleteTask(TaskItem task, TaskResult result);
    }
}
