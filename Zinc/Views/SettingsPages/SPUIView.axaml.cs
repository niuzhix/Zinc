using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views.SettingsPages;

public partial class SPUIView : UserControl
{
    public SPUIView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SPUIViewModel>();
    }
}