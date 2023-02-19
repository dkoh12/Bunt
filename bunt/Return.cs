namespace bunt
{
    /// <summary>
    /// We throw an exception upon 'return' to throw away the function call stack and exit.
    /// </summary>
    public class Return : Exception
    {
        public object value;

        public Return(object value) : base()        {
            this.value = value;
        }

    }

    /// <summary>
    /// We throw an exception upon 'break' to throw away the function call stack and exit.
    /// </summary>
    public class Break : Exception
    {

    }

    /// <summary>
    /// We throw an exception upon 'continue' to throw away the current block and exit.
    /// </summary>
    public class Continue : Exception
    {

    }
}
