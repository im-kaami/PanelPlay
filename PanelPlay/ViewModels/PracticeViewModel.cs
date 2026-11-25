using System.ComponentModel;
using PanelPlay.Models;

namespace PanelPlay.ViewModels;

internal sealed class PracticeViewModel : INotifyPropertyChanged
{
    private readonly PracticeSession session;

    public PracticeViewModel(Drill drill)
    {
        session = new PracticeSession(drill.Controls);
        Switches = drill.Controls.Select((control, index) =>
            new PracticeSwitch(this, control, index)).ToArray();
        RestartCommand = new Command(() =>
        {
            session.Reset();
            Refresh();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<PracticeSwitch> Switches { get; }
    public Command RestartCommand { get; }
    public bool IsFailed => session.ViolatedConstraint;
    public bool IsSolved => session.Solved;
    public bool IsActive => !IsFailed && !IsSolved;
    public string Status => IsFailed ? "A protected setting changed. Restart to try again."
        : IsSolved ? "Fault resolved." : "Fault active";
    public bool HasSignalReadings => session.MixerMeters.HasValue;
    public string MicrophoneInput => Signal(session.MixerMeters?.MicrophoneInputActive);
    public string MicrophoneAtMain => Signal(session.MixerMeters?.MicrophoneAtMain);
    public string MusicAtMain => Signal(session.MixerMeters?.MusicAtMain);

    public bool State(int index) => session.State(index);

    public void Toggle(int index)
    {
        if (!IsActive)
            return;

        session.Toggle(index);
        Refresh();
    }

    private void Refresh()
    {
        foreach (var control in Switches)
            control.Refresh();

        // A single switch can change the attempt, several readings, and command availability.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private static string Signal(bool? active) => active == true ? "Active" : "No signal";
}

internal sealed class PracticeSwitch : INotifyPropertyChanged
{
    private readonly PracticeViewModel practice;
    private readonly Control control;
    private readonly int index;

    public PracticeSwitch(PracticeViewModel practice, Control control, int index)
    {
        this.practice = practice;
        this.control = control;
        this.index = index;
        ToggleCommand = new Command(() => practice.Toggle(index), () => practice.IsActive);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsOn => practice.State(index);
    public string Text => $"{index + 1}. {control.Name} · {(IsOn ? "On" : "Off")}";
    public Command ToggleCommand { get; }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOn)));
        ToggleCommand.ChangeCanExecute();
    }
}
