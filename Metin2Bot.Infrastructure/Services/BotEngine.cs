using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;

namespace Metin2Bot.Infrastructure.Services
{
    /// <summary>
    /// Sıralı multi-client bot loop'u.
    /// Her tur başında: aynı title'a sahip pencerelere unique HWND ata (used-set ile).
    /// Sonra: her client sırayla → pencereyi öne getir → tara → eşleşme varsa tek tıklama
    ///        → ClientSwitchDelayMs bekleme.
    /// </summary>
    public class BotEngine : IBotEngine
    {
        private readonly IWindowService _windowService;
        private readonly IVisionService _visionService;
        private readonly IInputService _inputService;

        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private static readonly Random _rng = new();
        private int _clicksSinceBreak = 0;

        // Anti-spam: son tıklanan koordinatlar (Python'daki akilli_tiklama_uygun_mu mantığı)
        private readonly List<(int x, int y, DateTime time)> _recentClicks = new();
        private const int CooldownDistance = 35;
        private const double CooldownSeconds = 6.0;

        // Stuck detection: her client için aktif tracking (Python'daki duzeltme_gerekli_mi)
        private readonly Dictionary<Guid, ProductTrackingState> _tracking = new();
        private const int SameLocationThreshold = 7;
        private const double StuckTimeoutSeconds = 4.0;

        private class ProductTrackingState
        {
            public string ProductName = "";
            public int LastX;
            public int LastY;
            public DateTime FirstSeen;
            public bool CorrectionApplied;
        }

        public bool IsRunning { get; private set; }
        public event EventHandler<bool>? RunningStateChanged;
        public event EventHandler<string>? LogEmitted;

        public BotEngine(
            IWindowService windowService,
            IVisionService visionService,
            IInputService inputService)
        {
            _windowService = windowService;
            _visionService = visionService;
            _inputService = inputService;
        }

        public void Start(BotConfiguration config)
        {
            if (IsRunning) return;

            if (config.Clients.Count == 0)
            {
                EmitLog("Bot başlatılamadı: Hiç client kayıtlı değil.");
                return;
            }

            var clientsSnapshot = config.Clients.Select(c => new ClientConfig
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                WindowTitle = c.WindowTitle,
                Products = c.Products.ToList(),
                RuntimeHandle = c.RuntimeHandle
            }).ToList();

            int delayMs = Math.Max(50, config.Settings.ClientSwitchDelayMs);
            double threshold = Math.Clamp(config.Settings.MatchThreshold, 0.1, 0.99);
            bool bringToFront = config.Settings.BringWindowToFront;

            // Yeni run başlıyor — geçmiş tracking state'i temizle
            _recentClicks.Clear();
            _tracking.Clear();
            _clicksSinceBreak = 0;

            _cts = new CancellationTokenSource();
            IsRunning = true;
            RunningStateChanged?.Invoke(this, true);
            EmitLog($"Bot başladı. {clientsSnapshot.Count} client • geçiş {delayMs}ms • eşik {threshold:F2} • pencere öne: {(bringToFront ? "AÇIK" : "kapalı")}");

            _loopTask = Task.Run(() => LoopAsync(clientsSnapshot, delayMs, threshold, bringToFront, _cts.Token), _cts.Token);
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
            bool bringToFront,
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var resolved = ResolveHandles(clients);

                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        var client = clients[i];
                        int clientNo = i + 1;

                        if (!resolved.TryGetValue(client.Id, out var handle) || handle == IntPtr.Zero)
                        {
                            continue;
                        }

