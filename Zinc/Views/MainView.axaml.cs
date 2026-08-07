using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using FluentAvalonia.UI.Controls;
using System;
using Zinc.Models;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainView : UserControl
{
	int Count = 0;

	public MainView()
	{
		InitializeComponent();
		DataContext = new MainViewModel();
	}

	private void TabView_AddTabButtonClick(FATabView sender, System.EventArgs args)
	{
		if(DataContext is MainViewModel vm)
		{
			vm.Tabs.Add(
				new TabItemModel() { Header = $"New Document {++Count}", Content = new EditorView() { } }
			);
		}
	}

	private void TabView_TabCloseRequested(FATabView sender, FATabViewTabCloseRequestedEventArgs args)
	{
		if(DataContext is MainViewModel vm)
		{   
			vm.Tabs.Remove(args.Item as TabItemModel);
			
			if (vm.Tabs.Count > 1)
			{
				vm.Tabs.Add(
					new TabItemModel() { Header = $"New Document {++Count}", Content = new EditorView() { } }
				);
			}
		}
	}
}