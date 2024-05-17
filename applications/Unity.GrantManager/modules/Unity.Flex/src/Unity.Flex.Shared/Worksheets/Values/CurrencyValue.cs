namespace Unity.Flex.Worksheets
{
    public class CurrencyValue
    {
        public CurrencyValue()
        {
            // Intentionally left blank
        }

        public CurrencyValue(string value)
        {
            Value = value;
        }

        public object? Value { get; set; }
    }
}
