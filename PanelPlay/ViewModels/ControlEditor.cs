using PanelPlay.Models;

namespace PanelPlay.ViewModels;

internal sealed class ControlEditor(Control original)
{
    public string Name { get; set; } = original.Name;
    public bool Initial { get; set; } = original.Initial;
    public bool Target { get; set; } = original.Target;
    public bool Protected { get; set; } = original.Protected;

    public Control Apply()
    {
        var name = Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 40)
            throw new InvalidDataException("Enter a control name of 1 to 40 characters.");
        if (Protected && Initial != Target)
            throw new InvalidDataException("A protected control must start and end in the same state.");

        return original with { Name = name, Initial = Initial, Target = Target, Protected = Protected };
    }
}
