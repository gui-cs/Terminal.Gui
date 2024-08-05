﻿using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;

namespace CommunityToolkitExample;

public static class Program
{
    public static IServiceProvider? Services { get; private set; }

    private static void Main (string [] args)
    {
        Services = ConfigureServices ();
        Application.Init ();
        Application.Run (Services.GetRequiredService<LoginView> ());
        Application.Top?.Dispose();
        Application.Shutdown ();
    }

    private static IServiceProvider ConfigureServices ()
    {
        var services = new ServiceCollection ();
        services.AddTransient<LoginView> ();
        services.AddTransient<LoginViewModel> ();
        return services.BuildServiceProvider ();
    }
}