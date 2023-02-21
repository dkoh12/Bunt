using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bunt
{
    // kind of like Python's implementation of array / list
    internal class BuntList
    {
        readonly List<object> variables;

        public BuntList()
        {
            variables = new List<object>();
        }

        // used in interpreter.stringify
        public BuntList(List<object> variables) 
        { 
            this.variables = variables;
        }
        
        public int Length => variables.Count;

        public void Add (object variable)
        {
            variables.Add (variable);
        }

        public void Pop()
        {
            variables.RemoveAt(variables.Count - 1);
        }

        public object Get(int index)
        {
            if (index > variables.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return variables[index];
        }

        public void AddAt(int index, object variable)
        {
            if (index > variables.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            variables.Insert(index, variable);
        }

        public void RemoveAt(int index)
        {
            if (index > variables.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            variables.RemoveAt(index);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            
            foreach (object variable in variables)
            {
                builder.Append(variable.ToString());
                builder.Append(',');
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append(']');

            return builder.ToString();
        }

    }
}
