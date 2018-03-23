using Distributed.Core;
using System.Threading.Tasks;

namespace Distributed
{
    public abstract class TaskExecutor
    {
        public delegate void CompletedTaskHandler(TaskItem task, TaskResult result);

        /// <summary>
        /// This event is fired whenever a task is completed
        /// </summary>
        public event CompletedTaskHandler CompletedTask;

        /// <summary>
        /// Called when starting a session with a new dispatcher. TaskItems will not be started until
        /// this method completes.
        /// </summary>
        /// <param name="config">The opaque config object passed by the dispatcher</param>
        public abstract Task<InitializationResult> Initialize(object config);
        
        /// <summary>
        /// Called to notify the agent to start tasks. Should return array cooresponding to input array
        /// that has the result of STARTING the task. Note that this should not wait for the task to complete before
        /// returning. This is used to early out if a task fails to start for some reason.
        /// </summary>
        /// <param name="config">The opaque config object passed by the dispatcher</param>
        /// <returns>Returns array of the result of STARTING the tasks.</returns>
        public abstract Task<TaskResult[]> StartTasks(TaskItem[] tasks);

        /// <summary>
        /// Should called internally by the TaskExecutor when a task has completed successfully or failed
        /// </summary>
        /// <param name="task">The task item passed in by StartTasks</param>
        /// <param name="result">The task result data</param>
        protected void CompleteTask(TaskItem task, TaskResult result)
        {
            CompletedTask?.Invoke(task, result);
        }
    }
}
