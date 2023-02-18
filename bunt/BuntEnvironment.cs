using System.Collections.Generic;

namespace bunt
{
    // Hack for hackathon :)
    // This is a problem if it's in production
    public class BuntEnvironment
    {
        public readonly BuntEnvironment enclosing;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        // global scope
        public BuntEnvironment() {
            enclosing = null;
        }

        // local scope
        public BuntEnvironment(BuntEnvironment enclosing)
        {
            this.enclosing = enclosing;
        }

        /// <summary>
        /// We throw a runtime error rather than a syntax / compile time error if a variable is found but not value is bound to it.
        /// 
        /// The problem is that using a variable is not the same as referring to it. 
        /// We can refer to a variable without immediately evaluating it if that code is wrapped in a function. 
        /// If we make it a static error to mention a variable before it's declared, it becomes harder to define recursive functions
        /// 
        /// For example.
        /// 
        /// fun isOdd(n) {
        ///   if (n == 0) return false;
        ///   return isEven(n-1); // isEven is mentioned before it's declared.
        /// }
        /// 
        /// fun isEven(n) {
        ///   if (n == 0) return true;
        ///   return isOdd(n-1);
        /// }
        /// 
        /// 
        /// This means, we get a runtime error for the following rather than a compile time error
        /// 
        /// print a;
        /// var a = "too late";
        /// 
        /// </summary>
        /// <exception cref="RuntimeError"></exception>
        public object get(Token name)
        {
            if (values.ContainsKey(name.lexeme))
            {
                return values[name.lexeme];
            }

            if (enclosing != null) return enclosing.get(name);

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        /// <summary>
        /// When we add the key, we don't check to see if it's already present.
        /// 
        /// var a = "before";
        /// print a; // "before"
        /// var a = "after";
        /// print a; // "after"
        /// </summary>
        public void define(string name, object value)
        {
            if (!values.TryAdd(name, value))
            {
                values.Remove(name);
                values.Add(name, value);
            }
        }

        /// <summary>
        /// Difference between assign() and define() is that assignment is not allowed to create a new variable.
        /// Hence we throw a runtime error if the key doesn't already exist.
        /// </summary>
        public void assign(Token name, object value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values.Remove(name.lexeme);
                values.Add(name.lexeme, value);
                return;
            }

            if (enclosing != null)
            {
                enclosing.assign(name, value);
                return;
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        // go to the specific scope where the local variable is defined.
        // we know the variable is there because the resolver already found it before.
        public object getAt(int distance, string name)
        {
            return ancestor(distance).values[name];
        }

        BuntEnvironment ancestor(int distance)
        {
            BuntEnvironment environment = this;
            for (int i = 0; i < distance; i++)
            {
                environment = environment.enclosing;
            }

            return environment;
        }

        public void assignAt(int distance, Token name, object value)
        {
            if (ancestor(distance).values.ContainsKey(name.lexeme))
            {
                ancestor(distance).values.Remove(name.lexeme);
            }

            ancestor(distance).values.Add(name.lexeme, value);
        }

    }
}
