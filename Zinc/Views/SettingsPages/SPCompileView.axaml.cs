using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views.SettingsPages;

public partial class SPCompileView : UserControl
{
    public SPCompileView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SPCompileViewModel>();
    }
}