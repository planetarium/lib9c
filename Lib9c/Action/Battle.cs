using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("battle")]
    public class Battle : GameAction
    {
        public const int HpIncreasingModifier = 5;
        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public string signedMemo;

        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<RuneSlotInfo> runeInfos;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [MyAvatarAddressKey] = myAvatarAddress.Serialize(),
                [EnemyAvatarAddressKey] = enemyAvatarAddress.Serialize(),
                [SignedMemoKey] = signedMemo.Serialize(),
                [CostumesKey] = new List(
                    costumes.OrderBy(element => element).Select(e => e.Serialize())
                ),
                [EquipmentsKey] = new List(
                    equipments.OrderBy(element => element).Select(e => e.Serialize())
                ),
                [RuneInfos] = runeInfos
                    .OrderBy(x => x.SlotIndex)
                    .Select(x => x.Serialize())
                    .Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            myAvatarAddress = plainValue[MyAvatarAddressKey].ToAddress();
            enemyAvatarAddress = plainValue[EnemyAvatarAddressKey].ToAddress();
            signedMemo = plainValue[SignedMemoKey].ToString();
            costumes = ((List)plainValue[CostumesKey]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue[EquipmentsKey]).Select(e => e.ToGuid()).ToList();
            runeInfos = plainValue[RuneInfos].ToList(x => new RuneSlotInfo((List)x));
        }

        public override IWorld Execute(IActionContext context)
        {
            var sw = Stopwatch.StartNew();
            GasTracer.UseGas(1);
            var addressesHex = GetSignerAndOtherAddressesHex(
                context,
                myAvatarAddress,
                enemyAvatarAddress
            );
            Log.Verbose("{AddressesHex} Action execution started", addressesHex);

            var states = context.PreviousState;
            sw.Stop();

            var myAvatarState = MeasureAndLog(
                "Validate and get avatar state",
                sw,
                () => ValidateAndGetMyAvatarState(states, context.Signer)
            );

            var collectionStates = MeasureAndLog(
                "Get collection states",
                sw,
                () => states.GetCollectionStates(new[] { myAvatarAddress, enemyAvatarAddress })
            );
            var gameConfigState = states.GetGameConfigState();
            var sheets = MeasureAndLog(
                "Load sheets",
                sw,
                () => LoadSheets(states, collectionStates.Any())
            );

            var enemySpec = MeasureAndLog("Get enemy spec", sw, () => PrepareEnemyState(states));

            var updatedStatesAndSlots = MeasureAndLog(
                "Update my slots",
                sw,
                () =>
                    PrepareMyState(
                        states,
                        sheets,
                        myAvatarState,
                        context.BlockIndex,
                        addressesHex,
                        gameConfigState
                    )
            );

            var (updatedStates, myRuneSlotState, myRuneStates) = updatedStatesAndSlots;

            var resultLog = MeasureAndLog(
                "Simulate battle",
                sw,
                () =>
                    Simulate(
                        updatedStates,
                        sheets,
                        myAvatarState,
                        context.GetRandom(),
                        gameConfigState,
                        collectionStates.Any(),
                        collectionStates,
                        (myRuneSlotState, myRuneStates),
                        enemySpec
                    )
            );

            updatedStates = updatedStates.SetAvatarState(myAvatarAddress, myAvatarState);

            var parts = signedMemo.Split('/');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Invalid signed memo format.");
            }

            var addressHex = parts[0];

            var accountAddress = Addresses.Battle.Derive(new Address(addressHex).ToHex());
            var account = updatedStates.GetAccount(accountAddress);
            account = account.SetState(
                myAvatarAddress.Derive(context.TxId.ToString()),
                (Integer)(resultLog.Result.Equals(ArenaLog.ArenaResult.Win) ? 1 : 0)
            );
            updatedStates = updatedStates.SetAccount(accountAddress, account);

            sw.Stop();
            Log.Debug(
                "{AddressesHex} Total execution time: {Elapsed}",
                addressesHex,
                sw.Elapsed.TotalMilliseconds
            );

            return updatedStates;
        }

        private T MeasureAndLog<T>(string process, Stopwatch stopwatch, Func<T> action)
        {
            stopwatch.Restart();
            var result = action();
            stopwatch.Stop();
            Log.Verbose(
                "{Process} completed in {Elapsed} ms",
                process,
                stopwatch.Elapsed.TotalMilliseconds
            );
            return result;
        }

        private AvatarState ValidateAndGetMyAvatarState(IWorld states, Address signer)
        {
            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException("Battle initiated with identical addresses.");
            }

            if (!states.TryGetAvatarState(signer, myAvatarAddress, out var myAvatarState))
            {
                throw new FailedLoadStateException("Failed to load avatar state for signer.");
            }

            return myAvatarState;
        }

        private Dictionary<Type, (Address address, ISheet sheet)> LoadSheets(
            IWorld states,
            bool collectionExist
        )
        {
            var sheetTypes = new List<Type>
            {
                typeof(ArenaSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
            };

            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: sheetTypes
            );
            return sheets;
        }

        private (
            IWorld UpdatedStates,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates
        ) PrepareMyState(
            IWorld states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            long blockIndex,
            string addressesHex,
            GameConfigState gameConfigState
        )
        {
            myAvatarState.ValidEquipmentAndCostumeV2(
                costumes,
                equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                blockIndex,
                addressesHex,
                gameConfigState
            );

            var myRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myRuneSlotState = states.TryGetLegacyState(
                myRuneSlotStateAddress,
                out List rawRuneSlotState
            )
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            myRuneSlotState.UpdateSlot(runeInfos, runeListSheet);
            states = states.SetLegacyState(myRuneSlotStateAddress, myRuneSlotState.Serialize());

            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(
                myAvatarAddress,
                BattleType.Arena
            );
            var myItemSlotState = states.TryGetLegacyState(
                myItemSlotStateAddress,
                out List rawItemSlotState
            )
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            myItemSlotState.UpdateEquipment(equipments);
            myItemSlotState.UpdateCostumes(costumes);
            states = states.SetLegacyState(myItemSlotStateAddress, myItemSlotState.Serialize());

            var myRuneStates = states.GetRuneState(myAvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(myAvatarAddress, myRuneStates);
            }

            foreach (var runeSlotInfo in runeInfos)
            {
                myRuneStates.GetRuneState(runeSlotInfo.RuneId);
            }

            return (states, myRuneSlotState, myRuneStates);
        }

        private (
            ItemSlotState ItemSlotState,
            RuneSlotState RuneSlotState,
            AllRuneState RuneStates
        ) PrepareEnemyState(IWorld states)
        {
            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyItemSlotState = states.TryGetLegacyState(
                enemyItemSlotStateAddress,
                out List rawEnemyItemSlotState
            )
                ? new ItemSlotState(rawEnemyItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(
                enemyAvatarAddress,
                BattleType.Arena
            );
            var enemyRuneSlotState = states.TryGetLegacyState(
                enemyRuneSlotStateAddress,
                out List enemyRawRuneSlotState
            )
                ? new RuneSlotState(enemyRawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = states.GetRuneState(enemyAvatarAddress, out _);

            return (enemyItemSlotState, enemyRuneSlotState, enemyRuneStates);
        }

        private ArenaLog Simulate(
            IWorld states,
            Dictionary<Type, (Address address, ISheet sheet)> sheets,
            AvatarState myAvatarState,
            IRandom random,
            GameConfigState gameConfigState,
            bool collectionExist,
            Dictionary<Address, CollectionState> collectionStates,
            (RuneSlotState RuneSlotState, AllRuneState RuneStates) mySpec,
            (
                ItemSlotState ItemSlotState,
                RuneSlotState RuneSlotState,
                AllRuneState RuneStates
            ) enemySpec
        )
        {
            var myArenaPlayerDigest = new ArenaPlayerDigest(
                myAvatarState,
                equipments,
                costumes,
                mySpec.RuneStates,
                mySpec.RuneSlotState
            );
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                enemyAvatarState,
                enemySpec.ItemSlotState.Equipments,
                enemySpec.ItemSlotState.Costumes,
                enemySpec.RuneStates,
                enemySpec.RuneSlotState
            );

            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var collectionModifiers = new Dictionary<Address, List<StatModifier>>
            {
                [myAvatarAddress] = new(),
                [enemyAvatarAddress] = new(),
            };

            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                foreach (var (address, state) in collectionStates)
                {
                    collectionModifiers[address] = state.GetModifiers(collectionSheet);
                }
            }

            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            var simulator = new ArenaSimulator(
                random,
                HpIncreasingModifier,
                gameConfigState.ShatterStrikeMaxDamage
            );
            return simulator.Simulate(
                myArenaPlayerDigest,
                enemyArenaPlayerDigest,
                sheets.GetArenaSimulatorSheets(),
                collectionModifiers[myAvatarAddress],
                collectionModifiers[enemyAvatarAddress],
                buffLimitSheet,
                buffLinkSheet,
                true
            );
        }
    }
}
