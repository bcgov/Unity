namespace Unity.Flex.Worksheets.Definitions
{
    public class DefinitionBase
    {
        public virtual long? Min { get; set; } = 0;
        public virtual long? Max { get; set; } = default;
        public virtual uint? MinLength { get; set; } = default;
        public virtual uint? MaxLength { get; set;} = default;
    }
}
