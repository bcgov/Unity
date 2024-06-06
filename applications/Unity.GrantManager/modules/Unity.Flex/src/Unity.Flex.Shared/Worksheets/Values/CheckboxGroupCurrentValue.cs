namespace Unity.Flex.Worksheets.Values
{
    public class CheckboxGroupCurrentValue
    {
        public CheckboxGroupValueOption[] Value { get; set; } = [];
    }

    public class CheckboxGroupValueOption
    {
        public string Key { get; set; } = string.Empty;
        public bool Value { get; set; }
    }
}
