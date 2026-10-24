using Avalonia;
using AvaloniaEdit;
using Zinc.ViewModels;

namespace Zinc.Core.Views;

public static class EditorBinding
{
    public static readonly AttachedProperty<EditorViewModel?> HostProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, EditorViewModel?>("Host", typeof(EditorBinding));

    public static void SetHost(TextEditor element, EditorViewModel? value)
        => element.SetValue(HostProperty, value);

    public static EditorViewModel? GetHost(TextEditor element)
        => element.GetValue(HostProperty);

    static EditorBinding()
    {
        HostProperty.Changed.AddClassHandler<TextEditor>((editor, e) =>
        {
            if (e.OldValue is EditorViewModel oldVm)
            {
                oldVm.DetachEditor(editor);
            }

            if (e.NewValue is EditorViewModel newVm)
            {
                newVm.AttachEditor(editor);
            }
        });
    }
}