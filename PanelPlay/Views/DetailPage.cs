using PanelPlay.Models;
using PanelPlay.ViewModels;

namespace PanelPlay.Views;

internal sealed class DetailPage : ContentPage
{
    public DetailPage(Drill drill)
    {
        Title = "Drill";
        BackgroundColor = Ui.Paper;
        var details = new DrillDetail(drill);
        BindingContext = details;

        var title = Ui.SetHeading(Ui.Text("", 29, true));
        title.SetBinding(Label.TextProperty, nameof(DrillDetail.Title));
        var source = Ui.Kicker("");
        source.SetBinding(Label.TextProperty, nameof(DrillDetail.PanelSource));
        var summary = Ui.Text("", 14);
        summary.TextColor = Ui.Muted;
        summary.SetBinding(Label.TextProperty, nameof(DrillDetail.ControlSummary));
        var prompt = Ui.Text("");
        prompt.SetBinding(Label.TextProperty, nameof(DrillDetail.Prompt));

        var panel = new PanelCanvas { HeightRequest = 152, InputTransparent = true };
        panel.SetPhoto(drill.Photo);
        panel.SetControls(drill.Controls, index => drill.Controls[index].Initial);

        var edit = Ui.SecondaryButton(details.IsExample ? "Use as template" : "Edit drill", () => EditAsync(
            details.IsExample ? details.NewDraft() : details.Drill));
        var actions = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Ui.Button("Start practice", () => Ui.TryAsync(this,
                    () => Navigation.PushAsync(new PracticePage(details.Drill)))),
                Ui.SecondaryButton("Share playable drill", () => Ui.TryAsync(this, details.ShareAsync)),
                edit
            }
        };

        if (!details.IsExample)
        {
            actions.Add(Ui.SecondaryButton("Duplicate as new drill", () => EditAsync(details.NewDraft())));
            actions.Add(Ui.DangerButton("Delete drill", () => Ui.TryAsync(this, async () =>
            {
                if (await DisplayAlertAsync("Delete drill?", details.Title, "Delete", "Cancel"))
                {
                    await details.DeleteAsync();
                    await Navigation.PopAsync();
                }
            })));
        }

        Content = Ui.Scroll(new VerticalStackLayout { Spacing = 4, Children = { source, title, summary } },
            Ui.Card(panel, Ui.Navy, new Thickness(0)),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 5,
                Children = { Ui.SetHeading(Ui.Text("Objective", 17, true)), prompt }
            }),
            actions);
    }

    private Task EditAsync(Drill draft) => Ui.TryAsync(this,
        () => Navigation.PushAsync(new EditorPage(new DrillEditor(draft))));
}
