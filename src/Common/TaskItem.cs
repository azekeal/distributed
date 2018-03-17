using System.Collections.Generic;

namespace Common
{
    public struct TaskItem
    {
        public readonly string Identifier;
        public readonly object Data;

        public TaskItem(string identifier, object data)
        {
            Identifier = identifier;
            Data = data;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TaskItem))
            {
                return false;
            }

            var item = (TaskItem)obj;
            return Identifier == item.Identifier;
        }

        public override int GetHashCode()
        {
            var hashCode = 41865310;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Identifier);
            return hashCode;
        }
    }
}
