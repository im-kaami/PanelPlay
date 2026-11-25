namespace PanelPlay.Models;

internal static class BuiltInDrills
{
    private const string Prompt = "The microphone input meter shows activity, but the audience cannot hear the mic. "
        + "Restore the mic signal at Main without interrupting the music.";

    public static Drill MixerA() => MicrophoneFault("Silent mic · A", microphoneMuted: true, microphoneRouted: true);

    public static Drill MixerB() => MicrophoneFault("Silent mic · B", microphoneMuted: false, microphoneRouted: false);

    private static Drill MicrophoneFault(string title, bool microphoneMuted, bool microphoneRouted) =>
        new(Guid.Empty, title, Prompt, null,
        [
            new("Mic mute", .2, .5, microphoneMuted, false, false) { MixerRole = MixerControl.MicrophoneMute },
            new("Mic to Main", .5, .5, microphoneRouted, true, false) { MixerRole = MixerControl.MicrophoneRoute },
            new("Music to Main", .8, .5, true, true, true) { MixerRole = MixerControl.MusicRoute }
        ]);
}