                        // Opsiyonel: pencereyi öne getir (kullanıcı UI'dan açtıysa)
                        if (bringToFront)
                        {
                            _windowService.BringToFront(handle);

                            // Foreground polling — random delay her iter'de
                            int waitedMs = 0;
                            while (!_windowService.IsForeground(handle) && waitedMs < 600)
                            {
                                int pollDelay = _rng.Next(30, 80);
                                try { await Task.Delay(pollDelay, token); }
                                catch (OperationCanceledException) { break; }
                                waitedMs += pollDelay;
                            }

                            if (!_windowService.IsForeground(handle))
                            {
                                EmitLog($"Client{clientNo} ({client.DisplayName}): pencere öne getirilemedi, atlandı.");
                                try { await Task.Delay(_rng.Next(800, 1300), token); }
                                catch (OperationCanceledException) { break; }
                                continue;
                            }

                            // Foreground render settle — random
                            try { await Task.Delay(_rng.Next(100, 250), token); }
                            catch (OperationCanceledException) { break; }
                        }

                        // Pre-click delay: random 250-450ms — sabit 300ms pattern detection'ını kırar
                        try { await Task.Delay(_rng.Next(250, 450), token); }
                        catch (OperationCanceledException) { break; }

                        bool clicked = ProcessClient(clientNo, client, handle, threshold);

                        // Post-click delay: random 400-700ms — anti-cheat pattern fingerprint
                        if (clicked)
                        {
                            try { await Task.Delay(_rng.Next(400, 700), token); }
                            catch (OperationCanceledException) { break; }

                            // İnsan-benzeri "düşünme molası" — her 4-9 tıklamada bir 1-3 saniye ek pause
                            _clicksSinceBreak++;
                            int breakThreshold = _rng.Next(4, 10);
                            if (_clicksSinceBreak >= breakThreshold)
                            {
                                _clicksSinceBreak = 0;
                                int breakMs = _rng.Next(1000, 3000);
                                EmitLog($"Düşünme molası: {breakMs}ms");
                                try { await Task.Delay(breakMs, token); }
                                catch (OperationCanceledException) { break; }
                            }
                        }

                        // Client switch delay: kullanıcının ayarladığı temel değer + ±35% random jitter
                        int jitteredSwitch = (int)(delayMs * (0.80 + _rng.NextDouble() * 0.40));
                        try { await Task.Delay(jitteredSwitch, token); }
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

        private Dictionary<Guid, IntPtr> ResolveHandles(List<ClientConfig> clients)
        {
            var resolved = new Dictionary<Guid, IntPtr>();
            var used = new HashSet<IntPtr>();

            foreach (var c in clients)
            {
                if (c.RuntimeHandle != IntPtr.Zero
                    && _windowService.IsValidWindow(c.RuntimeHandle)
                    && !used.Contains(c.RuntimeHandle))
                {
                    resolved[c.Id] = c.RuntimeHandle;
                    used.Add(c.RuntimeHandle);
                }
            }

            foreach (var c in clients)
            {
                if (resolved.ContainsKey(c.Id)) continue;

                var candidates = _windowService.FindAllByTitle(c.WindowTitle).Select(w => w.Handle);
                var available = candidates.FirstOrDefault(h => !used.Contains(h));
                if (available != IntPtr.Zero)
                {
                    resolved[c.Id] = available;
                    used.Add(available);
                    c.RuntimeHandle = available;
                }
            }

            return resolved;
        }

