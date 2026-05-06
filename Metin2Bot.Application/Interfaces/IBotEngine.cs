using Metin2Bot.Domain.Models;

namespace Metin2Bot.Application.Interfaces
{
    public interface IBotEngine
    {
        bool IsRunning { get; }
        event EventHandler<bool>? RunningStateChanged;
        event EventHandler<string>? LogEmitted;

        void Start(BotConfiguration config);
        void Stop();
    }
}
