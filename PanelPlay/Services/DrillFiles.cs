using System.Text.Json;
using PanelPlay.Models;
using BitmapFactory = Android.Graphics.BitmapFactory;

namespace PanelPlay.Services;

internal static class DrillFiles
{
    private const int FileLimit = 4_000_000;
    private const int PhotoLimit = 2_000_000;
    private const int MaxPhotoDimension = 4096;
    private static readonly FilePickerFileType DrillType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["application/json", "application/octet-stream"]
    });

    public static async Task<string?> CaptureAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            throw new InvalidOperationException("This device has no available camera.");

        var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            MaximumWidth = 1200,
            MaximumHeight = 1200,
            CompressionQuality = 75,
            RotateImage = true,
            PreserveMetaData = false
        });
        if (photo is null)
            return null;

        await using var stream = await photo.OpenReadAsync();
        var image = await ReadBoundedAsync(stream, PhotoLimit);
        await Task.Run(() => ValidatePhoto(image));
        return Convert.ToBase64String(image);
    }

    public static async Task ShareAsync(Drill drill)
    {
        var payload = await Task.Run(() =>
        {
            Validate(drill);
            return JsonSerializer.Serialize(drill);
        });
        var folder = Path.Combine(FileSystem.CacheDirectory, "sharing-root");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"panelplay-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, payload);
        await Share.Default.RequestAsync(new ShareFileRequest(drill.Title, new ShareFile(path, "application/json")));
    }

    public static async Task<Drill?> ImportAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Open a PanelPlay drill",
            FileTypes = DrillType
        });
        if (file is null)
            return null;

        await using var stream = await file.OpenReadAsync();
        var bytes = await ReadBoundedAsync(stream, FileLimit);
        return await Task.Run(() =>
        {
            Drill drill;
            try
            {
                drill = JsonSerializer.Deserialize<Drill>(bytes) ?? throw new InvalidDataException("Empty drill file.");
            }
            catch (JsonException error)
            {
                throw new InvalidDataException("Choose a valid PanelPlay JSON drill file.", error);
            }
            Validate(drill);
            return drill with { Id = Guid.NewGuid() };
        });
    }

    public static void Validate(Drill drill)
    {
        if (string.IsNullOrWhiteSpace(drill.Title) || drill.Title.Length > 80)
            throw new InvalidDataException("Enter a drill title of 1 to 80 characters.");
        if (string.IsNullOrWhiteSpace(drill.Prompt) || drill.Prompt.Length > 600)
            throw new InvalidDataException("Enter a learner prompt of 1 to 600 characters.");
        if (drill.Controls is null || drill.Controls.Count is < 1 or > 6)
            throw new InvalidDataException("Add between one and six controls.");
        if (drill.Controls.Any(control => control is null || string.IsNullOrWhiteSpace(control.Name) ||
            control.Name.Length > 40 || !double.IsFinite(control.X) || !double.IsFinite(control.Y) ||
            control.X is < 0 or > 1 || control.Y is < 0 or > 1 ||
            (control.Protected && control.Initial != control.Target)))
            throw new InvalidDataException("Check the control names, positions, and protected settings.");
        if (drill.Controls.All(control => control.Initial == control.Target))
            throw new InvalidDataException("Set at least one control to a different target state.");
        if (drill.Photo is { } photo)
        {
            if (photo.Length > PhotoLimit * 4 / 3 + 4)
                throw new InvalidDataException("The panel photo is too large.");
            byte[] image;
            try
            {
                image = Convert.FromBase64String(photo);
            }
            catch (FormatException error)
            {
                throw new InvalidDataException("The panel photo could not be read.", error);
            }
            ValidatePhoto(image);
        }
    }

    private static void ValidatePhoto(byte[] image)
    {
        if (image.Length > PhotoLimit)
            throw new InvalidDataException("The panel photo is too large.");
        if (image.Length < 5 || image[0] != 255 || image[1] != 216 || image[2] != 255 ||
            image[^2] != 255 || image[^1] != 217)
            throw new InvalidDataException("The panel photo must be a complete JPEG.");

        using var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(image, 0, image.Length, bounds);
        if (bounds.OutWidth < 1 || bounds.OutHeight < 1)
            throw new InvalidDataException("The panel photo could not be read.");
        if (bounds.OutWidth > MaxPhotoDimension || bounds.OutHeight > MaxPhotoDimension)
            throw new InvalidDataException($"Each panel photo dimension must be at most {MaxPhotoDimension} pixels.");

        using var decoded = BitmapFactory.DecodeByteArray(image, 0, image.Length);
        if (decoded is null)
            throw new InvalidDataException("The panel photo could not be read.");
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, int limit)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk)) != 0)
        {
            if (buffer.Length + read > limit)
                throw new InvalidDataException("This file is too large.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
