namespace bunt
{
    // classes store behavior (methods)
    internal class BuntClass : IBuntCallable
    {
        public readonly string name;
        readonly BuntClass superclass;
        private readonly Dictionary<string, BuntFunction> methods;

        public BuntClass(string name, BuntClass superclass, Dictionary<string, BuntFunction> methods)
        {
            this.name = name;
            this.superclass = superclass;
            this.methods = methods;
        }

        // validate we passed the right number of arguments to a callable
        public int arity 
        {
            get {
                BuntFunction initializer = findMethod("init");
                if (initializer == null) return 0;
                return initializer.arity;
            }
        }

        public object call(Interpreter interpreter, List<object> arguments)
        {
            BuntInstance instance = new BuntInstance(this);
            BuntFunction initializer = findMethod("init");

            if (initializer != null)
            {
                initializer.bind(instance).call(interpreter, arguments);
            }

            return instance;
        }

        // methods are still accessed via instances
        public BuntFunction findMethod(string name)
        {
            if (methods.ContainsKey(name))
            {
                return methods[name];
            }

            if (superclass != null)
            {
                return superclass.findMethod(name);
            }

            return null;
        }

        public override string ToString()
        {
            return name + " class";
        }
    }
}
