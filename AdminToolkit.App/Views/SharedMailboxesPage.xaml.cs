using System.Windows.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App.Views;

public partial class SharedMailboxesPage : Page
{
    public SharedMailboxesPage(SharedMailboxesViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
