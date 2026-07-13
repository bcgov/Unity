using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.AI.Responses;

public class FormWorksheetResponse
{
    public string Worksheet { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public uint Version { get; set; } = 1;

    [JsonPropertyName("published")]
    public bool Published { get; set; }

    [JsonPropertyName("reportColumns")]
    public string ReportColumns { get; set; } = string.Empty;

    [JsonPropertyName("reportKeys")]
    public string ReportKeys { get; set; } = string.Empty;

    [JsonPropertyName("reportViewName")]
    public string ReportViewName { get; set; } = string.Empty;

    [JsonPropertyName("sections")]
    public List<FormWorksheetSectionResponse> Sections { get; set; } = [];
}

public class FormWorksheetSectionResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public uint Order { get; set; }

    [JsonPropertyName("fields")]
    public List<FormWorksheetFieldResponse> Fields { get; set; } = [];
}

public class FormWorksheetFieldResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("order")]
    public uint Order { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("definition")]
    public string? Definition { get; set; }
}
