using Microsoft.UI.Xaml.Controls;

using SDParamsDescripter.ViewModels;

namespace SDParamsDescripter.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        ViewModel.DispatcherQueue = DispatcherQueue;
        InitializeComponent();
    }
}
