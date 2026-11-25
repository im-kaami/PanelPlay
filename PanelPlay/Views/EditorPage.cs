using PanelPlay.Models;
using PanelPlay.ViewModels;

namespace PanelPlay.Views;

internal sealed class EditorPage : ContentPage
{
    private readonly DrillEditor editor;
    private readonly PanelCanvas panel = new() { HeightRequest = 220 };
    private readonly VerticalStackLayout controls = new() { Spacing = 8 };
    private readonly Label instruction = Ui.Text("", 14);
    private readonly Label photoHint = Ui.Text("", 14);
    private readonly ScrollView scroll;
    private readonly Entry title;
    private readonly Editor prompt;
    private readonly Button placementButton;
    private bool placing;
    private bool actionInProgress;
    private int? movingControl;

    public EditorPage(DrillEditor editor)
    {
        this.editor = editor;
        Title = "Drill editor";
        BindingContext = editor;
        NavigationPage.SetHasBackButton(this, false);

        title = new Entry
        {
            Placeholder = "Drill title", MaxLength = 80, TextColor = Ui.Ink,
            PlaceholderColor = Ui.Muted, MinimumHeightRequest = 48
        };
        title.SetBinding(Entry.TextProperty, nameof(DrillEditor.Title), BindingMode.TwoWay);
        SemanticProperties.SetDescription(title, "Drill title");
        prompt = new Editor
        {
            Placeholder = "What should the learner fix?", MaxLength = 600,
            AutoSize = EditorAutoSizeOption.TextChanges, TextColor = Ui.Ink,
            PlaceholderColor = Ui.Muted, MinimumHeightRequest = 72
        };
        prompt.SetBinding(Editor.TextProperty, nameof(DrillEditor.Prompt), BindingMode.TwoWay);
        SemanticProperties.SetDescription(prompt, "Learner task");
        instruction.TextColor = Ui.Muted;
        photoHint.TextColor = Ui.Muted;
        SemanticProperties.SetDescription(panel, "Panel for placing control pins");

        var photograph = Ui.SecondaryButton("Photograph panel", () => RunAsync(async () =>
        {
            if (editor.Photo is not null && editor.Controls.Count > 0 &&
                !await DisplayAlertAsync("Replace photo?", "Check that each pin still matches its control.",
                    "Replace", "Cancel"))
                return;

            await editor.CaptureAsync();
        }));
        void RefreshPhoto()
        {
            panel.SetPhoto(editor.Photo);
            photograph.Text = editor.Photo is null ? "Photograph panel" : "Replace photo";
            photoHint.Text = editor.Photo is null
                ? "Use a photo of your equipment or place the pins on the blank panel."
                : "Pins stay in the same relative positions when the photo scales.";
        }

        editor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DrillEditor.Photo))
                RefreshPhoto();
        };
        editor.Controls.CollectionChanged += (_, _) => RefreshControls();
        panel.Tapped += async point => await RunAsync(() => PlaceControlAsync(point));
        RefreshPhoto();
        RefreshControls();
        ShowPlacementHint();

        var scenarioHeading = Ui.Text("Write the scenario", 26, true);
        var panelHeading = Ui.Text("Build the panel", 23, true);
        Ui.SetHeading(scenarioHeading);
        Ui.SetHeading(panelHeading);
        placementButton = Ui.SecondaryButton("Add control", () => RunAsync(BeginNewControlAsync));
        scroll = Ui.Scroll(
            scenarioHeading,
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 4,
                Children = { Ui.Text("Drill title", 14, true), title, Ui.Text("Learner task", 14, true), prompt }
            }),
            panelHeading, photoHint, photograph, panel, instruction, placementButton,
            controls, Ui.EqualRow(Ui.SecondaryButton("Cancel", () => RunAsync(CancelAsync)),
                Ui.Button("Save drill", () => RunAsync(SaveAsync))));
        Content = scroll;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = RunAsync(CancelAsync);
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

    private async Task BeginNewControlAsync()
    {
        if (placing)
        {
            StopPlacement();
            return;
        }

        if (editor.Controls.Count >= DrillEditor.ControlLimit)
        {
            await DisplayAlertAsync("Panel full", "A drill can have up to six controls.", "OK");
            return;
        }

        await BeginPlacementAsync(null);
    }

    private async Task BeginPlacementAsync(int? index)
    {
        title.Unfocus();
        prompt.Unfocus();
        movingControl = index;
        placing = true;
        placementButton.Text = "Cancel placement";
        instruction.Text = index is int controlIndex
            ? $"Tap the new position for {editor.Controls[controlIndex].Name}."
            : "Tap the panel where the new control belongs.";
        await scroll.ScrollToAsync(panel, ScrollToPosition.Center, true);
    }

    private async Task PlaceControlAsync(Point point)
    {
        if (!placing)
            return;
        if (panel.GetPanelPosition(point) is not { } position)
        {
            instruction.Text = "Tap inside the panel to place the pin.";
            return;
        }

        var index = movingControl;
        StopPlacement();
        if (index is int existing)
        {
            editor.MoveControl(existing, position.X, position.Y);
            instruction.Text = "Pin moved. Save the drill to keep this change.";
            return;
        }

        await OpenControlAsync(null, new Control("", position.X, position.Y, false, true, false));
    }

    private Task EditControlAsync(int index)
    {
        StopPlacement();
        return OpenControlAsync(index, editor.Controls[index]);
    }

    private Task OpenControlAsync(int? index, Control control)
    {
        return Navigation.PushAsync(new ControlPage(control, replacement =>
        {
            if (index is int existing)
            {
                if (replacement is null)
                    editor.Controls.RemoveAt(existing);
                else
                    editor.Controls[existing] = replacement;
            }
            else if (replacement is not null)
            {
                editor.Controls.Add(replacement);
            }
        }, index is int moveIndex ? () => BeginPlacementAsync(moveIndex) : null));
    }

    private void RefreshControls()
    {
        panel.SetControls(editor.Controls);
        controls.Clear();
        for (var index = 0; index < editor.Controls.Count; index++)
        {
            var current = index;
            var control = editor.Controls[index];
            var initial = control.Initial ? "On" : "Off";
            var target = control.Target ? "On" : "Off";
            var summary = Ui.Text(control.Protected ? $"Protected · stays {initial.ToLowerInvariant()}"
                : $"Start {initial.ToLowerInvariant()}  ·  Target {target.ToLowerInvariant()}", 14);
            summary.TextColor = Ui.Muted;
            controls.Add(Ui.Card(new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    Ui.SecondaryButton($"Edit {index + 1}. {control.Name}",
                        () => RunAsync(() => EditControlAsync(current))),
                    summary
                }
            }));
        }
        ShowPlacementHint();
    }

    private void StopPlacement()
    {
        placing = false;
        movingControl = null;
        placementButton.Text = "Add control";
        ShowPlacementHint();
    }

    private void ShowPlacementHint()
    {
        if (!placing)
            instruction.Text = $"{editor.Controls.Count} of {DrillEditor.ControlLimit} controls placed";
    }

    private async Task CancelAsync()
    {
        if (editor.HasChanges &&
            !await DisplayAlertAsync("Discard draft?", "Your saved drill stays as it was.",
                "Discard", "Keep editing"))
            return;

        await Navigation.PopAsync();
    }

    private async Task SaveAsync()
    {
        if (placing)
            throw new InvalidOperationException("Place the pending pin before saving.");

        var drill = await editor.SaveAsync();
        var detail = new DetailPage(drill);
        var previous = Navigation.NavigationStack[^2];
        Navigation.InsertPageBefore(detail, this);
        if (previous is DetailPage)
            Navigation.RemovePage(previous);
        await Navigation.PopAsync();
    }
}
