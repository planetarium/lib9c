namespace Lib9c.Tests.Action
{
    using Libplanet.Action;

    internal class Random : System.Random, IRandom
    {
        internal Random(int seed)
            : base(seed)
        {
        }
    }
}
