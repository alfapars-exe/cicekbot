using Metin2Bot.Domain.Models;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class StuckCorrectionTracker
    {
        private readonly Dictionary<Guid, ProductTrackingState> _tracking = new();

        private const int SameLocationThreshold = 7;
        private const double StuckTimeoutSeconds = 4.0;

        public void Clear() => _tracking.Clear();

        public bool NeedsCorrection(Guid clientId, string productName, Location location, DateTime now)
        {
            if (_tracking.TryGetValue(clientId, out var previous) && previous.ProductName == productName)
            {
                return UpdateExisting(previous, location, now);
            }

            _tracking[clientId] = ProductTrackingState.Create(productName, location, now);
            return false;
        }

        private static bool UpdateExisting(ProductTrackingState previous, Location location, DateTime now)
        {
            if (!previous.IsSameLocation(location))
            {
                previous.MoveTo(location, now);
                return false;
            }

            if ((now - previous.FirstSeen).TotalSeconds < StuckTimeoutSeconds || previous.CorrectionApplied)
            {
                return false;
            }

            previous.CorrectionApplied = true;
            return true;
        }

        private sealed class ProductTrackingState
        {
            public string ProductName { get; private init; } = "";
            public int LastX { get; private set; }
            public int LastY { get; private set; }
            public DateTime FirstSeen { get; private set; }
            public bool CorrectionApplied { get; set; }

            public static ProductTrackingState Create(string productName, Location location, DateTime now)
            {
                return new ProductTrackingState
                {
                    ProductName = productName,
                    LastX = location.X,
                    LastY = location.Y,
                    FirstSeen = now,
                    CorrectionApplied = false
                };
            }

            public bool IsSameLocation(Location location)
            {
                return Math.Abs(LastX - location.X) < SameLocationThreshold
                    && Math.Abs(LastY - location.Y) < SameLocationThreshold;
            }

            public void MoveTo(Location location, DateTime now)
            {
                LastX = location.X;
                LastY = location.Y;
                FirstSeen = now;
                CorrectionApplied = false;
            }
        }
    }
}
