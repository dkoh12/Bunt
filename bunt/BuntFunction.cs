namespace bunt
{
    internal class BuntFunction : IBuntCallable
    {
        private readonly Stmt.Function declaration;
        private readonly BuntEnvironment closure;
        private readonly bool isInitializer;
        public BuntFunction(Stmt.Function declaration, BuntEnvironment closure, bool isInitializer) 
        { 
            this.declaration = declaration;
            this.closure = closure;
            this.isInitializer = isInitializer;
        }

        public int arity => declaration.parameters.Count;

        /// <summary>
        /// Creates a new environment at each function call rather than function declaration. Otherwise recursion breaks.
        /// functions encapsulates its parameters
        /// </summary>
        /// <returns></returns>
        public object call(Interpreter interpreter, List<object> arguments)
        {
            // when we call the function, we use that environment as the call's parent.
            BuntEnvironment environment = new BuntEnvironment(closure);

            for (int i = 0; i < declaration.parameters.Count; i++)
            {
                environment.define(declaration.parameters[i].lexeme, arguments[i]);
            }

            try
            {
                interpreter.executeBlock(declaration.body, environment);
            }
            catch (Return returnValue)
            {
                /* we don't allow returning a value in init() but do allow return;
                 * 
                 * class Foo {
                 *   init() {
                 *     return;
                 *   }
                 * }
                 * 
                 * in this case it returns 'this' instead of nil.
                 */
                if (isInitializer) return closure.getAt(0, "this");

                return returnValue.value;
            }

            if (isInitializer) return closure.getAt(0, "this");

            /*
             * Bunt is dynamically typed. There are no true void functions. Every function has to return something even 
             * if it contains no return statements at all. In such case we return "nil". That's why we return null at the end.
             */
            return null;
        }

        /// <summary>
        /// we create a new scope for 'this' if it appears inside a method. sort of a closure within a closure.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public BuntFunction bind(BuntInstance instance)
        {
            BuntEnvironment environment = new BuntEnvironment(closure);
            environment.define("this", instance);
            return new BuntFunction(declaration, environment, isInitializer);
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
