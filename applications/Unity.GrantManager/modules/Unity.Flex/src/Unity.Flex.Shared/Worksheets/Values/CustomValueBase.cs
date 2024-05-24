namespace Unity.Flex.Worksheets.Values
{
    public abstract class CustomValueBase
    {
        protected CustomValueBase()
        {
        }

        protected CustomValueBase(object value)
        {
            Value = value;
        }

        public object? Value { get; set; }
    }
}
