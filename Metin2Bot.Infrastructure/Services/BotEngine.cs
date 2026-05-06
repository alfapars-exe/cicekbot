using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using Metin2Bot.Infrastructure.Services.BotRuntime;

namespace Metin2Bot.Infrastructure.Services
{
    public class BotEngine : IBotEngine
    {
        private readonly ClientHandleResolver _handleResolver;
        private readonly ForegroundWindowCoordinator _foregroundCoordinator;
        private readonly ProductClickProcessor _productClickProcessor;

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public bool IsRunning { get; private set; }
        public event EventHandler<bool>? RunningStateChanged;
        public event EventHandler<string>? LogEmitted;

        public BotEngine(
            IWindowService windowService,
            IVisionService visionService,
            IInputService inputService)
        {
            _handleResolver = new ClientHandleResolver(windowService);
            _foregroundCoordinator = new ForegroundWindowCoordinator(windowService);
            _productClickProcessor = new ProductClickProcessor(windowService, visionService, inputService);
        }

        public void Start(BotConfiguration config)
        {
            if (IsRunning) return;

            if (config.Clients.Count == 0)
            {
                EmitLog("Bot başlatılamadı: Hiç client kayıtlı değil.");
                return;
            }

            var clientsSnapshot = CreateClientSnapshot(config.Clients);
            int delayMs = Math.Max(50, config.Settings.ClientSwitchDelayMs);
            double threshold = Math.Clamp(config.Settings.MatchThreshold, 0.1, 0.99);

            _productClickProcessor.Reset();

            _cts = new CancellationTokenSource();
            IsRunning = true;
            RunningStateChanged?.Invoke(this, true);
            EmitLog($"Bot başladı. {clientsSnapshot.Count} client • geçiş {delayMs}ms • eşik {threshold:F2} • tıklama modu: aktif pencere");

            _loopTask = Task.Run(() => LoopAsync(clientsSnapshot, delayMs, threshold, _cts.Token), _cts.Token);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            EmitLog("Durdurma isteği alındı...");
            try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        }

        private async Task LoopAsync(
            List<ClientConfig> clients,
            int delayMs,
            double threshold,
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var resolved = _handleResolver.Resolve(clients);

                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        var client = clients[i];
                        int clientNo = i + 1;

                        if (!resolved.TryGetValue(client.Id, out var handle) || handle == IntPtr.Zero)
                        {
                            continue;
                        }

                        if (!await _foregroundCoordinator.EnsureReadyAsync(clientNo, client.DisplayName, handle, EmitLog, token))
                        {
                            continue;
                        }

                        _productClickProcessor.Process(clientNo, client, handle, threshold, EmitLog);

                        // NOT: Click sonrası release zaten InputService.BackgroundClick içinde
                        // (ReleaseAfterClick: 3-kanal LEFTUP + 15ms settle) yapılıyor. Burada
                        // ekstra ReleaseMouseButtons çağrısı eklemek tıklama detection'ını boğuyordu.

                        try { await Task.Delay(delayMs, token); }
                        catch (OperationCanceledException) { break; }
                    }
                }
            }
            catch (Exception ex)
            {
                EmitLog($"Bot loop hatası: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
                RunningStateChanged?.Invoke(this, false);
                EmitLog("Bot durduruldu.");
            }
        }

        private static List<ClientConfig> CreateClientSnapshot(IEnumerable<ClientConfig> clients)
        {
            return clients.Select(c => new ClientConfig
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                WindowTitle = c.WindowTitle,
                Products = c.Products.ToList(),
                RuntimeHandle = c.RuntimeHandle
            }).ToList();
        }

        private void EmitLog(string message) => LogEmitted?.Invoke(this, message);
    }
}
