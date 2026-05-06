namespace Metin2Bot.Domain.Models
{
    public class BotSettings
    {
        public int ClientSwitchDelayMs { get; set; } = 1000;
        public double MatchThreshold { get; set; } = 0.45;

        /// <summary>
        /// Her client'a geçerken pencereyi öne getir (görsel takip).
        /// UYARI: Açıldığında bazı oyunlar DirectInput moduna geçip tıklamaları drop edebilir.
        /// </summary>
        public bool BringWindowToFront { get; set; } = false;
    }
}
