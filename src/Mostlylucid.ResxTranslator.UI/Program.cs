using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlyucid.LlmBackend.DependencyInjection;
using Mostlylucid.ResxTranslator.Core.Configuration;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Services;
using Mostlylucid.ResxTranslator.UI.Forms;
using Serilog;

namespace Mostlylucid.ResxTranslator.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "translator-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        // Build service provider
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add LLM Backend services
        services.AddLlmBackend(configuration);

        // Add Translation services
        services.Configure<TranslationSettings>(
            configuration.GetSection(TranslationSettings.SectionName));

        services.AddSingleton<ResxParser>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<IResxTranslator, ResxTranslator>();

        // Add UI forms
        services.AddTransient<MainForm>();
        services.AddTransient<SettingsForm>();

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Run the application
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show(
                $"Failed to start application:\n{ex.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
