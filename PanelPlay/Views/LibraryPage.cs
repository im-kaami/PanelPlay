using PanelPlay.Models;
using PanelPlay.Services;
using PanelPlay.ViewModels;

namespace PanelPlay.Views;

internal sealed class LibraryPage : ContentPage
{
    private static readonly ControlCountConverter CountConverter = new();
    private readonly DrillLibrary library = new();
    private readonly Drill firstDrill = BuiltInDrills.MixerA();
    private readonly Drill secondDrill = BuiltInDrills.MixerB();
    private readonly CollectionView savedDrills;
    private readonly ScrollView emptyLibrary;

    public LibraryPage()
    {
        Title = "PanelPlay";
        BackgroundColor = Ui.Paper;
        BindingContext = library;

        savedDrills = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            Header = BuildHeader(),
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            ItemTemplate = new DataTemplate(BuildSavedDrill),
            IsVisible = false
        };
        savedDrills.SetBinding(ItemsView.ItemsSourceProperty, nameof(DrillLibrary.Drills));

        var emptyState = Ui.Card(new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Ui.Text("No drills saved yet", 16, true),
                new Label
                {
                    Text = "Create a drill or import one someone shared with you.",
                    TextColor = Ui.Muted, FontSize = 14
                }
            }
        });
        emptyState.Margin = new Thickness(20, 0, 20, 20);
        emptyLibrary = new ScrollView
        {
            Content = new VerticalStackLayout { Children = { BuildHeader(), emptyState } }
        };
        Content = new Grid { Children = { savedDrills, emptyLibrary } };
    }

    private View BuildHeader()
    {
        var heading = Ui.SetHeading(Ui.Text("Practice on your own panel", 30, true));
        heading.TextColor = Colors.White;
        var description = Ui.Text("Turn a panel photo into an offline troubleshooting drill.", 15);
        description.TextColor = Color.FromArgb("#C8D8DD");

        var heroLayout = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }
        };
        heroLayout.Add(new VerticalStackLayout
        {
            Spacing = 7, Children = { heading, description }
        }, 0, 0);
        heroLayout.Add(new GraphicsView
        {
            Drawable = new MixerIllustration(), WidthRequest = 70, HeightRequest = 92,
            VerticalOptions = LayoutOptions.Center, InputTransparent = true
        }, 1, 0);

        var examples = new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                Ui.SetHeading(Ui.Text("Try a mixer drill", 20, true)),
                new Label
                {
                    Text = "Two setups with the same symptom.",
                    TextColor = Ui.Muted, FontSize = 14
                },
                Ui.EqualRow(
                    Ui.SecondaryButton(firstDrill.Title, () => OpenAsync(firstDrill)),
                    Ui.SecondaryButton(secondDrill.Title, () => OpenAsync(secondDrill)))
            }
        };

        return new VerticalStackLayout
        {
            Padding = new Thickness(20, 18, 20, 10),
            Spacing = 15,
            Children =
            {
                Ui.Card(heroLayout, Ui.Navy),
                Ui.EqualRow(Ui.Button("New drill", () => Ui.TryAsync(this,
                    () => Navigation.PushAsync(new EditorPage(new DrillEditor(Drill.New()))))),
                    Ui.SecondaryButton("Import drill", ImportAsync)),
                Ui.Card(examples),
                Ui.SetHeading(Ui.Text("Saved drills", 21, true))
            }
        };
    }

    private View BuildSavedDrill()
    {
        var content = new VerticalStackLayout { Spacing = 7 };
        var summary = Ui.Text("", 14);
        summary.TextColor = Ui.Muted;
        summary.SetBinding(Label.TextProperty, new Binding("Controls.Count", converter: CountConverter));

        // The template's data context changes when a list item is recycled.
        var open = Ui.SecondaryButton("", () => content.BindingContext is Drill drill
            ? OpenAsync(drill) : Task.CompletedTask);
        open.SetBinding(Button.TextProperty, nameof(Drill.Title));
        content.Children.Add(open);
        content.Children.Add(summary);
        var card = Ui.Card(content, padding: new Thickness(10));
        card.Margin = new Thickness(20, 0, 20, 10);
        return card;
    }

    private Task OpenAsync(Drill drill) =>
        Ui.TryAsync(this, () => Navigation.PushAsync(new DetailPage(drill)));

    private Task ImportAsync() => Ui.TryAsync(this, async () =>
    {
        if (await DrillFiles.ImportAsync() is { } imported)
        {
            await DrillLibrary.SaveAsync(imported);
            await RefreshAsync();
        }
    });

    private async Task RefreshAsync()
    {
        await library.RefreshAsync();
        var hasDrills = library.Drills.Count > 0;
        savedDrills.IsVisible = hasDrills;
        emptyLibrary.IsVisible = !hasDrills;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Ui.TryAsync(this, RefreshAsync);
    }

    private sealed class MixerIllustration : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.FillColor = Color.FromArgb("#233543");
            canvas.FillRoundedRectangle(bounds.X, bounds.Y + 3, bounds.Width, bounds.Height - 6, 14);
            for (var track = 0; track < 3; track++)
            {
                var x = bounds.X + 15 + track * 20;
                var position = bounds.Y + (track == 1 ? 54 : 27 + track * 8);
                canvas.StrokeColor = Color.FromArgb("#496170");
                canvas.StrokeSize = 2;
                canvas.DrawLine(x, bounds.Y + 19, x, bounds.Bottom - 19);
                canvas.FillColor = track == 1 ? Ui.Signal : Color.FromArgb("#A5B7BF");
                canvas.FillRoundedRectangle(x - 6, position, 12, 8, 2);
            }
        }
    }
}
