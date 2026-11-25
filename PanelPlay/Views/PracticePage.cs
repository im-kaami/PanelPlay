using Microsoft.Maui.Controls.Shapes;
using PanelPlay.Models;
using PanelPlay.ViewModels;

namespace PanelPlay.Views;

internal sealed class PracticePage : ContentPage
{
    private static readonly Color FailedBackground = Color.FromArgb("#FCE5DB");
    private static readonly Color FailedInk = Color.FromArgb("#8A3421");
    private static readonly Color SolvedBackground = Color.FromArgb("#D5F3E8");
    private static readonly Color SolvedInk = Color.FromArgb("#085747");
    private static readonly Color ActiveBackground = Color.FromArgb("#F4E9CC");

    public PracticePage(Drill drill)
    {
        Title = "Practice";
        BackgroundColor = Ui.Paper;
        var practice = new PracticeViewModel(drill);
        BindingContext = practice;

        var panel = new PanelCanvas { HeightRequest = 178 };
        panel.SetPhoto(drill.Photo);
        panel.SetControls(drill.Controls, practice.State);
        panel.Tapped += point =>
        {
            if (panel.FindMarker(point) is int index)
                practice.Toggle(index);
        };
        practice.PropertyChanged += (_, _) => panel.Invalidate();

        var title = Ui.SetHeading(Ui.Text(drill.Title, 28, true));
        var controls = new VerticalStackLayout { Spacing = 7 };
        foreach (var control in practice.Switches)
        {
            var button = new Button
            {
                BindingContext = control,
                BackgroundColor = Ui.Surface,
                TextColor = Ui.Ink,
                BorderColor = Ui.Line,
                BorderWidth = 1,
                CornerRadius = 10,
                MinimumHeightRequest = 48,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap,
                Command = control.ToggleCommand
            };
            button.SetBinding(Button.TextProperty, nameof(PracticeSwitch.Text));
            var on = new DataTrigger(typeof(Button)) { Binding = new Binding(nameof(PracticeSwitch.IsOn)), Value = true };
            on.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Ui.Tint });
            button.Triggers.Add(on);
            controls.Add(button);
        }

        var restart = new Button
        {
            Text = "Restart drill",
            BackgroundColor = Ui.Accent,
            TextColor = Colors.White,
            CornerRadius = 10,
            MinimumHeightRequest = 48,
            Command = practice.RestartCommand
        };
        var panelFrame = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = panel
        };
        var content = new List<View>
        {
            title, Ui.Text(drill.Prompt, 15), StatusPanel(), panelFrame
        };
        if (practice.HasSignalReadings)
            content.Add(SignalPanel());
        var switchesHeading = Ui.Text("Switches", 17, true);
        SemanticProperties.SetHeadingLevel(switchesHeading, SemanticHeadingLevel.Level2);
        content.AddRange([switchesHeading, controls, restart]);
        Content = Ui.Scroll([.. content]);
    }

    private static Border StatusPanel()
    {
        var label = Ui.Text("", 15, true);
        label.SetBinding(Label.TextProperty, nameof(PracticeViewModel.Status));
        var status = new Border
        {
            Padding = new Thickness(12, 9),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = ActiveBackground,
            Content = label
        };
        AddStatusTrigger(status, VisualElement.BackgroundColorProperty, nameof(PracticeViewModel.IsSolved), SolvedBackground);
        AddStatusTrigger(status, VisualElement.BackgroundColorProperty, nameof(PracticeViewModel.IsFailed), FailedBackground);
        AddStatusTrigger(label, Label.TextColorProperty, nameof(PracticeViewModel.IsSolved), SolvedInk);
        AddStatusTrigger(label, Label.TextColorProperty, nameof(PracticeViewModel.IsFailed), FailedInk);
        return status;
    }

    private static View SignalPanel()
    {
        var readings = new Grid { ColumnSpacing = 8 };
        var signals = new (string Label, string Property)[]
        {
            ("Mic input", nameof(PracticeViewModel.MicrophoneInput)),
            ("Mic at Main", nameof(PracticeViewModel.MicrophoneAtMain)),
            ("Music at Main", nameof(PracticeViewModel.MusicAtMain))
        };
        for (var column = 0; column < signals.Length; column++)
        {
            var signal = signals[column];
            readings.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var value = Ui.Text("", 13, true);
            value.SetBinding(Label.TextProperty, signal.Property);
            var active = new DataTrigger(typeof(Label)) { Binding = new Binding(signal.Property), Value = "Active" };
            active.Setters.Add(new Setter { Property = Label.TextColorProperty, Value = SolvedInk });
            value.Triggers.Add(active);
            readings.Add(new VerticalStackLayout
            {
                Spacing = 3,
                Children = { Ui.Text(signal.Label, 12), value }
            }, column, 0);
        }
        var heading = Ui.Text("Simulated signal", 13, true);
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level2);
        return Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { heading, readings } },
            padding: new Thickness(12));
    }

    private static void AddStatusTrigger(VisualElement element, BindableProperty property, string state, Color color)
    {
        var trigger = new DataTrigger(element.GetType()) { Binding = new Binding(state), Value = true };
        trigger.Setters.Add(new Setter { Property = property, Value = color });
        element.Triggers.Add(trigger);
    }
}
