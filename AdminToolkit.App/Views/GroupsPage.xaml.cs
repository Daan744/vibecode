using System.Windows.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App.Views;

public partial class GroupsPage : Page
{
    public GroupsPage(GroupsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
