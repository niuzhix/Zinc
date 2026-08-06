using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Zinc.Models;
using Zinc.Views;

namespace Zinc.ViewModels;
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TabItemModel> _tabs = new ObservableCollection<TabItemModel>()
    {
        new TabItemModel(){ Header = "New Document 0", Content = new EditorView(){ } }
    };

}
