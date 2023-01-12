using System.Collections.Generic;
using Lib9c.Model.Faucet;
using Libplanet;

namespace Lib9c.DevExtensions.Action.Interface
{
    public interface IFaucetRune
    {
        Address AvatarAddress { get; set; }
        List<FaucetRuneInfo> FaucetRuneInfos { get; set; }
    }
}
