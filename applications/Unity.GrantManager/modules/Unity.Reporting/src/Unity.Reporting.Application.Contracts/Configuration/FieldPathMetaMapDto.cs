namespace Unity.Reporting.Configuration
{
    public class FieldPathMetaMapDto
    {
        public FieldPathTypeDto[] Fields { get; set; } = [];
        public MapMetadataDto? Metadata { get; set; } = null;
    }
}