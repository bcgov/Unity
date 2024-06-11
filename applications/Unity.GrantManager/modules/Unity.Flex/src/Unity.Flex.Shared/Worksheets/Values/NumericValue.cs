namespace Unity.Flex.Worksheets
{
    public class NumericValue
    {
        public NumericValue()
        {
            // Intentionally left blank
        }

        public NumericValue(string value)
        {
            Value = value;
        }

        public object? Value { get; set; }
    }
}
