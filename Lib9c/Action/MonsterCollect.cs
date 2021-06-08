using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("monster_collect2")]
    public class MonsterCollect : GameAction
    {
        public int level;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, 0);
            if (context.Rehearsal)
            {
                return states
                    .SetState(monsterCollectionAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, monsterCollectionAddress);
            }

            MonsterCollectionSheet monsterCollectionSheet = states.GetSheet<MonsterCollectionSheet>();

            AgentState agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            if (level < 0 || level > 0 && !monsterCollectionSheet.TryGetValue(level, out MonsterCollectionSheet.Row _))
            {
                throw new MonsterCollectionLevelException();
            }

            Currency currency = states.GetGoldCurrency();
            // Set default gold value.
            FungibleAssetValue requiredGold = currency * 0;
            FungibleAssetValue balance = states.GetBalance(context.Signer, currency);

            if (states.TryGetState(monsterCollectionAddress, out Dictionary stateDict))
            {
                var existingStates = new MonsterCollectionState(stateDict);
                int previousLevel = existingStates.Level;
                // 락업 확인
                if (level < previousLevel && existingStates.IsLock(context.BlockIndex))
                {
                    throw new RequiredBlockIndexException();
                }

                if (level == previousLevel)
                {
                    throw new MonsterCollectionLevelException();
                }

                // 언스테이킹
                FungibleAssetValue gold = states.GetBalance(monsterCollectionAddress, currency);
                states = states.TransferAsset(monsterCollectionAddress, context.Signer, gold);
            }

            if (level == 0)
            {
                return states.SetState(monsterCollectionAddress, new Null());
            }

            var monsterCollectionState = new MonsterCollectionState(monsterCollectionAddress, level, context.BlockIndex);
            for (int i = 0; i < level; i++)
            {
                requiredGold += currency * monsterCollectionSheet[i + 1].RequiredGold;
            }

            if (balance < requiredGold)
            {
                throw new InsufficientBalanceException(context.Signer, requiredGold,
                    $"There is no sufficient balance for {context.Signer}: {balance} < {requiredGold}");
            }
            states = states.TransferAsset(context.Signer, monsterCollectionAddress, requiredGold);
            states = states.SetState(monsterCollectionAddress, monsterCollectionState.SerializeV2());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [LevelKey] = level.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            level = plainValue[LevelKey].ToInteger();
        }
    }
}
