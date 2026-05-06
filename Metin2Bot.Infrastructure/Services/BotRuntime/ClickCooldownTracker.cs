using Metin2Bot.Domain.Models;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ClickCooldownTracker
    {
        private readonly List<ClickRecord> _recentClicks = new();

        private const int CooldownDistance = 35;
        private const double CooldownSeconds = 6.0;

        public void Clear() => _recentClicks.Clear();

        public bool IsCoolingDown(Guid clientId, Guid productId, Location location, DateTime now)
        {
            _recentClicks.RemoveAll(click => (now - click.Time).TotalSeconds > CooldownSeconds);

            return _recentClicks.Any(click =>
                click.ClientId == clientId &&
                click.ProductId == productId &&
                Math.Abs(location.X - click.X) < CooldownDistance &&
                Math.Abs(location.Y - click.Y) < CooldownDistance);
        }

        public void Remember(Guid clientId, Guid productId, Location location, DateTime now)
        {
            _recentClicks.Add(new ClickRecord(clientId, productId, location.X, location.Y, now));
        }

        private sealed record ClickRecord(Guid ClientId, Guid ProductId, int X, int Y, DateTime Time);
    }
}
