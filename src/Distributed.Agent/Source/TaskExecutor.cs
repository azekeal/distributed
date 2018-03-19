﻿using Distributed.Core;
using System.Threading.Tasks;

namespace Distributed
{
    public abstract class TaskExecutor
    {
        public Agent Agent { get; internal set; }

        protected void CompleteTask(TaskItem task, TaskResult result)
        {
            Agent.CompleteTask(task, result);
        }

        public abstract Task<InitializationResult> Initialize(object config);
        public abstract Task<TaskResult[]> StartTasks(TaskItem[] tasks);
    }
}