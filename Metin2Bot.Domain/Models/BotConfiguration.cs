namespace Metin2Bot.Domain.Models
{
    public class BotConfiguration
    {
        public BotSettings Settings { get; set; } = new();
        public List<ClientConfig> Clients { get; set; } = new();
    }
}
