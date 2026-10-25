namespace Zinc.Abstractions;

/// <summary>由 ViewModel 发起的窗口操作。</summary>
public interface IWindowService
{
    /// <summary>打开设置窗口；已打开时将其激活。</summary>
    void ShowSettings();
}
