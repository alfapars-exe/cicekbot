namespace Metin2Bot.Application.Interfaces
{
    public interface IHotkeyService : IDisposable
    {
        /// <summary>
        /// DEL tuşunu host pencerenin handle'ı altında global hotkey olarak kaydeder.
        /// Bot başlat/durdur toggle eylemini tetikler.
        /// </summary>
        /// <param name="windowHandle">Host pencerenin HWND'i (RegisterHotKey hedefi).</param>
        /// <param name="onPressed">Hotkey basıldığında çağrılır (UI thread).</param>
        /// <returns>Kayıt başarılıysa true.</returns>
        bool Register(IntPtr windowHandle, Action onPressed);

        void Unregister();
    }
}
