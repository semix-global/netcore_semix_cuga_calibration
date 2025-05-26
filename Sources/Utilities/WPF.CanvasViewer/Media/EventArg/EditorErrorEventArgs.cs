namespace CanvasViewer.Media.EventArg;

public sealed class EditorErrorEventArgs(Exception? error) : EventArgs
{
    public Exception? Error { get; private set; } = error;
}