namespace bunt
{
    public interface IBuntCallable
    {
        public int arity { get; }
        public object call(Interpreter interpreter, List<object> arguments);
    }
}
