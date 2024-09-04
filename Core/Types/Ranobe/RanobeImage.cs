using System.Text.Json.Serialization;

namespace Core.Types.Ranobe; 

public class RanobeImage {
    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}