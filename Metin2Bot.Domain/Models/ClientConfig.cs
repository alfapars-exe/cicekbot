using System.Text.Json.Serialization;

namespace Metin2Bot.Domain.Models
{
    public class ClientConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DisplayName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public List<ProductTemplate> Products { get; set; } = new();

        /// <summary>
        /// Bu oturumda bu client'a atanmış pencere handle'ı.
        /// JSON'a yazılmaz — uygulama yeniden açıldığında title'a göre yeniden çözülür.
        /// </summary>
        [JsonIgnore]
        public IntPtr RuntimeHandle { get; set; } = IntPtr.Zero;
    }
}
