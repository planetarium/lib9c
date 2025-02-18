using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// The ClaimWorldBossReward class is an action that allows a player to claim rewards for their contribution in defeating a world boss.
    /// This action ensures that only the owner of the avatar can claim the reward and calculates the reward based on the player's contribution.
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ClaimWorldBossReward : GameAction
    {
        /// <summary>
        /// The minimum contribution percentage required to be eligible for a reward.
        /// </summary>
        public const decimal MinimumContribution = 0.0001m;

        /// <summary>
        /// A string identifier for this action type.
        /// </summary>
        public const string TypeIdentifier = "claim_world_boss_reward";

        /// <summary>
        /// The address of the avatar claiming the reward.
        /// </summary>
        public Address AvatarAddress;

        /// <summary>
        /// Initializes a new instance of the ClaimWorldBossReward class.
        /// </summary>
        public ClaimWorldBossReward()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified avatar address.
        /// </summary>
        /// <param name="address">The address of the avatar.</param>
        public ClaimWorldBossReward(Address address)
        {
            AvatarAddress = address;
        }

        /// <summary>
        /// Executes the action to claim the world boss reward.
        /// </summary>
        /// <param name="context">The action context.</param>
        /// <returns>The updated world state.</returns>
        /// <exception cref="InvalidAddressException">Thrown if the avatar address does not belong to the signer.</exception>
        /// <exception cref="AlreadyClaimedException">Thrown if the reward has already been claimed.</exception>
        /// <exception cref="InvalidClaimException">Thrown if no valid rewards are calculated.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the contribution does not meet the minimum requirement.</exception>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var currentBlockIndex = context.BlockIndex;
            // Validate that the avatar address belongs to the signer.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            // Retrieve previous season's WorldBossState
            var sheets = states.GetSheets(containItemSheet: true, sheetTypes: new []
            {
                typeof(WorldBossListSheet),
                typeof(WorldBossContributionRewardSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            var previousSeasonRow = worldBossListSheet.FindPreviousRowByBlockIndex(currentBlockIndex);
            var raidId = previousSeasonRow.Id;
            var worldBossAddress = Addresses.GetWorldBossAddress(raidId);
            var raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);

            // Retrieve RaiderState
            var raiderState = new RaiderState((List)states.GetLegacyState(raiderAddress));
            if (raiderState.HasClaimedReward)
            {
                throw new AlreadyClaimedException();
            }

            // Calculate contribution
            var worldBossState = new WorldBossState((List)states.GetLegacyState(worldBossAddress));
            var contribution =
                WorldBossHelper.CalculateContribution(worldBossState.TotalDamage,
                    raiderState.TotalScore);

            // Reward claim logic
            if (contribution >= MinimumContribution)
            {
                var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
                var inventory = avatarState.inventory;

                // Implement reward distribution logic
                var itemSheet = sheets.GetItemSheet();
                var rewardSheet = sheets.GetSheet<WorldBossContributionRewardSheet>();
                var rewardRow = rewardSheet[worldBossState.Id];
                var random = context.GetRandom();
                var (items, fav) =
                    WorldBossHelper.CalculateContributionReward(rewardRow, contribution);
                if (!items.Any() && !fav.Any())
                {
                    throw new InvalidClaimException();
                }

                foreach (var (itemId, count) in items)
                {
                    var itemRow = itemSheet[itemId];
                    inventory.MintItem(itemRow, count, false, random);
                }

                foreach (var asset in fav)
                {
                    var recipient = Currencies.PickAddress(asset.Currency, context.Signer, AvatarAddress);
                    states = states.MintAsset(context, recipient, asset);
                }
                var mailBox = avatarState.mailBox;
                var mail = new WorldBossRewardMail(context.BlockIndex, random.GenerateRandomGuid(), context.BlockIndex, fav, items);
                mailBox.Add(mail);
                mailBox.CleanUp();
                avatarState.mailBox = mailBox;

                // Set states
                return states
                    .SetAvatarState(AvatarAddress, avatarState, setAvatar: true, setInventory: true,
                        setWorldInformation: false, setQuestList: false);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Serializes the action's data into a plain value format.
        /// </summary>
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();

        /// <summary>
        /// Deserializes the plain value back into the action's data.
        /// </summary>
        /// <param name="plainValue">The serialized plain value.</param>
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
