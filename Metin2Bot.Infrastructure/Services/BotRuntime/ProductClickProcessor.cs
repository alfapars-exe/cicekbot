using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using Region = Metin2Bot.Domain.Models.Region;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ProductClickProcessor
    {
        private readonly IWindowService _windowService;
        private readonly IVisionService _visionService;
        private readonly IInputService _inputService;
        private readonly SessionFatigueModel _fatigueModel;
        private readonly ProductRotationState _rotationState = new();
        private readonly ClickCooldownTracker _cooldownTracker = new();
        private readonly StuckCorrectionTracker _stuckTracker = new();

        private const int CorrectionOffset = 100;
        private const int CorrectionRetryDelayMs = 100;

        public ProductClickProcessor(
            IWindowService windowService,
            IVisionService visionService,
            IInputService inputService,
            SessionFatigueModel fatigueModel)
        {
            _windowService = windowService;
            _visionService = visionService;
            _inputService = inputService;
            _fatigueModel = fatigueModel;
        }

        public void Reset()
        {
            _rotationState.Clear();
            _cooldownTracker.Clear();
            _stuckTracker.Clear();
            _fatigueModel.Reset();
        }

        public bool Process(
            int clientNo,
            ClientConfig client,
            IntPtr handle,
            double threshold,
            Action<string> emitLog)
        {
            if (!CanProcessClient(clientNo, client, handle, emitLog, out var region))
            {
                return false;
            }

            var scanState = new ProductScanState();
            int startIndex = _rotationState.GetStartIndex(client.Id, client.Products.Count);

            for (int offset = 0; offset < client.Products.Count; offset++)
            {
                int productIndex = (startIndex + offset) % client.Products.Count;
                var product = client.Products[productIndex];

                if (!File.Exists(product.ImagePath))
                {
                    emitLog($"Client{clientNo} ({client.DisplayName}): {product.Name} dosyası eksik.");
                    continue;
                }

                var result = _visionService.FindTemplate(product.ImagePath, region, threshold);
                if (!result.HasValue) continue;

                scanState.TrackBest(product.Name, result.Value.Confidence);
                if (result.Value.Confidence < threshold) continue;

                if (TryClickProduct(clientNo, client, handle, product, productIndex, result.Value, scanState, emitLog))
                {
                    return true;
                }
            }

            scanState.EmitNoClickLog(clientNo, client.DisplayName, threshold, emitLog);
            return false;
        }

        private bool CanProcessClient(
            int clientNo,
            ClientConfig client,
            IntPtr handle,
            Action<string> emitLog,
            out Region region)
        {
            region = default;

            if (client.Products.Count == 0)
            {
                emitLog($"Client{clientNo} ({client.DisplayName}): hiç ürün eklenmemiş.");
                return false;
            }

            if (_windowService.IsMinimized(handle))
            {
                emitLog($"Client{clientNo} ({client.DisplayName}): pencere minimize, atlandı.");
                return false;
            }

            var rect = _windowService.GetWindowRect(handle);
            if (rect.Width <= 0 || rect.Height <= 0) return false;

            region = new Region(rect.X, rect.Y, rect.Width, rect.Height);
            return true;
        }

        private bool TryClickProduct(
            int clientNo,
            ClientConfig client,
            IntPtr handle,
            ProductTemplate product,
            int productIndex,
            MatchResult result,
            ProductScanState scanState,
            Action<string> emitLog)
        {
            int productNo = productIndex + 1;
            var location = result.Location;
            var now = DateTime.Now;

            if (_cooldownTracker.IsCoolingDown(client.Id, product.Id, location, now))
            {
                scanState.MarkCooldown(productNo, product.Name, location, result.Confidence);
                return false;
            }

            bool needsCorrection = _stuckTracker.NeedsCorrection(client.Id, product.Name, location, now);
            if (needsCorrection)
            {
                ClickWithCorrection(clientNo, client.DisplayName, productNo, handle, location, emitLog);
            }
            else
            {
                emitLog($"Client{clientNo} ({client.DisplayName}), {productNo}. ürün ({product.Name}) tıklandı -> ({location.X},{location.Y}) [eşleşme {result.Confidence:F2}]");
                ApplyFatigue(clientNo, client.DisplayName, emitLog);
                _inputService.BackgroundClick(handle, location.X, location.Y);
            }

            _cooldownTracker.Remember(client.Id, product.Id, location, now);
            _rotationState.MarkClicked(client.Id, productIndex, client.Products.Count);
            return true;
        }

        private void ClickWithCorrection(
            int clientNo,
            string displayName,
            int productNo,
            IntPtr handle,
            Location location,
            Action<string> emitLog)
        {
            int correctionX = location.X + CorrectionOffset;
            int correctionY = location.Y + CorrectionOffset;

            emitLog($"Client{clientNo} ({displayName}), {productNo}. ürün takıldı - düzeltme tıklaması ({correctionX},{correctionY}) -> tekrar ({location.X},{location.Y})");
            ApplyFatigue(clientNo, displayName, emitLog);
            _inputService.BackgroundClick(handle, correctionX, correctionY);
            Thread.Sleep(CorrectionRetryDelayMs);
            _inputService.BackgroundClick(handle, location.X, location.Y);
        }

        private void ApplyFatigue(int clientNo, string displayName, Action<string> emitLog)
        {
            var pause = _fatigueModel.OnBeforeClick();
            if (pause is null) return;

            int ms = (int)pause.Value.TotalMilliseconds;
            if (ms >= 4000)
            {
                emitLog($"Client{clientNo} ({displayName}): mola — {ms}ms.");
            }
            else if (ms >= 1000)
            {
                emitLog($"Client{clientNo} ({displayName}): kısa düşünme — {ms}ms.");
            }
            Thread.Sleep(ms);
        }

        private sealed class ProductScanState
        {
            private double _bestConfidence;
            private string _bestProductName = "";
            private CooldownSkip? _cooldownSkip;

            public void TrackBest(string productName, double confidence)
            {
                if (confidence <= _bestConfidence) return;

                _bestConfidence = confidence;
                _bestProductName = productName;
            }

            public void MarkCooldown(int productNo, string productName, Location location, double confidence)
            {
                _cooldownSkip = new CooldownSkip(productNo, productName, location, confidence);
            }

            public void EmitNoClickLog(
                int clientNo,
                string displayName,
                double threshold,
                Action<string> emitLog)
            {
                if (_cooldownSkip is not null)
                {
                    emitLog($"Client{clientNo} ({displayName}), {_cooldownSkip.ProductNo}. ürün ({_cooldownSkip.ProductName}) cooldown'da, atlandı -> ({_cooldownSkip.Location.X},{_cooldownSkip.Location.Y}) [eşleşme {_cooldownSkip.Confidence:F2}]");
                    return;
                }

                if (_bestConfidence > 0 && _bestConfidence < threshold)
                {
                    emitLog($"Client{clientNo} ({displayName}): eşleşme YOK (en iyi: {_bestProductName} = {_bestConfidence:F2}, eşik {threshold:F2}).");
                    return;
                }

                if (_bestConfidence == 0)
                {
                    emitLog($"Client{clientNo} ({displayName}): hiçbir ürün ekran üzerinde tespit edilemedi.");
                }
            }

            private sealed record CooldownSkip(
                int ProductNo,
                string ProductName,
                Location Location,
                double Confidence);
        }
    }
}
