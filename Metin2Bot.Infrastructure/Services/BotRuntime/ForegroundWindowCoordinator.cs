using Metin2Bot.Application.Interfaces;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ForegroundWindowCoordinator
    {
        private readonly IWindowService _windowService;

        private const int PollDelayMs = 50;
        private const int TimeoutMs = 400;
        private const int SettleMs = 100;

        public ForegroundWindowCoordinator(IWindowService windowService)
        {
            _windowService = windowService;
        }

        /// <summary>
        /// Pencereyi öne getirmeyi dener. Foreground olamasa bile bot devam eder —
        /// PostMessage tabanlı tıklama pencere arkadayken de çalışır, bu yüzden foreground
        /// "best effort" olarak ele alınır, blocking değil.
        /// </summary>
        public async Task<bool> EnsureReadyAsync(
            int clientNo,
            string displayName,
            IntPtr handle,
            Action<string> emitLog,
            CancellationToken token)
        {
            _windowService.BringToFront(handle);

            // Foreground olmasını kısa süre bekle, ama olmazsa da pes etme — yine click dene
            int waitedMs = 0;
            while (!_windowService.IsForeground(handle) && waitedMs < TimeoutMs)
            {
                if (!await DelayAsync(PollDelayMs, token)) return false;
                waitedMs += PollDelayMs;
            }

            // Foreground olamadıysa uyarı log'la ama TRUE dön — tıklamayı yine deneyelim
            if (!_windowService.IsForeground(handle))
            {
                emitLog($"Client{clientNo} ({displayName}): pencere öne getirilemedi, arka planda denenecek.");
            }

            // Render settle — pencere öne geldi veya gelemedi, her durumda kısa bekleme yararlı
            return await DelayAsync(SettleMs, token);
        }

        private static async Task<bool> DelayAsync(int milliseconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(milliseconds, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
