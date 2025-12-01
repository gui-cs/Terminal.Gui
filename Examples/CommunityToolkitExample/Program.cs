using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace CommunityToolkitExample;

public static class Program
{
    public static IServiceProvider? Services { get; private set; }

    private static void Main (string [] args)
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        Services = ConfigureServices ();
        using IApplication app = Application.Create ();
        app.Init ();
        using var loginView = Services.GetRequiredService<LoginView> ();
        app.Run (loginView);
    }

    private static IServiceProvider ConfigureServices ()
    {
        var services = new ServiceCollection ();
        services.AddTransient<LoginView> ();
        services.AddTransient<LoginViewModel> ();

        return services.BuildServiceProvider ();
    }
}
