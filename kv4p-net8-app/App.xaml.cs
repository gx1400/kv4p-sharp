using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using kv4p_net8_app.Models;
using kv4p_net8_app.ViewModels;
using kv4p_net8_app.Views;
using kv4p_net8_lib.Interface;
using kv4p_net8_lib.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace kv4p_net8_app
{
    public partial class App : Application
    {
        public static IServiceProvider Provider;

        private string logPath;

        /// <summary>
        /// Interaction logic for App.xaml
        /// </summary>
        public App()
        {
            logPath = Path.Combine(Path.GetTempPath(), "kv4p-net8", "Logs");
            IServiceProvider serviceProvider = ConfigureServices();
            Provider = serviceProvider;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            IServiceProvider serviceProvider = ConfigureServices();
            Provider = serviceProvider;

            Window window = new MainView();
            window.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            window.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.RollingFile(new JsonFormatter(), logPath)
                .Enrich.WithProperty("SessionId", new Guid())
                .Enrich.WithProperty("Version", Assembly.GetEntryAssembly().GetName().Version)
                .CreateLogger();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, true));
            services.AddSingleton<ISerialPort, SerialPortWrapper>();

            // Models
            services.AddSingleton<RadioModel>();

            // ViewModels
            services.AddSingleton<MainViewModel, MainViewModel>();
            return services.BuildServiceProvider();
        }
    }
}
