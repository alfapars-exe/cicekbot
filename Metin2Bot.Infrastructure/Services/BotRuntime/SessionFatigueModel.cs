using Metin2Bot.Infrastructure.Services.Input;

namespace Metin2Bot.Infrastructure.Services.BotRuntime
{
    internal sealed class SessionFatigueModel
    {
        private readonly Random _rng = new();
        private int _clicksSinceThink;
        private int _nextThinkAt;
        private DateTime _nextBreakAt;

        public SessionFatigueModel()
        {
            ScheduleNextThink();
            ScheduleNextBreak();
        }

        public TimeSpan? OnBeforeClick()
        {
            if (DateTime.UtcNow >= _nextBreakAt)
            {
                ScheduleNextBreak();
                _clicksSinceThink = 0;
                int breakMs = _rng.Next(5000, 15001);
                return TimeSpan.FromMilliseconds(breakMs);
            }

            _clicksSinceThink++;
            if (_clicksSinceThink >= _nextThinkAt)
            {
                _clicksSinceThink = 0;
                ScheduleNextThink();
                int thinkMs = Gaussian.IntAround(_rng, 1800, 500, 1000, 3000);
                return TimeSpan.FromMilliseconds(thinkMs);
            }

            return null;
        }

        public void Reset()
        {
            _clicksSinceThink = 0;
            ScheduleNextThink();
            ScheduleNextBreak();
        }

        private void ScheduleNextThink()
        {
            _nextThinkAt = _rng.Next(4, 10);
        }

        private void ScheduleNextBreak()
        {
            int minutes = _rng.Next(25, 36);
            _nextBreakAt = DateTime.UtcNow.AddMinutes(minutes);
        }
    }
}
