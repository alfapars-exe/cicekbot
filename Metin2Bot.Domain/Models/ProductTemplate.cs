namespace Metin2Bot.Domain.Models
{
    public class ProductTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public Region SourceRegion { get; set; }
    }
}
