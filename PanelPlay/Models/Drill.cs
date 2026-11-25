using System.Text.Json.Serialization;

namespace PanelPlay.Models;

public sealed record Control(string Name, double X, double Y, bool Initial, bool Target, bool Protected)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MixerControl MixerRole { get; init; }
}

public enum MixerControl
{
    None,
    MicrophoneMute,
    MicrophoneRoute,
    MusicRoute
}

public sealed record Drill(Guid Id, string Title, string Prompt, string? Photo, List<Control> Controls)
{
    public static Drill New() => new(Guid.NewGuid(), "", "", null, []);
}
