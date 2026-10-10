using Avalonia;
using AvaloniaEdit;
using Zinc.ViewModels;

namespace Zinc.Core.Views;

public static class EditorBinding
{
    public static readonly AttachedProperty<EditorViewModel?> HostProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, EditorViewModel?>(
            "Host", typeof(EditorBinding));

    public static void SetHost(TextEditor editor, EditorViewModel? value)
        => editor.SetValue(HostProperty, value);

    public static EditorViewModel? GetHost(TextEditor editor)
        => editor.GetValue(HostProperty);

    static EditorBinding()
    {
        HostProperty.Changed.AddClassHandler<TextEditor>((editor, e) =>
        {
            if (e.NewValue is EditorViewModel vm)
                vm.AttachEditor(editor);
        });
    }
}