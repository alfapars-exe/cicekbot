using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace Metin2Bot.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IConfigStore _configStore;
        private readonly IWindowService _windowService;
        private readonly IVisionService _visionService;
        private readonly IInputService _inputService;
        private readonly ISnipService _snipService;
        private readonly IBotEngine _botEngine;
        private readonly BotConfiguration _config;

        [ObservableProperty]
        private ObservableCollection<ClientViewModel> _clients = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddProductCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveClientCommand))]
        private ClientViewModel? _selectedClient;

        [ObservableProperty]
        private ObservableCollection<string> _logs = new();

        [ObservableProperty]
        private int _clientSwitchDelayMs;

        [ObservableProperty]
        private double _matchThreshold;

        [ObservableProperty]
        private bool _bringClientToFront;

        [ObservableProperty]
        private bool _isBotRunning;

        [ObservableProperty]
        private string _botStatus = "Beklemede (DEL ile başlat)";

        public MainViewModel(
            IConfigStore configStore,
            IWindowService windowService,
            IVisionService visionService,
            IInputService inputService,
            ISnipService snipService,
            IBotEngine botEngine)
        {
            _configStore = configStore;
            _windowService = windowService;
            _visionService = visionService;
            _inputService = inputService;
            _snipService = snipService;
            _botEngine = botEngine;

            _config = _configStore.Load();
            _clientSwitchDelayMs = _config.Settings.ClientSwitchDelayMs;
            _matchThreshold = _config.Settings.MatchThreshold;
            _bringClientToFront = _config.Settings.BringClientToFront;

            foreach (var clientModel in _config.Clients)
            {
                Clients.Add(new ClientViewModel(clientModel));
            }

            _botEngine.RunningStateChanged += OnBotStateChanged;
            _botEngine.LogEmitted += (_, msg) => AddLog(msg);

            AddLog($"Konfigürasyon yüklendi. {Clients.Count} client kayıtlı.");
        }

        private void OnBotStateChanged(object? sender, bool running)
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                IsBotRunning = running;
                BotStatus = running ? "Çalışıyor (DEL ile durdur)" : "Beklemede (DEL ile başlat)";
            });
        }

        partial void OnClientSwitchDelayMsChanged(int value)
        {
            _config.Settings.ClientSwitchDelayMs = value;
            PersistConfig();
        }

        partial void OnMatchThresholdChanged(double value)
        {
            _config.Settings.MatchThreshold = value;
            PersistConfig();
        }

        partial void OnBringClientToFrontChanged(bool value)
        {
            _config.Settings.BringClientToFront = value;
            PersistConfig();
        }

        [RelayCommand]
        private void AddClient()
        {
            // Halihazırda eklenmiş client'ların kullandığı HWND'leri exclude et
            var excludeHandles = _config.Clients
                .Select(c => c.RuntimeHandle)
                .Where(h => h != IntPtr.Zero && _windowService.IsValidWindow(h))
                .ToList();

            var pickerVm = new WindowPickerViewModel(_windowService, excludeHandles);
            var dialog = new Views.WindowPickerDialog
            {
                DataContext = pickerVm,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && pickerVm.SelectedWindow is not null)
            {
                var clientModel = new ClientConfig
                {
                    DisplayName = string.IsNullOrWhiteSpace(pickerVm.DisplayName)
                        ? pickerVm.SelectedWindow.Title
                        : pickerVm.DisplayName,
                    WindowTitle = pickerVm.SelectedWindow.Title,
                    RuntimeHandle = pickerVm.SelectedWindow.Handle
                };
                _config.Clients.Add(clientModel);

                var clientVm = new ClientViewModel(clientModel);
                Clients.Add(clientVm);
                SelectedClient = clientVm;
                PersistConfig();
                AddLog($"Client eklendi: {clientModel.DisplayName} (HWND={clientModel.RuntimeHandle.ToInt64():X})");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedClient))]
        private void RemoveClient()
        {
            var target = SelectedClient;
            if (target is null) return;

            try
            {
                var result = MessageBox.Show(
                    $"'{target.DisplayName}' client'ı ve tüm ürünleri silinecek. Emin misiniz?",
                    "Client Sil",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                // 1. Önce UI binding'lerini gevşet — SelectedClient null olunca
                //    sağ paneldeki SelectedClient.Products binding'i de düşer, böylece
                //    item silerken visual tree problemleri olmaz
                SelectedClient = null;

                // 2. Ürün PNG'lerini sil
                var imagesToDelete = target.Products.Select(p => p.ImagePath).ToList();
                foreach (var path in imagesToDelete)
                {
                    _configStore.DeleteTemplate(path);
                }

                // 3. Koleksiyonlardan çıkar
                _config.Clients.Remove(target.Model);
                Clients.Remove(target);

                AddLog($"Client silindi: {target.DisplayName}");
                PersistConfig();
            }
            catch (Exception ex)
            {
                AddLog($"Client silinirken hata: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddProduct))]
        private async Task AddProductAsync()
        {
            if (SelectedClient is null) return;

            var handle = ResolveClientHandle(SelectedClient.Model);
            if (handle == IntPtr.Zero)
            {
                AddLog($"Hata: '{SelectedClient.WindowTitle}' penceresi bulunamadı. Pencere açık ve görünür olmalı.");
                return;
            }
            if (_windowService.IsMinimized(handle))
            {
                AddLog("Hata: Hedef pencere minimize. Önce büyültün.");
                return;
            }

            // Ana pencereyi geçici olarak minimize et ki overlay'in altına gizlenmesin
            var mainWindow = System.Windows.Application.Current?.MainWindow;
            var prevState = mainWindow?.WindowState ?? WindowState.Normal;
            if (mainWindow is not null) mainWindow.WindowState = WindowState.Minimized;
            // Pencere render'ının yerleşmesi için kısa bir nefes
            await Task.Delay(200);

            ProductTemplate? template = null;
            try
            {
                string suggested = $"urun_{SelectedClient.Products.Count + 1}";
                template = _snipService.SnipFromWindow(handle, SelectedClient.Id, suggested);
            }
            catch (Exception ex)
            {
                AddLog($"Snip hatası: {ex.Message}");
            }
            finally
            {
                if (mainWindow is not null) mainWindow.WindowState = prevState;
            }

            if (template is null)
            {
                AddLog("Snip iptal edildi.");
                return;
            }

            SelectedClient.AddProduct(template);
            PersistConfig();
            AddLog($"Ürün eklendi: {template.Name} ({template.SourceRegion.Width}x{template.SourceRegion.Height}px)");
        }

        [RelayCommand]
        private void RemoveProduct(ProductViewModel? product)
        {
            if (product is null || SelectedClient is null) return;

            _configStore.DeleteTemplate(product.ImagePath);
            SelectedClient.RemoveProduct(product);
            AddLog($"Ürün silindi: {product.Name}");
            PersistConfig();
        }

        [RelayCommand]
        private void ToggleBot()
        {
            if (_botEngine.IsRunning)
            {
                _botEngine.Stop();
            }
            else
            {
                _botEngine.Start(_config);
            }
        }

        private bool HasSelectedClient() => SelectedClient is not null;

        private bool CanAddProduct()
        {
            if (SelectedClient is null) return false;
            var handle = ResolveClientHandle(SelectedClient.Model);
            return handle != IntPtr.Zero && !_windowService.IsMinimized(handle);
        }

        /// <summary>
        /// Client'ın RuntimeHandle'ı geçerliyse onu kullanır; değilse aynı title'a sahip,
        /// başka client'larca kullanılmamış bir pencere bulup atar. Bulamazsa Zero döner.
        /// </summary>
        private IntPtr ResolveClientHandle(ClientConfig client)
        {
            if (client.RuntimeHandle != IntPtr.Zero
                && _windowService.IsValidWindow(client.RuntimeHandle))
            {
                return client.RuntimeHandle;
            }

            var usedByOthers = _config.Clients
                .Where(c => c.Id != client.Id && c.RuntimeHandle != IntPtr.Zero)
                .Select(c => c.RuntimeHandle)
                .ToHashSet();

            var candidate = _windowService.FindAllByTitle(client.WindowTitle)
                .Select(w => w.Handle)
                .FirstOrDefault(h => !usedByOthers.Contains(h));

            if (candidate != IntPtr.Zero)
            {
                client.RuntimeHandle = candidate;
            }
            return candidate;
        }

        public void AddLog(string message)
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (Logs.Count > 200) Logs.RemoveAt(200);
            });
        }

        private void PersistConfig()
        {
            try
            {
                _configStore.Save(_config);
            }
            catch (Exception ex)
            {
                AddLog($"Konfigürasyon kaydedilemedi: {ex.Message}");
            }
        }
    }
}
