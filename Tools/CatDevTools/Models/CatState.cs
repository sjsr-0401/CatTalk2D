using System.Text.Json.Serialization;

namespace CatDevTools.Models;

/// <summary>
/// Unity에서 받은 상태 데이터
/// </summary>
public class CatStateData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("gameDate")]
    public string GameDate { get; set; } = "";

    [JsonPropertyName("catAgeDays")]
    public int CatAgeDays { get; set; }

    [JsonPropertyName("currentHour")]
    public int CurrentHour { get; set; }

    [JsonPropertyName("currentMinute")]
    public int CurrentMinute { get; set; }

    [JsonPropertyName("hunger")]
    public float Hunger { get; set; }

    [JsonPropertyName("energy")]
    public float Energy { get; set; }

    [JsonPropertyName("stress")]
    public float Stress { get; set; }

    [JsonPropertyName("fun")]
    public float Fun { get; set; }

    [JsonPropertyName("affection")]
    public float Affection { get; set; }

    [JsonPropertyName("trust")]
    public float Trust { get; set; }

    [JsonPropertyName("experience")]
    public int Experience { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("playful")]
    public float Playful { get; set; }

    [JsonPropertyName("shy")]
    public float Shy { get; set; }

    [JsonPropertyName("aggressive")]
    public float Aggressive { get; set; }

    [JsonPropertyName("curious")]
    public float Curious { get; set; }

    [JsonPropertyName("mood")]
    public string Mood { get; set; } = "";
}

/// <summary>
/// 상태 변경 요청
/// </summary>
public class StateChangeRequest
{
    [JsonPropertyName("hunger")]
    public float Hunger { get; set; } = -1;

    [JsonPropertyName("energy")]
    public float Energy { get; set; } = -1;

    [JsonPropertyName("stress")]
    public float Stress { get; set; } = -1;

    [JsonPropertyName("fun")]
    public float Fun { get; set; } = -1;

    [JsonPropertyName("affection")]
    public float Affection { get; set; } = -1;

    [JsonPropertyName("trust")]
    public float Trust { get; set; } = -1;

    [JsonPropertyName("experience")]
    public int Experience { get; set; } = -1;
}
