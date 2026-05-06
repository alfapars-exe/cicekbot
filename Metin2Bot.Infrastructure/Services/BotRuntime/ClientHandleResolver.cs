using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ClientHandleResolver
    {
        private readonly IWindowService _windowService;

        public ClientHandleResolver(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public Dictionary<Guid, IntPtr> Resolve(List<ClientConfig> clients)
        {
            var resolved = new Dictionary<Guid, IntPtr>();
            var used = new HashSet<IntPtr>();

            foreach (var client in clients)
            {
                if (IsReusable(client.RuntimeHandle, used))
                {
                    resolved[client.Id] = client.RuntimeHandle;
                    used.Add(client.RuntimeHandle);
                }
            }

            foreach (var client in clients)
            {
                if (resolved.ContainsKey(client.Id)) continue;

                var handle = FindAvailableHandle(client.WindowTitle, used);
                if (handle == IntPtr.Zero) continue;

                resolved[client.Id] = handle;
                used.Add(handle);
                client.RuntimeHandle = handle;
            }

            return resolved;
        }

        private bool IsReusable(IntPtr handle, HashSet<IntPtr> used)
        {
            return handle != IntPtr.Zero
                && _windowService.IsValidWindow(handle)
                && !used.Contains(handle);
        }

        private IntPtr FindAvailableHandle(string windowTitle, HashSet<IntPtr> used)
        {
            return _windowService.FindAllByTitle(windowTitle)
                .Select(window => window.Handle)
                .FirstOrDefault(handle => !used.Contains(handle));
        }
    }
}
