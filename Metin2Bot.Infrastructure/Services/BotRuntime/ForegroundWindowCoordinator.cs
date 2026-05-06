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
        /// Pencereyi öne getirmeyi dener. Foreground sağlanamasa bile bot devam eder —
        /// PostMessage tabanlı tıklama pencere arkadayken de çalışır, foreground best-effort.
        /// </summary>
        public async Task<bool> EnsureReadyAsync(
            int clientNo,
            string displayName,
            IntPtr handle,
            bool bringToFront,
            Action<string> emitLog,
            CancellationToken token)
        {
            if (!bringToFront)
            {
                // Saf background mode — kbunall referansı gibi, focus thrashing yapmadan tıkla.
                return await DelayAsync(SettleMs, token);
            }

            _windowService.BringToFront(handle);

            int waitedMs = 0;
            while (!_windowService.IsForeground(handle) && waitedMs < TimeoutMs)
            {
                if (!await DelayAsync(PollDelayMs, token)) return false;
                waitedMs += PollDelayMs;
            }

            if (!_windowService.IsForeground(handle))
            {
                emitLog($"Client{clientNo} ({displayName}): pencere öne getirilemedi, arka planda denenecek.");
            }

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
