namespace CanvasViewer.Media.EventArg;

public sealed class EditorPromptEventArgs(string status) : EventArgs
{
    public string Status { get; private set; } = status;

    public EditorPromptEventArgs() : this(string.Empty)
    {
    }
}