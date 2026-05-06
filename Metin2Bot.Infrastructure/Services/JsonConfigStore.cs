using System.Text.Json;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;

namespace Metin2Bot.Infrastructure.Services
{
    public class JsonConfigStore : IConfigStore
    {
        private readonly string _rootDir;
        private readonly string _configPath;
        private readonly string _templatesDir;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonConfigStore()
        {
            _rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Metin2Bot");
            _configPath = Path.Combine(_rootDir, "config.json");
            _templatesDir = Path.Combine(_rootDir, "templates");

            Directory.CreateDirectory(_rootDir);
            Directory.CreateDirectory(_templatesDir);
        }

        public BotConfiguration Load()
        {
            if (!File.Exists(_configPath))
                return new BotConfiguration();

            try
            {
                string json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<BotConfiguration>(json, _jsonOptions);
                return config ?? new BotConfiguration();
            }
            catch (JsonException)
            {
                // Bozuk config — yedeğe al ve yeniden başla
                string backup = _configPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Move(_configPath, backup);
                return new BotConfiguration();
            }
        }

        public void Save(BotConfiguration configuration)
        {
            string json = JsonSerializer.Serialize(configuration, _jsonOptions);
            string tempPath = _configPath + ".tmp";

            File.WriteAllText(tempPath, json);

            // Atomic replace: yazım sırasında crash olursa eski config sağlam kalır
            if (File.Exists(_configPath))
                File.Replace(tempPath, _configPath, null);
            else
                File.Move(tempPath, _configPath);
        }

        public string GetTemplatesDirectory() => _templatesDir;

        public string GetClientTemplatesDirectory(Guid clientId)
        {
            string dir = Path.Combine(_templatesDir, clientId.ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }

        public void DeleteTemplate(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            string fullPath = Path.IsPathRooted(imagePath)
                ? imagePath
                : Path.Combine(_rootDir, imagePath);

            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* dosya kilitli olabilir, sessiz geç */ }
            }
        }
    }
}
