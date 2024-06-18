using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Extensions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class UnlockFloor : ActionBase
    {
        public const string TypeIdentifier = "unlock_floor";
        public const int OpeningFloor = 5;
        public const int TotalFloor = 20;
        public const int GoldenDustId = 600201;

        // NOTE: This may temporary
        // Use MaxFloor as key. If not find key, this means already opened all floors.
        public readonly Dictionary<int, Dictionary<string, int>> UnlockDict =
            new ()
            {
                {
                    5, new Dictionary<string, int>
                    {
                        { "NCG", 5 },
                        { "GoldenDust", 5 },
                    }
                },
                {
                    10, new Dictionary<string, int>
                    {
                        { "NCG", 10 },
                        { "GoldenDust", 10 },
                    }
                },
                {
                    15, new Dictionary<string, int>
                    {
                        { "NCG", 15 },
                        { "GoldenDust", 15 },
                    }
                },
            };

        public int Season;
        public Address AvatarAddress;
        public bool UseNcg;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values",
                List.Empty
                    .Add(Season.Serialize())
                    .Add(AvatarAddress.Serialize())
                    .Add(UseNcg.Serialize())
            );

        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List)((Dictionary)plainValue)["values"];
            Season = list[0].ToInteger();
            AvatarAddress = list[1].ToAddress();
            UseNcg = list[2].ToBoolean();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var currency = states.GetGoldCurrency();

            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
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

            if (!UnlockDict.ContainsKey(explorer.MaxFloor))
            {
                throw new InvalidOperationException(
                    $"Floor {explorer.MaxFloor} not found. Maybe already opened all floors."
                );
            }

            // Check balance and unlock
            var price = UnlockDict[explorer.MaxFloor];
            var agentAddress = states.GetAvatarState(AvatarAddress).agentAddress;
            var balance = states.GetBalance(agentAddress, currency);
            var exploreBoard = states.GetExploreBoard(Season);
            if (UseNcg)
            {
                if (balance < price["NCG"] * currency)
                {
                    throw new InsufficientBalanceException(
                        $"{balance} is less than {price["NCG"] * currency}",
                        agentAddress, balance
                    );
                }

                explorer.UsedNcg += price["NCG"];
                exploreBoard.UsedNcg += price["NCG"];
                // FIXME: Send unlock NCG to operational address
                states = states.TransferAsset(context, agentAddress, new Address(),
                    price["NCG"] * currency);
            }
            else // Use GoldenDust
            {
                var sheets = states.GetSheets(sheetTypes: new[]
                {
                    typeof(MaterialItemSheet),
                });
                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var material = materialSheet.OrderedList.First(m => m.Id == GoldenDustId);

                var inventory = states.GetInventoryV2(AvatarAddress);
                if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                        price["GoldenDust"]))
                {
                    throw new NotEnoughMaterialException(
                        $"Not enough golden dust to open new floor: needs {price["GoldenDust"]}");
                }

                explorer.UsedGoldenDust += price["GoldenDust"];
                exploreBoard.UsedGoldenDust += price["GoldenDust"];
                states = states.SetInventory(AvatarAddress, inventory);
            }

            explorer.MaxFloor += OpeningFloor;

            return states
                .SetExploreBoard(Season, exploreBoard)
                .SetExplorer(Season, explorer);
        }
    }
}
