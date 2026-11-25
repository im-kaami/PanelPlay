using Microsoft.Maui.Controls.Shapes;

namespace PanelPlay.Views;

internal static class Ui
{
    public static readonly Color Ink = Color.FromArgb("#172631");
    public static readonly Color Accent = Color.FromArgb("#075E50");
    public static readonly Color Paper = Color.FromArgb("#F6F5F0");
    public static readonly Color Surface = Colors.White;
    public static readonly Color Navy = Color.FromArgb("#111F2C");
    public static readonly Color Muted = Color.FromArgb("#52636A");
    public static readonly Color Line = Color.FromArgb("#D9DFDA");
    public static readonly Color Tint = Color.FromArgb("#DEF4E9");
    public static readonly Color Signal = Color.FromArgb("#65F4CB");

    public static Label Text(string text, double size = 16, bool bold = false)
    {
        return new Label
        {
            Text = text, FontSize = size, TextColor = Ink,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Label Kicker(string text)
    {
        var label = Text(text, 12, true);
        label.TextColor = Muted;
        label.CharacterSpacing = .75;
        return label;
    }

    public static Label SetHeading(Label label)
    {
        SemanticProperties.SetHeadingLevel(label, SemanticHeadingLevel.Level1);
        return label;
    }

    public static Button Button(string title, Func<Task> action) =>
        ActionButton(title, action, Accent, Colors.White);

    public static Button SecondaryButton(string title, Func<Task> action) =>
        ActionButton(title, action, Tint, Ink);

    public static Button DangerButton(string title, Func<Task> action) =>
        ActionButton(title, action, Color.FromArgb("#F9E8E3"), Color.FromArgb("#8A2D21"));

    private static Button ActionButton(string title, Func<Task> action, Color background, Color foreground)
    {
        var button = new Button
        {
            Text = title, BackgroundColor = background, TextColor = foreground,
            CornerRadius = 12, MinimumHeightRequest = 50, FontSize = 15,
            FontAttributes = FontAttributes.Bold, Padding = new Thickness(12, 10),
            LineBreakMode = LineBreakMode.WordWrap
        };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await action();
            }
            finally
            {
                button.IsEnabled = true;
            }
        };
        return button;
    }

    public static Border Card(View content, Color? background = null, Thickness? padding = null) =>
        new()
        {
            Content = content, BackgroundColor = background ?? Surface,
            Stroke = new SolidColorBrush(background ?? Line), StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = padding ?? new Thickness(16)
        };

    public static Grid EqualRow(View left, View right)
    {
        var row = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }
        };
        row.Add(left, 0, 0);
        row.Add(right, 1, 0);
        return row;
    }

    public static ScrollView Scroll(params View[] views)
    {
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 14, BackgroundColor = Paper };
        foreach (var view in views)
            layout.Add(view);
        return new ScrollView { Content = layout };
    }

    public static async Task TryAsync(ContentPage page, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            var message = error switch
            {
                InvalidDataException or InvalidOperationException when error is not ObjectDisposedException => error.Message,
                PermissionException => "Allow camera access in Android settings to take a panel photo.",
                _ => "The operation could not finish. Your saved drills are still available."
            };
            await page.DisplayAlertAsync("PanelPlay", message, "OK");
        }
    }
}
