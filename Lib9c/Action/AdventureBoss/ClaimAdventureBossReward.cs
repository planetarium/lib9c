using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions;
using Nekoyume.Data;
using Nekoyume.Exceptions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ClaimAdventureBossReward : GameAction
    {
        public const string TypeIdentifier = "claim_adventure_boss_reward";

        public Address AvatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            AvatarAddress = plainValue["a"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

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

            var gameConfig = states.GetGameConfigState();
            var latestSeason = states.GetLatestAdventureBossSeason();
            var myReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            var ncgRewardRatioSheet = states.GetSheet<AdventureBossNcgRewardRatioSheet>();
            var continueInv = true;
            var continueExp = true;
            var random = context.GetRandom();

            for (var szn = latestSeason.Season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);
                if (seasonInfo.EndBlockIndex > context.BlockIndex)
                {
                    // Season in progress. Skip this season.
                    continue;
                }

                if (seasonInfo.EndBlockIndex + gameConfig.AdventureBossClaimInterval <
                    context.BlockIndex)
                {
                    // Claim interval expired.
                    break;
                }

                var bountyBoard = states.GetBountyBoard(szn);
                var exploreBoard = states.GetExploreBoard(szn);
                var explorerList = states.GetExplorerList(szn);

                // Pick explore raffle winner
                if (exploreBoard.RaffleReward is null)
                {
                    exploreBoard =
                        AdventureBossHelper.PickExploreRaffle(bountyBoard, exploreBoard,
                            explorerList, random);
                    states = states.SetExploreBoard(szn, exploreBoard);
                }

                // Send 80% NCG to operational account. 20% are for rewards.
                var seasonBountyBoardAddress =
                    Addresses.BountyBoard.Derive(
                        AdventureBossHelper.GetSeasonAsAddressForm(szn));
                if (bountyBoard.totalBounty() ==
                    states.GetBalance(seasonBountyBoardAddress, bountyBoard.totalBounty().Currency)
                   )
                {
                    states = states.TransferAsset(context, seasonBountyBoardAddress,
                        // FIXME: Set operational account address
                        new Address(),
                        (bountyBoard.totalBounty() * 80).DivRem(100, out _)
                    );
                }

                var investor =
                    bountyBoard.Investors.FirstOrDefault(inv => inv.AvatarAddress == AvatarAddress);
                var ncgReward = 0 * bountyBoard.totalBounty().Currency;
                var explorer = states.TryGetExplorer(szn, AvatarAddress, out var exp) ? exp : null;

                if ((investor is not null && investor.Claimed) ||
                    (explorer is not null && explorer.Claimed))
                {
                    // Already claimed reward. Stop here.
                    break;
                }

                if (investor is not null)
                {
                    continueInv = AdventureBossHelper.CollectWantedReward(myReward,
                        gameConfig, ncgRewardRatioSheet, seasonInfo, bountyBoard, investor,
                        context.BlockIndex, AvatarAddress, ref myReward
                    );
                    investor.Claimed = true;
                    states = states.SetBountyBoard(szn, bountyBoard);
                }

                if (explorer is not null)
                {
                    continueExp = AdventureBossHelper.CollectExploreReward(myReward, gameConfig,
                        ncgRewardRatioSheet, seasonInfo, bountyBoard, exploreBoard, explorer,
                        context.BlockIndex, AvatarAddress, ref myReward, out ncgReward);
                    explorer.Claimed = true;
                    states = states.SetExplorer(szn, explorer);
                }

                if (ncgReward.RawValue > 0)
                {
                    states = states.TransferAsset(context,
                        Addresses.BountyBoard.Derive(
                            AdventureBossHelper.GetSeasonAsAddressForm(szn)
                        ),
                        context.Signer,
                        ncgReward);
                }

                if (!continueInv && !continueExp)
                {
                    break;
                }
            }

            if (myReward.IsEmpty())
            {
                throw new EmptyRewardException($"{AvatarAddress} has no reward to receive.");
            }

            // Give rewards
            // NOTE: NCG must be transferred from seasonal address. So this must be done in collection stage.
            if (myReward.ItemReward.Count > 0)
            {
                var materialSheet = states.GetSheet<MaterialItemSheet>();
                var inventory = states.GetInventoryV2(AvatarAddress);
                foreach (var reward in myReward.ItemReward.ToImmutableSortedDictionary())
                {
                    var material =
                        ItemFactory.CreateMaterial(
                            materialSheet.Values.First(row => row.Id == reward.Key));
                    inventory.AddItem(material, reward.Value);
                }

                states = states.SetInventory(AvatarAddress, inventory);
            }

            if (myReward.FavReward.Count > 0)
            {
                var runeSheet = states.GetSheet<RuneSheet>();
                foreach (var reward in myReward.FavReward.ToImmutableSortedDictionary())
                {
                    var runeRow = runeSheet.Values.First(row => row.Id == reward.Key);
                    var ticker = runeRow.Ticker;
                    var currency = Currencies.GetRune(ticker);
                    states = states.MintAsset(context, AvatarAddress, currency * reward.Value);
                }
            }

            return states;
        }
    }
}
