using System.Collections.Specialized;

namespace CanvasViewer.Media.EventArg;

#region 委托

public delegate void DocumentChangedEventHandler(object sender, EventArgs e);

public delegate void TransientsChangedEventHandler(object sender, EventArgs e);

public delegate void SelectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e);

public delegate void CursorEventHandler(object sender, CursorEventArgs e);

public delegate void EditorPromptEventHandler(object sender, EditorPromptEventArgs e);

public delegate void EditorErrorEventHandler(object sender, EditorErrorEventArgs e);

#endregion 委托