using System.Text.Json.Serialization;

namespace Core.Types.Dreame; 

public class DreameChapter {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
}