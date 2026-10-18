using DesignPatterns.Core.ViewModels;

namespace DesignPatterns.App;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        MainPage = new AppShell(services);
    }
}
