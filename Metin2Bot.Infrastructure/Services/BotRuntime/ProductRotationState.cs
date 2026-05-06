namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class ProductRotationState
    {
        private readonly Dictionary<Guid, int> _nextIndexes = new();

        public void Clear() => _nextIndexes.Clear();

        public int GetStartIndex(Guid clientId, int productCount)
        {
            if (productCount <= 0) return 0;

            return _nextIndexes.TryGetValue(clientId, out int savedIndex)
                ? Math.Clamp(savedIndex, 0, productCount - 1)
                : 0;
        }

        public void MarkClicked(Guid clientId, int clickedIndex, int productCount)
        {
            if (productCount <= 0) return;

            _nextIndexes[clientId] = (clickedIndex + 1) % productCount;
        }
    }
}
