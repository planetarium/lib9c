using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Data;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class UnlockFloor : GameAction
    {
        public const string TypeIdentifier = "unlock_floor";
        public const int OpeningFloor = 5;
        public const int TotalFloor = 20;
        public const int GoldenDustId = 600201;

        public int Season;
        public Address AvatarAddress;
        public bool UseNcg;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["s"] = (Integer)Season,
                ["a"] = AvatarAddress.Serialize(),
                ["u"] = UseNcg.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Season = (Integer)plainValue["s"];
            AvatarAddress = plainValue["a"].ToAddress();
            UseNcg = plainValue["u"].ToBoolean();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var currency = states.GetGoldCurrency();
            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
            var addresses = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            // NOTE: The `AvatarAddress` must contained in `Signer`'s `AgentState.avatarAddresses`.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    TypeIdentifier,
                    addresses,
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in" +
                    $" AvatarAddress({AvatarAddress}).");
            }

            if (Season != latestSeason.Season)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not current season {latestSeason.Season}"
                );
            }

            if (context.BlockIndex > latestSeason.EndBlockIndex)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Season {Season} Finished at block {latestSeason.EndBlockIndex}"
                );
            }

            if (!states.TryGetExplorer(Season, AvatarAddress, out var explorer))
            {
                throw new FailedLoadStateException($"No Explorer {AvatarAddress} found.");
            }

            if (explorer.MaxFloor == TotalFloor)
            {
                throw new InvalidOperationException("Already opened all floors");
            }

            if (explorer.Floor != explorer.MaxFloor)
            {
                throw new InvalidOperationException(
                    $"You have to clear floor {explorer.MaxFloor}. Current floor is {explorer.Floor}"
                );
            }

            var sheets = states.GetSheets(new[]
            {
                typeof(MaterialItemSheet),
                typeof(AdventureBossSheet),
                typeof(AdventureBossFloorSheet),
                typeof(AdventureBossUnlockFloorCostSheet),
            });

            var floorSheet = sheets.GetSheet<AdventureBossFloorSheet>();
            var costSheet = sheets.GetSheet<AdventureBossUnlockFloorCostSheet>();
            var adventureBossId = sheets.GetSheet<AdventureBossSheet>().OrderedList.First(
                row => row.BossId == latestSeason.BossId
            ).Id;
            var floorId = floorSheet.OrderedList.First(row =>
                row.AdventureBossId == adventureBossId && row.Floor == explorer.MaxFloor + 1
            ).Id;

            if (!costSheet.ContainsKey(floorId))
            {
                throw new InvalidOperationException(
                    $"Floor {explorer.MaxFloor + 1} not found. Maybe already opened all floors."
                );
            }

            // Check balance and unlock
            var price = costSheet[floorId];
            var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var agentAddress = avatarState.agentAddress;
            var balance = states.GetBalance(agentAddress, currency);
            var exploreBoard = states.GetExploreBoard(Season);
            if (UseNcg)
            {
                if (balance < price.NcgPrice * currency)
                {
                    throw new InsufficientBalanceException(
                        $"{balance} is less than {price.NcgPrice * currency}",
                        agentAddress, balance
                    );
                }

                explorer.UsedNcg += price.NcgPrice;
                exploreBoard.UsedNcg += price.NcgPrice;
                var feeAddress = Addresses.RewardPool;
                // TODO: [GuildMigration] Remove this after migration
                if (states.GetDelegationMigrationHeight() is long migrationHeight
                    && context.BlockIndex < migrationHeight)
                {
                    feeAddress = AdventureBossGameData.AdventureBossOperationalAddress;
                }
                states = states.TransferAsset(context, agentAddress,
                    feeAddress,
                    price.NcgPrice * currency);
            }
            else // Use GoldenDust
            {
                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var material = materialSheet.OrderedList.First(m => m.Id == GoldenDustId);

                var inventory = states.GetInventoryV2(AvatarAddress);
                if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                        price.GoldenDustPrice))
                {
                    throw new NotEnoughMaterialException(
                        $"Not enough golden dust to open new floor: needs {price.GoldenDustPrice}");
                }

                explorer.UsedGoldenDust += price.GoldenDustPrice;
                exploreBoard.UsedGoldenDust += price.GoldenDustPrice;
                states = states.SetInventory(AvatarAddress, inventory);
            }

            explorer.MaxFloor += OpeningFloor;

            return states
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer);
        }
    }
}
