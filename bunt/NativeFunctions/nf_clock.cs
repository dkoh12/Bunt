namespace bunt.NativeFunctions
{
    internal class nf_clock : IBuntCallable
    {
        public int arity => 0;

        public object call(Interpreter interpreter, List<object> arguments)
        {
            return DateTime.UtcNow;
        }

        public string toString() { return "<native fn>"; }
    }
}
