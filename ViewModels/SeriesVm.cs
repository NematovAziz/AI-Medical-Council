using System.Text.Json.Serialization;

namespace AI.MedicalCouncil.ViewModels;

/// <summary>One line on a client-rendered trend chart.</summary>
public class SeriesVm(string label, string color, double[] points)
{
    [JsonPropertyName("label")] public string Label { get; } = label;
    [JsonPropertyName("color")] public string Color { get; } = color;
    [JsonPropertyName("points")] public double[] Points { get; } = points;
}
