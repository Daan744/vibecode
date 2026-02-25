using Wpf.Ui;
using Wpf.Ui.Controls;
using AdminToolkit.App.ViewModels;

namespace AdminToolkit.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow(
        IServiceProvider serviceProvider,
        MainViewModel viewModel,
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        DataContext = viewModel;
        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        RootNavigation.SetServiceProvider(serviceProvider);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogPresenter);

        Loaded += (_, _) => RootNavigation.Navigate(typeof(Views.DashboardPage));
    }
}
