using System.Globalization;

namespace PanelPlay.Views;

internal sealed class ControlCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int count
            ? count.ToString(culture) + (count == 1 ? " control" : " controls")
            : "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
