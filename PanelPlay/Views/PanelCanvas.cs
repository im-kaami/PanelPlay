using Microsoft.Maui.Graphics.Platform;
using PanelPlay.Models;

namespace PanelPlay.Views;

internal sealed class PanelCanvas : GraphicsView
{
    private const float MarkerRadius = 18;
    private const float HitRadius = 22;
    private readonly Surface surface = new();
    private string? encodedPhoto;

    public PanelCanvas()
    {
        HeightRequest = 250;
        Drawable = surface;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, args) =>
        {
            if (args.GetPosition(this) is { } point)
                Tapped?.Invoke(point);
        };
        GestureRecognizers.Add(tap);
        Loaded += (_, _) =>
        {
            if (surface.Image is null && encodedPhoto is not null)
                SetPhoto(encodedPhoto);
        };
        Unloaded += (_, _) => ReplaceImage(null);
    }

    public event Action<Point>? Tapped;

    public Point? GetPanelPosition(Point point)
    {
        var bounds = surface.PanelBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 || !bounds.Contains((float)point.X, (float)point.Y))
            return null;
        return new Point((point.X - bounds.X) / bounds.Width, (point.Y - bounds.Y) / bounds.Height);
    }

    public int? FindMarker(Point point)
    {
        var bounds = surface.PanelBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;
        double nearestDistance = HitRadius * HitRadius;
        int? nearest = null;
        for (var index = 0; index < surface.Controls.Count; index++)
        {
            var control = surface.Controls[index];
            var dx = bounds.X + control.X * bounds.Width - point.X;
            var dy = bounds.Y + control.Y * bounds.Height - point.Y;
            var distance = dx * dx + dy * dy;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = index;
            }
        }
        return nearest;
    }

    public void SetPhoto(string? photo)
    {
        Microsoft.Maui.Graphics.IImage? image = null;
        if (photo is not null)
        {
            using var stream = new MemoryStream(Convert.FromBase64String(photo));
            image = PlatformImage.FromStream(stream);
        }
        encodedPhoto = photo;
        ReplaceImage(image);
        Invalidate();
    }

    private void ReplaceImage(Microsoft.Maui.Graphics.IImage? image)
    {
        surface.Image?.Dispose();
        surface.Image = image;
    }

    public void SetControls(IEnumerable<Control> controls, Func<int, bool>? state = null)
    {
        surface.Controls = controls.ToArray();
        surface.State = state;
        Invalidate();
    }

    private sealed class Surface : IDrawable
    {
        private static readonly Color Chassis = Color.FromArgb("#142331");
        private static readonly Color Module = Color.FromArgb("#1D3040");
        private static readonly Color Engraving = Color.FromArgb("#395363");
        private static readonly Color Caption = Color.FromArgb("#96B3BD");
        private static readonly Color MarkerOn = Color.FromArgb("#50DFC4");
        private static readonly Color MarkerOff = Color.FromArgb("#20384A");

        public Microsoft.Maui.Graphics.IImage? Image { get; set; }
        public IReadOnlyList<Control> Controls { get; set; } = [];
        public Func<int, bool>? State { get; set; }
        public RectF PanelBounds { get; private set; }

        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.FillColor = Chassis;
            canvas.FillRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            PanelBounds = bounds;
            if (Image is { Width: > 0, Height: > 0 } image)
            {
                var scale = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
                var width = image.Width * scale;
                var height = image.Height * scale;
                PanelBounds = new RectF(bounds.X + (bounds.Width - width) / 2,
                    bounds.Y + (bounds.Height - height) / 2, width, height);
                canvas.DrawImage(image, PanelBounds.X, PanelBounds.Y, width, height);
            }
            else
            {
                DrawSchematic(canvas, bounds);
            }

            for (var index = 0; index < Controls.Count; index++)
            {
                var control = Controls[index];
                var x = PanelBounds.X + (float)control.X * PanelBounds.Width;
                var y = PanelBounds.Y + (float)control.Y * PanelBounds.Height;
                var on = State?.Invoke(index) == true;
                canvas.FillColor = on ? MarkerOn : MarkerOff;
                canvas.FillCircle(x, y, MarkerRadius);
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = 2;
                canvas.DrawCircle(x, y, MarkerRadius);
                canvas.FontColor = on ? Chassis : Colors.White;
                canvas.FontSize = 15;
                canvas.DrawString((index + 1).ToString(), x - MarkerRadius, y - MarkerRadius,
                    MarkerRadius * 2, MarkerRadius * 2,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        private void DrawSchematic(ICanvas canvas, RectF bounds)
        {
            const float padding = 12;
            canvas.StrokeColor = Engraving;
            canvas.StrokeSize = 1;
            for (var x = bounds.X + padding; x < bounds.Right - padding; x += 16)
            {
                for (var y = bounds.Y + padding; y < bounds.Bottom - padding; y += 16)
                    canvas.DrawLine(x, y, x + 1, y + 1);
            }

            if (!Controls.Any(control => control.MixerRole != MixerControl.None))
                return;

            var moduleWidth = bounds.Width * .24f;
            var moduleHeight = bounds.Height * .58f;
            var moduleTop = bounds.Y + bounds.Height * .23f;
            for (var index = 0; index < 3; index++)
            {
                var center = bounds.X + bounds.Width * (.2f + index * .3f);
                var moduleLeft = center - moduleWidth / 2;
                canvas.FillColor = Module;
                canvas.FillRoundedRectangle(moduleLeft, moduleTop, moduleWidth, moduleHeight, 6);
                canvas.DrawRoundedRectangle(moduleLeft, moduleTop, moduleWidth, moduleHeight, 6);

                canvas.DrawLine(center, moduleTop + 18, center, moduleTop + moduleHeight - 19);
                for (var tick = 0; tick < 5; tick++)
                {
                    var y = moduleTop + 20 + tick * (moduleHeight - 40) / 4;
                    canvas.DrawLine(center - 8, y, center + 8, y);
                }
            }
            canvas.FontColor = Caption;
            canvas.FontSize = 9;
            canvas.DrawString("SIGNAL ROUTING", bounds.X + padding, bounds.Y + padding,
                bounds.Width - 2 * padding, 15, HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }
}
