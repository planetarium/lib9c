namespace Nekoyume.Action.DPoS.Exception
{
    public class NullNativeTokensException : System.Exception
    {
        public NullNativeTokensException()
            : base($"At least one native token have to be set on block policy")
        {
        }
    }
}
