namespace Metin2Bot.Infrastructure.Services.Input
{
    internal static class Gaussian
    {
        public static double Sample(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        public static double Around(Random rng, double mean, double sigma)
        {
            return mean + sigma * Sample(rng);
        }

        public static int IntAround(Random rng, double mean, double sigma, int min, int max)
        {
            return (int)Math.Clamp(Math.Round(Around(rng, mean, sigma)), min, max);
        }

        public static int Delta(Random rng, double sigma, int absLimit)
        {
            double v = sigma * Sample(rng);
            return (int)Math.Clamp(Math.Round(v), -absLimit, absLimit);
        }
    }
}
