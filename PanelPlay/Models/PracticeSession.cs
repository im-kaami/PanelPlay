namespace PanelPlay.Models;

public sealed class PracticeSession(IReadOnlyList<Control> controls)
{
    private readonly bool[] states = controls.Select(control => control.Initial).ToArray();
    private readonly int microphoneMute = FindRole(controls, MixerControl.MicrophoneMute);
    private readonly int microphoneRoute = FindRole(controls, MixerControl.MicrophoneRoute);
    private readonly int musicRoute = FindRole(controls, MixerControl.MusicRoute);

    public bool ViolatedConstraint { get; private set; }

    public bool Solved => !ViolatedConstraint && controls.Count > 0 &&
        controls.Select((control, index) => control.Target == states[index]).All(match => match);

    public MixerMeters? MixerMeters =>
        microphoneMute >= 0 && microphoneRoute >= 0 && musicRoute >= 0
            ? new MixerMeters(true, !states[microphoneMute] && states[microphoneRoute], states[musicRoute])
            : null;

    public bool State(int index) => states[index];

    public void Toggle(int index)
    {
        if (Solved || ViolatedConstraint)
            return;

        states[index] = !states[index];
        if (controls[index].Protected && states[index] != controls[index].Initial)
            ViolatedConstraint = true;
    }

    public void Reset()
    {
        for (var index = 0; index < states.Length; index++)
            states[index] = controls[index].Initial;

        ViolatedConstraint = false;
    }

    private static int FindRole(IReadOnlyList<Control> controls, MixerControl role)
    {
        var match = -1;
        for (var index = 0; index < controls.Count; index++)
        {
            if (controls[index].MixerRole != role)
                continue;
            if (match >= 0)
                return -1;
            match = index;
        }
        return match;
    }
}

public readonly record struct MixerMeters(bool MicrophoneInputActive, bool MicrophoneAtMain, bool MusicAtMain);
