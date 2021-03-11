namespace Lib9c.Tests.Action
{
    using Libplanet.Action;

    public class TestRandom : IRandom
    {
        private readonly System.Random _random = new System.Random(Seed: 0);

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            _random.NextBytes(buffer);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
