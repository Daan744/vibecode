using System.Windows.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App.Views;

public partial class RunbookPage : Page
{
    public RunbookPage(RunbookViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += (_, _) => viewModel.LoadScriptsCommand.Execute(null);
    }
}
