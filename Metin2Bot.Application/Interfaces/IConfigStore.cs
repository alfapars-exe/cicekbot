using Metin2Bot.Domain.Models;

namespace Metin2Bot.Application.Interfaces
{
    public interface IConfigStore
    {
        BotConfiguration Load();
        void Save(BotConfiguration configuration);
        string GetTemplatesDirectory();
        string GetClientTemplatesDirectory(Guid clientId);
        void DeleteTemplate(string imagePath);
    }
}
