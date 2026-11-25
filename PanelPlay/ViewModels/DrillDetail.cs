using PanelPlay.Models;
using PanelPlay.Services;

namespace PanelPlay.ViewModels;

internal sealed class DrillDetail(Drill drill)
{
    public Drill Drill { get; } = drill;
    public string Title => Drill.Title;
    public string Prompt => Drill.Prompt;
    public bool IsExample => Drill.Id == Guid.Empty;
    public string PanelSource => IsExample ? "Mixer example" : "Saved drill";

    public string ControlSummary
    {
        get
        {
            var count = Drill.Controls.Count;
            var protectedCount = Drill.Controls.Count(control => control.Protected);
            var countText = count == 1 ? "1 control" : $"{count} controls";
            return protectedCount == 0 ? countText : $"{countText} · {protectedCount} protected";
        }
    }

    public Drill NewDraft() => Drill with { Id = Guid.NewGuid() };

    public Task ShareAsync() => DrillFiles.ShareAsync(Drill);

    public Task DeleteAsync() => DrillLibrary.DeleteAsync(Drill.Id);
}
