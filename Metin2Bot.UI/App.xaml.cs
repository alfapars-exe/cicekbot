using Metin2Bot.Application.Interfaces;
using Metin2Bot.Infrastructure.Services;
using Metin2Bot.UI.Services;
using Metin2Bot.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Metin2Bot.UI
{
    public partial class App : System.Windows.Application
    {
        public IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Yakalanmamış exception'ları logla — sessiz çökme yerine kullanıcıya göster
            DispatcherUnhandledException += (s, args) =>
            {
                var msg = $"Beklenmeyen hata:\n\n{args.Exception.GetType().Name}: {args.Exception.Message}\n\n{args.Exception.StackTrace}";
                MessageBox.Show(msg, "Metin2Bot - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"Kritik hata:\n\n{ex.Message}\n\n{ex.StackTrace}",
                        "Metin2Bot - Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                args.SetObserved();
                MessageBox.Show($"Task hatası:\n\n{args.Exception.Message}",
                    "Metin2Bot - Task Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            };

            var services = new ServiceCollection();

            // Infrastructure services
            services.AddSingleton<IConfigStore, JsonConfigStore>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IVisionService, VisionService>();
            services.AddSingleton<IInputService, InputService>();
            services.AddSingleton<IBotEngine, BotEngine>();

            // UI services
            services.AddSingleton<ISnipService, SnipService>();
            services.AddSingleton<IHotkeyService, HotkeyService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();

            // Window
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            var main = Services.GetRequiredService<MainWindow>();
            main.Show();
        }
    }
}
