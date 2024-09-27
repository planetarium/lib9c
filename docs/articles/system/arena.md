# Arena

Once you've developed your avatar through the [Adventure](./adventure.md), you'll have access to Arena content where you can compete against other players. For more information, see the [official documentation](https://docs.nine-chronicles.com/introduction/intro/game-contents/arena-pvp-competition).

## Round

The Arena operates in rounds. Each round lasts for a certain amount of block range and is of one of the following types: seasonal, offseason, or championship.

- Championship ID the round belongs to: [Nekoyume.TableData.ArenaSheet.Row.ChampionshipId](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L17)
- Round number within a championship ID: [Nekoyume.TableData.ArenaSheet.Row.Round](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L17)
- Type of the round: [Nekoyume.TableData.ArenaSheet.Row.ArenaType](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L19)
- Duration of the round:
   - Start: [Nekoyume.TableData.ArenaSheet.Row.StartBlockIndex](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L20)
   - End: [Nekoyume.TableData.ArenaSheet.Row.EndBlockIndex](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L21)

## Join

To join a round, use the [Nekoyume.Action.JoinArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/JoinArena.cs) action.<br>
Each round has a requirement or cost to join, which you can see in the table below.

| | offseason | season | championship | ref |
| :---: | :---: | :---: | :---: | :---: |
| Conditions of entry | X | X | O | [Nekoyume.TableData.ArenaSheet.Row.RequiredMedalCount](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L22) |
| Entry Fee | X | O | O | [Nekoyume.TableData.ArenaSheet.Row.EntranceFee](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L23) |

### Conditions of Entry

All Championship rounds have their own medal items that must be collected in a certain amount to be eligible to participate in each Championship Round.

### Entry Fee

To participate in Seasons and Championship rounds, players must pay an entry fee in crystals that is proportional to the level of the participating avatar.

### States {#join-states}

**List of round participants**

- Account Address: [Libplanet.Action.State.ReservedAddresses.LegacyAccount](https://github.com/planetarium/libplanet/blob/main/src/Libplanet.Action/State/ReservedAddresses.cs#L7)
- State Address: This will have a separate address based on the Championship ID and round.
    ```cs
    public Address GetArenaParticipantsAddress(int championshipId, int round)
    {
        return ArenaParticipants.DeriveAddress(championshipId, round);
    }
    ```
- Type: [Nekoyume.Model.Arena.ArenaParticipants](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Arena/ArenaParticipants.cs)

**Participant's round information**

- Account Address: [Libplanet.Action.State.ReservedAddresses.LegacyAccount](https://github.com/planetarium/libplanet/blob/main/src/Libplanet.Action/State/ReservedAddresses.cs#L7)
- State Address: This will have a separate address based on the Championship ID and round and your avatar's address.
    ```cs
    public Address GetArenaInformation(Address avatarAddress, int championshipId, int round)
    {
        ArenaInformation.DeriveAddress(avatarAddress, championshipId, round);
    }
    ```
- Type: [Nekoyume.Model.Arena.ArenaInformation](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Arena/ArenaInformation.cs)

**Participant's score**

- Account Address: [Libplanet.Action.State.ReservedAddresses.LegacyAccount](https://github.com/planetarium/libplanet/blob/main/src/Libplanet.Action/State/ReservedAddresses.cs#L7)
- State Address: This will have a separate address based on the Championship ID and round and your avatar's address.
    ```cs
    public Address GetArenaScore(Address avatarAddress, int championshipId, int round)
    {
        ArenaScore.DeriveAddress(avatarAddress, championshipId, round);
    }
    ```
- Type: [Nekoyume.Model.Arena.ArenaScore](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Arena/ArenaScore.cs)

## Battle(ArenaSimulator) {#battle}

The battle uses the [Nekoyume.Action.BattleArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/BattleArena.cs) action and utilizes the [Nekoyume.Arena.ArenaSimulator](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaSimulator.cs) class to perform the battle.

### Tickets {#battle-tickets}

Arena tickets are required to fight battles. When you first join a round, you are automatically issued the maximum number of tickets. These tickets are issued anew to participants in that round at each round's ticket reset cycle.

- Ticket maximum: [Nekoyume.Model.Arena.ArenaInformation.MaxTicketCount](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Arena/ArenaInformation.cs#L16)
- Ticket reset interval: [Nekoyume.Model.State.GameConfigState.DailyArenaInterval](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/State/GameConfigState.cs#L20)

**Buy Tickets**

If you run out of tickets, you can purchase them directly to use in battle. There is a maximum amount and price of tickets that can be purchased within a round or ticket reset cycle.

- Maximum number of tickets you can purchase within a round: [Nekoyume.TableData.ArenaSheet.RoundData.MaxPurchaseCount](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L26)
- Maximum number of tickets you can purchase within a ticket reset cycle: [Nekoyume.TableData.ArenaSheet.RoundData.MaxPurchaseCountWithInterval](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/ArenaSheet.cs#L27)
- Ticket purchase price: The purchase price of tickets is determined by a set price for each round and the number of tickets already purchased, as shown in the code below.
    ```cs
    public FungibleAssetValue GetTicketPrice(
        ArenaSheet.RoundData round,
        int alreadyPurchasedCount)
    {
        return GetTickgetPrice(
            round.TicketPrice,
            round.AdditionalTicketPrice,
            alreadyPurchasedCount);
    }

    public FungibleAssetValue GetTicketPrice(
        decimal price,
        decimal additionalprice,
        int alreadyPurchasedCount)
    {
        return price.DivRem(100, out _) +
            additionalprice.DivRem(100, out _) * alreadyPurchasedCount;
    }
    ```

### Battle Target Limit

In Arena, there is a limit to who you can battle. This is the score limit, see the code below.

https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/BattleArena.cs#L274
```cs:line-numbers=274
if (!ArenaHelper.ValidateScoreDifference(
    ArenaHelper.ScoreLimits,
    roundData.ArenaType,
    myArenaScore.Score,
    enemyArenaScore.Score))
{
    // ...
}
```

In the code above, the [ArenaHelper.ValidateScoreDifference](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaHelper.cs#L135) method validates the difference between the player's and enemy's scores to determine if a battle is possible. It takes in [ArenaHelper.ScoreLimits](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaHelper.cs#L50), the arena type from the round data, and the player's and enemy's scores as arguments to determine if the score difference is within an acceptable range.

The current score limits are shown in the table below.

| attacker score - defender Score | offseason | season | championships |
| :-: | :-: | :-: | :-: |
| minimum score difference | - | -100 | -100 | -100 |
| max score difference | - | 200 | 200 | 200 |

For example, when fighting in the Offseason round, you can challenge anyone to a fight with no score limit, while in the Season or Championship round, if your player avatar has a score of 2000, the opponent you can challenge must have a score between 1900 and 2200.

### Rules(different from the Adventure) {#battle-rule}

Unlike the Adventure, Arena battles are fought between players(PvP). The basic battle rules are the same as for the Adventure, but see below for the differences.

**Attack skill hits**.

Battles in the Arena follow different rules than in Adventure when determining whether an attack skill hits.

- See also: [Adventure > Normal Attack Hits](./adventure.md#normal-attack-hits)
- Determines the hit of all attack skills, not just **normal attack**.
- Does not take into account the level difference between attacker and defender.

| Content | Hit or Miss Coverage | Variables |
| :-: | :-: | :-: |
| Arena | All attack skills, including **normal attack** | HIT stat
| Adventure | **Normal Attack** | Avatar Level, HIT Stat |

Below is the order in which hits are determined in the Arena.

- Get the hit status of an attack skill in the [Nekoyume.Model.Skill.ArenaAttackSkill.ProcessDamage](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Skill/Arena/ArenaAttackSkill.cs#L46) method: `target.IsHit(caster)`.
- The `IsHit` method above points to [Nekoyume.Model.ArenaCharacter.IsHit(ArenaCharacter)](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Character/ArenaCharacter.cs#L930).
- If we take a quick look at the logic of a hit in the Arena:
   - If the attacker is under the effect of a focus buff([Nekoyume.Model.Buff.Focus](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Buff/Focus.cs)), it will hit 100% of the time.
      - Otherwise, handle hits based on the result of the [Nekoyume.Battle.HitHelper.IsHitWithoutLevelCorrection](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Battle/HitHelper.cs#L46) method.

## Rewards

Rewards in Arena battles are divided into base rewards and victory rewards.

**Base Reward**

Base Rewards are determined by Avatar's Arena Score and range in quantity and type based on Avatar's level.

- Rewards list:
    - [Nekoyume.TableData.WeeklyArenaRewardSheet](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/TableData/WeeklyArenaRewardSheet.cs)
    - https://9c-board.nine-chronicles.dev/odin/tablesheet/WeeklyArenaRewardSheet
- Number of rewards per Arena Score: Check out the [Nekoyume.Arena.ArenaHelper.GetRewardCount](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaHelper.cs#L182) method.
    | Score | 1000 | 1001~ | 1100~ | 1200~ | 1400~ | 1800~ |
    | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
    | Number of rewards | 1 | 2 | 3 | 4 | 5 | 6 |

**Victory Rewards(Medals)**

When you win an Arena battle, you receive one medal for the Championship that the round belongs to. For more information, see the [Nekoyume.Action.BettleArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/BattleArena.cs#L446) action and the [Nekoyume.Arena.ArenaHelper.GetMedal](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaHelper.cs#L60) method.

## Score

When you join a round, you start with a score of 1000 points([Nekoyume.Model.Arena.ArenaScore.ArenaScoreDefault](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Arena/ArenaScore.cs#L17)).<br>
After that, the score changes through the battle, which is affected by the difference between the attacker's and defender's scores. For more information, see the [Nekoyume.Action.BettleArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/BattleArena.cs#L454) action and the [Nekoyume.Arena.ArenaHelper.GetScores](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Arena/ArenaHelper.cs#L160) method.

## Ranking

The ranking is determined by the score in the arena. It's important to note that the ranking is not handled by the blockchain state, but by an external service.

**Handle Ties**

In Nine Chronicles' Arena Rankings, ties are broken by bundling them into a lower ranking. For example, if the highest score in a particular round is 2000 points and there are three ties, they will all be treated as third place.

## Actions

List of actions related to the arena:

- [JoinArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/JoinArena.cs): An action to join a specific round in the Arena.
- [BattleArena](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Action/BattleArena.cs): An action in which you battle other avatars in a specific round of the Arena.
