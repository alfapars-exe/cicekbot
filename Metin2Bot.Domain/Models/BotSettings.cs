namespace Metin2Bot.Domain.Models
{
    public class BotSettings
    {
        public int ClientSwitchDelayMs { get; set; } = 1000;
        public double MatchThreshold { get; set; } = 0.45;

    }
}
