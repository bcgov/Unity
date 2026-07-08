namespace Unity.AI.Domain;

public class AIModelSettings
{
    public bool MaxOutputTokenCountSupported { get; set; } = true;

    public double? Temperature { get; set; }
}
