using PanelPlay.Models;
using PanelPlay.ViewModels;

namespace PanelPlay.Views;

internal sealed class ControlPage : ContentPage
{
    private bool actionInProgress;

    public ControlPage(Control control, Action<Control?> save, Func<Task>? move)
    {
        var editor = new ControlEditor(control);
        Title = "Panel control";
        BindingContext = editor;
        NavigationPage.SetHasBackButton(this, false);
        var heading = Ui.Text(move is null ? "Add a control" : "Edit control", 26, true);
        Ui.SetHeading(heading);
        var name = new Entry
        {
            Placeholder = "Control name", MaxLength = 40, TextColor = Ui.Ink,
            PlaceholderColor = Ui.Muted, MinimumHeightRequest = 48
        };
        name.SetBinding(Entry.TextProperty, nameof(ControlEditor.Name), BindingMode.TwoWay);
        SemanticProperties.SetDescription(name, "Control name");

        async Task ApplyAsync()
        {
            save(editor.Apply());
            await Navigation.PopAsync();
        }

        var settings = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Text("Control name", 14, true),
                name,
                Row("Starts on", "State at the beginning of practice", nameof(ControlEditor.Initial)),
                Row("Must end on", "State needed to solve the fault", nameof(ControlEditor.Target)),
                Row("Must stay unchanged", "Changing this control ends the attempt", nameof(ControlEditor.Protected))
            }
        };
        var content = new List<View>
        {
            heading,
            Ui.Card(settings),
            Ui.Button("Apply control", () => RunAsync(ApplyAsync))
        };
        if (move is not null)
        {
            content.Add(Ui.SecondaryButton("Move pin", () => RunAsync(async () =>
            {
                await ApplyAsync();
                await move();
            })));
            content.Add(Ui.DangerButton("Remove control", () => RunAsync(async () =>
            {
                if (!await DisplayAlertAsync("Remove control?", $"Remove {control.Name} from the panel?",
                        "Remove", "Cancel"))
                    return;

                save(null);
                await Navigation.PopAsync();
            })));
        }
        content.Add(Ui.SecondaryButton("Cancel", () => RunAsync(() => Navigation.PopAsync())));
        Content = Ui.Scroll([.. content]);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = RunAsync(() => Navigation.PopAsync());
        return true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (actionInProgress)
            return;

        actionInProgress = true;
        try
        {
            await Ui.TryAsync(this, action);
        }
        finally
        {
            actionInProgress = false;
        }
    }

    private static View Row(string title, string description, string propertyName)
    {
        var toggle = new Switch { HorizontalOptions = LayoutOptions.End, MinimumHeightRequest = 48 };
        toggle.SetBinding(Switch.IsToggledProperty, propertyName, BindingMode.TwoWay);
        SemanticProperties.SetDescription(toggle, title);
        var hint = Ui.Text(description, 14);
        hint.TextColor = Ui.Muted;
        var labels = new VerticalStackLayout
        {
            Spacing = 2, VerticalOptions = LayoutOptions.Center,
            Children = { Ui.Text(title, 16, true), hint }
        };
        var row = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        row.Add(labels, 0, 0);
        row.Add(toggle, 1, 0);
        return row;
    }
}
