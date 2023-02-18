namespace bunt
{
    // instances stores state (fields)
    internal class BuntInstance
    {
        private BuntClass klass;
        private readonly Dictionary<string, object> fields = new Dictionary<string, object>();

        public BuntInstance(BuntClass klass) 
        { 
            this.klass = klass;
        }

        // get the property (either field or method)
        public object get(Token name)
        {
            if (fields.ContainsKey(name.lexeme))
            {
                return fields[name.lexeme];
            }

            BuntFunction method = klass.findMethod(name.lexeme);
            if (method != null) return method.bind(this);

            throw new RuntimeError(name, "Undefined property '" + name.lexeme + "'.");
        }

        public void set(Token name, object value)
        {
            if (fields.ContainsKey(name.lexeme))
            {
                fields.Remove(name.lexeme);
            }

            fields.Add(name.lexeme, value);
        }

        public override string ToString()
        {
            return klass.name + " instance";
        }
    }
}
