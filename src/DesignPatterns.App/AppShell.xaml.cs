using DesignPatterns.App.Pages;

namespace DesignPatterns.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Маршруты для Shell.GoToAsync; id приходит через query-параметр «id».
        Routing.RegisterRoute(Routing.Category, typeof(CategoryPage));
        Routing.RegisterRoute(Routing.Pattern, typeof(PatternDetailPage));
        Routing.RegisterRoute(Routing.Favorites, typeof(FavoritesPage));
    }
}
