namespace Metin2Bot.Domain.Models
{
    public class BotSettings
    {
        public int ClientSwitchDelayMs { get; set; } = 1000;
        public double MatchThreshold { get; set; } = 0.45;

        /// <summary>
        /// Tıklama öncesi pencereyi öne getir. Multi-client'ta ardışık focus alternation
        /// bazı client'ların message queue'sunu drop edebilir; o durumda false yap, bot
        /// kbunall referansı gibi saf background tıklasın.
        /// </summary>
        public bool BringClientToFront { get; set; } = true;
    }
}
