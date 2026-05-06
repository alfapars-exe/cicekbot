using Metin2Bot.Application.Interfaces;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ForegroundWindowCoordinator
    {
        private readonly IWindowService _windowService;

        private const int PollDelayMs = 50;
        private const int TimeoutMs = 600;
        private const int SettleMs = 100;
        private const int FailureDelayMs = 500;

        public ForegroundWindowCoordinator(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public async Task<bool> EnsureReadyAsync(
            int clientNo,
            string displayName,
            IntPtr handle,
            Action<string> emitLog,
            CancellationToken token)
        {
            _windowService.BringToFront(handle);

            int waitedMs = 0;
            while (!_windowService.IsForeground(handle) && waitedMs < TimeoutMs)
            {
                if (!await DelayAsync(PollDelayMs, token)) return false;
                waitedMs += PollDelayMs;
            }

            if (!_windowService.IsForeground(handle))
            {
                emitLog($"Client{clientNo} ({displayName}): pencere öne getirilemedi, atlandı.");
                await DelayAsync(FailureDelayMs, token);
                return false;
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
