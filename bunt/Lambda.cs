namespace bunt
{
    internal class Lambda : IBuntCallable
    {
        private readonly Expr.Lambda declaration;
        private readonly BuntEnvironment closure;

        public Lambda(Expr.Lambda declaration, BuntEnvironment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        public int arity => declaration.parameters.Count;

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
                return returnValue.value;
            }

            /*
             * Bunt is dynamically typed. There are no true void functions. Every function has to return something even 
             * if it contains no return statements at all. In such case we return "nil". That's why we return null at the end.
             */
            return null;
        }
    }
}
