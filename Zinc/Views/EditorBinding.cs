using Avalonia;
using AvaloniaEdit;
using Zinc.ViewModels;

namespace Zinc.Views;

/// <summary>把 <see cref="EditorViewModel"/> 与 <see cref="TextEditor"/> 关联起来的附加属性。</summary>
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
            if (e.OldValue is EditorViewModel oldViewModel)
            {
                oldViewModel.DetachEditor(editor);
            }

            if (e.NewValue is EditorViewModel newViewModel)
            {
                newViewModel.AttachEditor(editor);
            }
        });
    }
}
