using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views.SettingsPages;

public partial class SPEditView : UserControl
{
    public SPEditView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SPEditViewModel>();
    }
}