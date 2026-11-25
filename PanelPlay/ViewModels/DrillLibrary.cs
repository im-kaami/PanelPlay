using System.Collections.ObjectModel;
using PanelPlay.Models;
using PanelPlay.Services;

namespace PanelPlay.ViewModels;

internal sealed class DrillLibrary
{
    public ObservableCollection<Drill> Drills { get; } = [];

    public async Task RefreshAsync()
    {
        var drills = await Task.Run(() =>
        {
            using var store = new DrillStore();
            return store.List();
        });
        Drills.Clear();
        foreach (var drill in drills)
            Drills.Add(drill);
    }

    public static Task SaveAsync(Drill drill) => Task.Run(() =>
    {
        DrillFiles.Validate(drill);
        using var store = new DrillStore();
        store.Save(drill);
    });

    public static Task DeleteAsync(Guid id) => Task.Run(() =>
    {
        using var store = new DrillStore();
        store.Delete(id);
    });
}
