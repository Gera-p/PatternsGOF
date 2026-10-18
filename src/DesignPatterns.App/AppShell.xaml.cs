namespace DesignPatterns.App;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        Routing.RegisterRoute(Routing.Category, typeof(CategoryPage));
        Routing.RegisterRoute(Routing.Pattern, typeof(PatternDetailPage));
        Routing.RegisterRoute(Routing.Favorites, typeof(FavoritesPage));

        // Страницы создаются через DI (MauiApp IServiceProvider).
        NavigationPage.SetHasNavigationBar(this, false);
    }
}
