using System.Collections.ObjectModel;
using System.ComponentModel;
using PanelPlay.Models;
using PanelPlay.Services;

namespace PanelPlay.ViewModels;

internal sealed class DrillEditor : INotifyPropertyChanged
{
    public const int ControlLimit = 6;

    private readonly Drill original;
    private string title;
    private string prompt;
    private string? photo;

    public DrillEditor(Drill drill)
    {
        original = drill;
        title = drill.Title;
        prompt = drill.Prompt;
        photo = drill.Photo;
        Controls = new ObservableCollection<Control>(drill.Controls);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => title;
        set => SetText(ref title, value ?? "", nameof(Title));
    }

    public string Prompt
    {
        get => prompt;
        set => SetText(ref prompt, value ?? "", nameof(Prompt));
    }

    public string? Photo => photo;
    public ObservableCollection<Control> Controls { get; }
    public bool HasChanges => title != original.Title || prompt != original.Prompt ||
        photo != original.Photo || !Controls.SequenceEqual(original.Controls);

    public async Task<Drill> SaveAsync()
    {
        var drill = new Drill(original.Id, Title.Trim(), Prompt.Trim(), Photo, [.. Controls]);
        await DrillLibrary.SaveAsync(drill);
        return drill;
    }

    public void MoveControl(int index, double x, double y) =>
        Controls[index] = Controls[index] with { X = x, Y = y };

    public async Task CaptureAsync()
    {
        var captured = await DrillFiles.CaptureAsync();
        if (captured is null || captured == photo)
            return;

        photo = captured;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photo)));
    }

    private void SetText(ref string field, string value, string property)
    {
        if (field == value)
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
