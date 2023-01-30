using System;
using System.Linq;
using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public static class RapidCombinationFactory
    {
        private static (Type type, string actionType)[] _tuples;

        private static (Type type, string actionType)[] Tuples =>
            _tuples ??= FactoryUtils.GetTuples<IRapidCombination>();

        public static IRapidCombination Create(
            long blockIndex,
            Address avatarAddr,
            int slotIndex)
        {
            if (blockIndex < 0L)
            {
                throw new NotMatchFoundException(
                    typeof(IRapidCombination),
                    blockIndex);
            }

            // NOTE: RapidCombination0.ObsoleteIndex equals to RapidCombination2.ObsoleteIndex.
            // NOTE: RapidCombination2.ObsoleteIndex equals to RapidCombination3.ObsoleteIndex.
            // NOTE: RapidCombination3.ObsoleteIndex equals to RapidCombination4.ObsoleteIndex.
            if (blockIndex < RapidCombination4.ObsoleteIndex)
            {
                return new RapidCombination4
                {
                    avatarAddress = avatarAddr,
                    slotIndex = slotIndex,
                };
            }

            if (blockIndex < RapidCombination5.ObsoleteIndex)
            {
                return new RapidCombination5
                {
                    avatarAddress = avatarAddr,
                    slotIndex = slotIndex,
                };
            }

            // NOTE: RapidCombination6.ObsoleteIndex equals to RapidCombination7.ObsoleteIndex.
            if (blockIndex < RapidCombination7.ObsoleteIndex)
            {
                return new RapidCombination7
                {
                    avatarAddress = avatarAddr,
                    slotIndex = slotIndex,
                };
            }

            return new RapidCombination
            {
                avatarAddress = avatarAddr,
                slotIndex = slotIndex,
            };
        }

        public static IRapidCombination Create(
            string actionType,
            Address avatarAddr,
            int slotIndex)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                throw new NotMatchFoundException(
                    typeof(IRapidCombination),
                    actionType);
            }

            var (type, _) = Tuples.FirstOrDefault(tuple => tuple.actionType == actionType);
            if (type is null)
            {
                throw new NotMatchFoundException(
                    typeof(IRapidCombination),
                    actionType);
            }

            var action = Activator.CreateInstance(type) as IRapidCombination;
            if (action is null)
            {
                throw new NotMatchFoundException(
                    typeof(IRapidCombination),
                    actionType);
            }

            switch (action)
            {
                case RapidCombination0 rapidCombination0:
                    rapidCombination0.avatarAddress = avatarAddr;
                    rapidCombination0.slotIndex = slotIndex;
                    return rapidCombination0;
                case RapidCombination2 rapidCombination2:
                    rapidCombination2.avatarAddress = avatarAddr;
                    rapidCombination2.slotIndex = slotIndex;
                    return rapidCombination2;
                case RapidCombination3 rapidCombination3:
                    rapidCombination3.avatarAddress = avatarAddr;
                    rapidCombination3.slotIndex = slotIndex;
                    return rapidCombination3;
                case RapidCombination4 rapidCombination4:
                    rapidCombination4.avatarAddress = avatarAddr;
                    rapidCombination4.slotIndex = slotIndex;
                    return rapidCombination4;
                case RapidCombination5 rapidCombination5:
                    rapidCombination5.avatarAddress = avatarAddr;
                    rapidCombination5.slotIndex = slotIndex;
                    return rapidCombination5;
                case RapidCombination6 rapidCombination6:
                    rapidCombination6.avatarAddress = avatarAddr;
                    rapidCombination6.slotIndex = slotIndex;
                    return rapidCombination6;
                case RapidCombination7 rapidCombination7:
                    rapidCombination7.avatarAddress = avatarAddr;
                    rapidCombination7.slotIndex = slotIndex;
                    return rapidCombination7;
                case RapidCombination rapidCombination:
                    rapidCombination.avatarAddress = avatarAddr;
                    rapidCombination.slotIndex = slotIndex;
                    return rapidCombination;
                default:
                    throw new NotMatchFoundException(
                        $"{actionType} is not supported.",
                        new NotImplementedException());
            }
        }
    }
}
