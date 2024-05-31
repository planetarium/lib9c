using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Battle;
using Nekoyume.Data;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ExploreAdventureBoss : ActionBase
    {
        public const string TypeIdentifier = "explore_adventure_boss";
        public const int UnitApPotion = 1;

        public int Season;
        public Address AvatarAddress;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(Season)
                .Add(AvatarAddress.Serialize())
            );

        public override void LoadPlainValue(IValue plainValue)
        {
            var values = (List)((Dictionary)plainValue)["values"];
            Season = (Integer)values[0];
            AvatarAddress = values[1].ToAddress();
        }


        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

            // Validation
            var latestSeason = states.GetLatestAdventureBossSeason();
            if (latestSeason.Season != Season)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not current season: {latestSeason.Season}"
                );
            }

            if (context.BlockIndex > latestSeason.EndBlockIndex)
            {
                throw new InvalidSeasonException(
                    $"Season finished at block {latestSeason.EndBlockIndex}."
                );
            }

            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState.agentAddress != context.Signer)
            {
                throw new InvalidAddressException();
            }

            var exploreBoard = states.GetExploreBoard(Season);
            var explorer = states.TryGetExplorer(Season, AvatarAddress, out var exp) ? exp : new Explorer(AvatarAddress);
            exploreBoard.AddExplorer(AvatarAddress);

            if (explorer.Floor == UnlockFloor.TotalFloor)
            {
                throw new InvalidOperationException("Already cleared all floors");
            }

            var sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(MaterialItemSheet),
            });
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var material =
                materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
            var inventory = states.GetInventory(AvatarAddress);
            var random = context.GetRandom();
            var selector = new WeightedSelector<AdventureBossData.ExploreReward>(random);
            var rewardList = new List<AdventureBossData.ExploreReward>();

            // Claim floors from last failed
            for (var fl = explorer.Floor + 1; fl < explorer.MaxFloor + 1; fl++)
            {
                // Use AP Potion
                if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                        UnitApPotion))
                {
                    break;
                }

                exploreBoard.UsedApPotion += UnitApPotion;
                explorer.UsedApPotion += UnitApPotion;

                // TODO: Run simulator
                // var simulator = new AdventureBossSimulator(
                //     bossId: latestSeason.BossId,
                //     stageId: fl,
                // );

                // Get Reward if cleared
                // TODO: if (simulator.Log.IsClear)
                if (true)
                {
                    // Add point, reward
                    var (minPoint, maxPoint) = AdventureBossData.PointDict[fl];
                    var point = random.Next(minPoint, maxPoint + 1);

                    explorer.Floor = fl;
                    explorer.Score += point;

                    exploreBoard.TotalPoint += point;

                    selector.Clear();
                    var floorReward = AdventureBossData.AdventureBossRewards
                        .First(rw => rw.BossId == latestSeason.BossId).exploreReward[fl];
                    foreach (var reward in floorReward.FirstReward)
                    {
                        selector.Add(reward, reward.Ratio);
                    }

                    // Explore clear is always first because we explore from last failed floor.
                    rewardList.Add(selector.Select(1).First());

                    selector.Clear();
                    foreach (var reward in floorReward.Reward)
                    {
                        selector.Add(reward, reward.Ratio);
                    }

                    rewardList.Add(selector.Select(1).First());
                }
                else
                {
                    break;
                }
            }

            states = AdventureBossHelper.AddExploreRewards(context, states, AvatarAddress,
                inventory, rewardList);

            return states
                .SetInventory(AvatarAddress, inventory)
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer);
        }
    }
}
