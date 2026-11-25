using PanelPlay.Views;

namespace PanelPlay;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(new LibraryPage())
        {
            BarBackgroundColor = Ui.Navy,
            BarTextColor = Colors.White
        });
    }
}
