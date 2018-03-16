using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public struct TaskItem
    {
        public string Name;
        public int Priority;
    }

    public struct TaskPriorityGroup
    {
        public int Priority;
        public int TaskCount;
    }

    public struct DispatcherWork
    {
        public List<TaskPriorityGroup> AllTasks;
    }
}
