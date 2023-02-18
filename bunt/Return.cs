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
}
