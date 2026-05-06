using Metin2Bot.Domain.Models;

namespace Metin2Bot.Application.Interfaces
{
    public interface ISnipService
    {
        /// <summary>
        /// Belirtilen pencerenin üzerinde snip overlay açar, kullanıcının seçtiği alanı PNG olarak kaydeder.
        /// </summary>
        /// <returns>Yeni ProductTemplate (kullanıcı iptal ederse null).</returns>
        ProductTemplate? SnipFromWindow(IntPtr windowHandle, Guid clientId, string suggestedName);
    }
}
