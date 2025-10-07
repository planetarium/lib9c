using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class RelationshipModule
    {
        public static int GetRelationship(this IWorldState state, Address avatarAddress)
        {
            var account = state.GetAccountState(Addresses.Relationship);
            return account.GetState(avatarAddress) is Integer p ? p : 0;
        }

        public static IWorld SetRelationship(this IWorld world, Address avatarAddress,
            int relationship)
        {
            var account = world.GetAccount(Addresses.Relationship);
            account = account.SetState(avatarAddress, (Integer)relationship);
            return world.SetAccount(Addresses.Relationship, account);
        }
    }
}
