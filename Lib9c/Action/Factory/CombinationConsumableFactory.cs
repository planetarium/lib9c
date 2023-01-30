using System;
using System.Linq;
using Libplanet;
using Nekoyume.Action.Interface;
using Nekoyume.BlockChain.Policy;

namespace Nekoyume.Action.Factory
{
    public static class CombinationConsumableFactory
    {
        private static (Type type, string actionType)[] _tuples;

        private static (Type type, string actionType)[] Tuples =>
            _tuples ??= FactoryUtils.GetTuples<ICombinationConsumable>();

        public static ICombinationConsumable Create(long blockIndex, Address avatarAddress,
            int slotIndex, int recipeId)
        {
            if (blockIndex > BlockPolicySource.V100080ObsoleteIndex)
            {
                return new CombinationConsumable
                {
                    avatarAddress = avatarAddress,
                    slotIndex = slotIndex,
                    recipeId = recipeId
                };
            }

            return new CombinationConsumable7
            {
                AvatarAddress = avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId
            };
        }

        public static ICombinationConsumable Create(string actionType, Address avatarAddress,
            int slotIndex, int recipeId)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                throw new NotMatchFoundException(
                    typeof(ICombinationConsumable),
                    actionType);
            }

            var (type, _) = Tuples.FirstOrDefault(tuple => tuple.actionType == actionType);
            if (type is null)
            {
                throw new NotMatchFoundException(
                    typeof(ICombinationConsumable),
                    actionType);
            }

            var action = Activator.CreateInstance(type) as ICombinationConsumable;
            if (action is null)
            {
                throw new NotMatchFoundException(
                    typeof(ICombinationConsumable),
                    actionType);
            }

            switch (action)
            {
                case CombinationConsumable cc:
                    cc.avatarAddress = avatarAddress;
                    cc.slotIndex = slotIndex;
                    cc.recipeId = recipeId;
                    return cc;
                case CombinationConsumable7 cc7:
                    cc7.AvatarAddress = avatarAddress;
                    cc7.slotIndex = slotIndex;
                    cc7.recipeId = recipeId;
                    return cc7;
                case CombinationConsumable6 cc6:
                    cc6.AvatarAddress = avatarAddress;
                    cc6.slotIndex = slotIndex;
                    cc6.recipeId = recipeId;
                    return cc6;
                case CombinationConsumable5 cc5:
                    cc5.AvatarAddress = avatarAddress;
                    cc5.slotIndex = slotIndex;
                    cc5.recipeId = recipeId;
                    return cc5;
                case CombinationConsumable4 cc4:
                    cc4.AvatarAddress = avatarAddress;
                    cc4.slotIndex = slotIndex;
                    cc4.recipeId = recipeId;
                    return cc4;
                case CombinationConsumable3 cc3:
                    cc3.AvatarAddress = avatarAddress;
                    cc3.slotIndex = slotIndex;
                    cc3.recipeId = recipeId;
                    return cc3;
                case CombinationConsumable2 cc2:
                    cc2.AvatarAddress = avatarAddress;
                    cc2.slotIndex = slotIndex;
                    cc2.recipeId = recipeId;
                    return cc2;
                case CombinationConsumable0 cc0:
                    cc0.AvatarAddress = avatarAddress;
                    cc0.slotIndex = slotIndex;
                    cc0.recipeId = recipeId;
                    return cc0;
                default:
                    throw new NotMatchFoundException(
                        $"{actionType} is not supported.",
                        new NotImplementedException());
            }
        }
    }
}
