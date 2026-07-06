using ROMVaultWeb.Components;
using ROMVaultWeb.Services;
using RomVaultCore;

namespace ROMVaultWeb;

public class Program
{
    public static string strVersion;

    public static void Main(string[] args)
    {
        var asmVersion = typeof(Program).Assembly.GetName().Version ?? new Version(3, 7, 5);
        strVersion = $"{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";

        // Working directory must be /config in the container: RomVault resolves
        // config/RomVault3cfg.xml, the scan cache and DatRoot relative to CWD.
        Settings.checkdirs();
        Settings.rvSettings = new Settings();
        Settings.rvSettings = Settings.SetDefaults(out string errorReadingSettings);

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddSingleton<RvApp>();

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
            builder.WebHost.UseUrls("http://0.0.0.0:3000");

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapGet("/health", () => Results.Ok("ok"));

        // Kick off the DB load (the splash-screen equivalent) in the background;
        // the UI shows the loading state until it completes.
        var rv = app.Services.GetRequiredService<RvApp>();
        rv.SettingsLoadError = errorReadingSettings;
        rv.BeginStartup();

        app.Run();
    }
}
