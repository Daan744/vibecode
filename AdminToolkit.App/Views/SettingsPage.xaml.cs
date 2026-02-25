using System.Windows.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