        /// <summary>Tıklama yapıldıysa true döner — caller post-click delay uygulayabilir.</summary>
        private bool ProcessClient(int clientNo, ClientConfig client, IntPtr handle, double threshold)
        {
            if (client.Products.Count == 0)
            {
                EmitLog($"Client{clientNo} ({client.DisplayName}): hiç ürün eklenmemiş.");
                return false;
            }
            if (_windowService.IsMinimized(handle))
            {
                EmitLog($"Client{clientNo} ({client.DisplayName}): pencere minimize, atlandı.");
                return false;
            }

            var rect = _windowService.GetWindowRect(handle);
            if (rect.Width <= 0 || rect.Height <= 0) return false;

            var region = new Region(rect.X, rect.Y, rect.Width, rect.Height);

            double bestConfidence = 0;
            string bestProductName = "";

            for (int p = 0; p < client.Products.Count; p++)
            {
                var product = client.Products[p];
                if (!File.Exists(product.ImagePath))
                {
                    EmitLog($"Client{clientNo} ({client.DisplayName}): {product.Name} dosyası eksik.");
                    continue;
                }

                var result = _visionService.FindTemplate(product.ImagePath, region, threshold);
                if (!result.HasValue) continue;

                if (result.Value.Confidence > bestConfidence)
                {
                    bestConfidence = result.Value.Confidence;
                    bestProductName = product.Name;
                }

                if (result.Value.Confidence < threshold) continue;

                int productNo = p + 1;
                var loc = result.Value.Location;
                var now = DateTime.Now;

                // Anti-spam: son 6 saniye içinde aynı koordinata ±35px yakın tıklandı mı?
                _recentClicks.RemoveAll(c => (now - c.time).TotalSeconds > CooldownSeconds);
                bool recentlyClicked = _recentClicks.Any(c =>
                    Math.Abs(loc.X - c.x) < CooldownDistance && Math.Abs(loc.Y - c.y) < CooldownDistance);
                if (recentlyClicked)
                {
                    // Aynı yere tıklamayı atla — karakter zaten oraya yürüyor
                    continue;
                }

                // Stuck detection: aynı çiçek 4+ saniyedir aynı yerde mi?
                bool needsCorrection = false;
                if (_tracking.TryGetValue(client.Id, out var prev) && prev.ProductName == product.Name)
                {
                    bool sameSpot = Math.Abs(prev.LastX - loc.X) < SameLocationThreshold
                                 && Math.Abs(prev.LastY - loc.Y) < SameLocationThreshold;
                    if (sameSpot)
                    {
                        if ((now - prev.FirstSeen).TotalSeconds >= StuckTimeoutSeconds && !prev.CorrectionApplied)
                        {
                            prev.CorrectionApplied = true;
                            needsCorrection = true;
                        }
                    }
                    else
                    {
                        // Pozisyon değişti, takip sıfırlansın
                        prev.LastX = loc.X;
                        prev.LastY = loc.Y;
                        prev.FirstSeen = now;
                        prev.CorrectionApplied = false;
                    }
                }
                else
                {
                    _tracking[client.Id] = new ProductTrackingState
                    {
                        ProductName = product.Name,
                        LastX = loc.X,
                        LastY = loc.Y,
                        FirstSeen = now,
                        CorrectionApplied = false
                    };
                }

                if (needsCorrection)
                {
                    int corrX = loc.X + 100;
                    int corrY = loc.Y + 100;
                    EmitLog($"Client{clientNo} ({client.DisplayName}), {productNo}. ürün takıldı — düzeltme tıklaması ({corrX},{corrY}) → tekrar ({loc.X},{loc.Y})");
                    _inputService.BackgroundClick(handle, corrX, corrY);
                    Thread.Sleep(_rng.Next(200, 350));
                    _inputService.BackgroundClick(handle, loc.X, loc.Y);
                }
                else
                {
                    EmitLog($"Client{clientNo} ({client.DisplayName}), {productNo}. ürün ({product.Name}) tıklandı → ({loc.X},{loc.Y}) [eşleşme {result.Value.Confidence:F2}]");
                    _inputService.BackgroundClick(handle, loc.X, loc.Y);
                }

                _recentClicks.Add((loc.X, loc.Y, now));
                return true;
            }

            // Hiç tıklanabilir eşleşme yok
            if (bestConfidence > 0 && bestConfidence < threshold)
            {
                EmitLog($"Client{clientNo} ({client.DisplayName}): eşleşme YOK (en iyi: {bestProductName} = {bestConfidence:F2}, eşik {threshold:F2}).");
            }
            else if (bestConfidence == 0)
            {
                EmitLog($"Client{clientNo} ({client.DisplayName}): hiçbir ürün ekran üzerinde tespit edilemedi.");
            }
            // bestConfidence >= threshold ama tüm eşleşmeler cooldown'da → log atma (sessiz atla)
            return false;
        }

        private void EmitLog(string message) => LogEmitted?.Invoke(this, message);
    }
}
