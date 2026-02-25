using System.Windows.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App.Views;

public partial class UsersPage : Page
{
    public UsersPage(UsersViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
