# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Lib9c is the core game logic library for **Nine Chronicles**, a decentralized RPG built on the Libplanet blockchain. It contains actions (state-mutating transactions), game models, battle simulation, and CSV-driven game data. All game logic must be **deterministic** for blockchain consensus.

## Build & Test Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~Lib9c.Tests.Action.HackAndSlashTest"

# Run a single test method
dotnet test --filter "FullyQualifiedName~Lib9c.Tests.Action.HackAndSlashTest.Execute"

# Build with Release configuration
dotnet build --configuration Release
```

Requires .NET SDK 6.0.400+ (see `global.json`, rollForward: major).

## Architecture

### Project Structure

- **Lib9c/** - Main library. Root namespace: `Nekoyume`. Contains all game logic, models, actions, and table data.
- **Lib9c.Abstractions/** - Pure interfaces. Root namespace: `Lib9c`.
- **Lib9c.Policy/** - Blockchain policy definitions.
- **Lib9c.Renderers/** - Event rendering/subscription for off-chain UI handling.
- **Lib9c.MessagePack/** - MessagePack serialization with code generation.
- **Lib9c.Utils/, Lib9c.Proposer/** - Utilities and block proposer logic.
- **Lib9c.DevExtensions/** - Dev-only features (built with `DevEx` configuration).
- **.Lib9c.Tests/** - XUnit test suite.
- **.Lib9c.Tools/** - CLI tools for management and codegen.

### Key Layers (inside `Lib9c/`)

**Actions** (`Nekoyume.Action/`) - Game operations that mutate blockchain state. Each action:
- Inherits from `ActionBase` (implements `IAction`)
- Has an `[ActionType("type_identifier")]` attribute
- Implements `Execute(IActionContext)` returning `IWorld`
- Implements `PlainValue`/`LoadPlainValue` for Bencodex serialization

**Models** (`Nekoyume.Model/`) - Immutable state objects stored on-chain. Serialized via Bencodex (`IBencodable`).

**Modules** (`Nekoyume.Module/`) - State read/write helpers that operate on `IWorld`. `LegacyModule` handles backwards-compatible state access.

**TableData** (`Nekoyume.TableData/`) - C# classes that parse CSV game data. Each `*Sheet` class corresponds to a CSV file in `Lib9c/TableCSV/`.

**TableCSV** (`Lib9c/TableCSV/`) - 260+ CSV files defining game balance (items, equipment, skills, stages, etc.). Updated frequently from Google Sheets.

**Battle** (`Nekoyume.Battle/`) - Turn-based battle simulation (PvE dungeons, PvP arena, raids, world boss).

### Core Dependencies

- **Libplanet 5.5.2** - Blockchain framework (version managed in `Directory.Build.props`)
- **Bencodex** - Canonical serialization for blockchain state
- **MessagePack** - Binary serialization for action evaluation results
- **CsvHelper** - CSV parsing for game table data

## Game Domains

### Battle & Combat
- **Dungeon (PvE)**: `HackAndSlash`, `HackAndSlashSweep` — standard stage battles
- **Arena (PvP)**: `BattleArena`, `JoinArena` — PvP ranking battles with dedicated arena skill variants (`ArenaSkill`, `ArenaNormalAttack`, etc.)
- **Raid / World Boss**: `Raid`, `ClaimRaidReward`, `ClaimWorldBossReward`
- **Adventure Boss**: `ExploreAdventureBoss`, `SweepAdventureBoss`, `UnlockFloor`, `Wanted` (in `Action/AdventureBoss/`)
- **Infinite Tower**: `InfiniteTowerBattle` — progressive dungeon
- **Event Dungeon**: `EventDungeonBattle`, `EventDungeonBattleSweep`

### Skill System (`Model/Skill/`)
Class hierarchy: `Skill` (abstract base) → attack/heal/buff subtypes.

**Skill types** (`SkillType` enum): Attack, Heal, Buff, Debuff

**Attack skills** (inherit `AttackSkill`):
- `NormalAttack` — single target, single hit
- `DoubleAttack` — single target, two hits (supports combo bonus)
- `BlowAttack` — single target blow
- `AreaAttack` — multi-target (all enemies)
- `BuffRemovalAttack` — deals damage + removes target's stat buffs
- `ShatterStrike` — damage based on target's max HP (clamped to `Simulator.ShatterStrikeMaxDamage`)

**Utility skills**: `HealSkill` (HP recovery), `BuffSkill` (applies buffs/debuffs without damage)

**Arena skill variants** (`Model/Skill/Arena/`): Each attack/heal/buff skill has an `Arena*` counterpart for PvP (e.g., `ArenaNormalAttack`, `ArenaHealSkill`).

**Skill categories** (`SkillCategory` enum): NormalAttack, BlowAttack, DoubleAttack, AreaAttack, BuffRemovalAttack, ShatterStrike, Heal, Buff, Debuff, TickDamage, Focus, Dispel, HPBuff, AttackBuff, DefenseBuff, CriticalBuff, HitBuff, SpeedBuff, DamageReductionBuff, CriticalDamageBuff

**Skill target types** (`SkillTargetType` enum): Enemy, Enemies, Self, Ally

**Key classes**: `SkillFactory` (instantiation + deserialization), `SkillCustomField` (custom equipment overrides for duration/value)

### Buff / Debuff System (`Model/Buff/`)
Base class `Buff` → two branches:

**StatBuff** — Modifies character stats (HP, ATK, DEF, CRI, HIT, SPD, DRV, DRR, CDMG). Supports stacking (`MaxStack`), group override (same `GroupId` → highest ID wins).

**ActionBuff** — Special effects:
- `Bleed` — DoT (damage over time)
- `Stun` — prevents actions
- `Vampiric` — heals attacker based on damage dealt (basis point percentage)
- `Focus` — guarantees hit accuracy
- `Dispel` — probability-based debuff resistance
- `IceShield` — protective shield, links to FrostBite via `BuffLinkSheet`

**BuffFactory** creates buffs from sheet data. Buff data comes from `StatBuffSheet` (CSV: `BuffSheet.csv`), `ActionBuffSheet`, `SkillBuffSheet` (maps skill→stat buff IDs), `SkillActionBuffSheet` (maps skill→action buff IDs), `BuffLinkSheet`, `BuffLimitSheet`.

### Item & Equipment (`Model/Item/`)
Hierarchy: `ItemBase` → `Equipment`, `Consumable`, `Material`, `Costume`

**Equipment types**: `Weapon`, `Armor`, `Belt`, `Ring`, `Necklace`, `Aura`, `Grimoire` — support enhancement levels, option stats, and skill attachments.

**Related actions**:
- Crafting: `CombinationEquipment`, `CombinationConsumable`, `CustomEquipmentCraft`
- Enhancement: `ItemEnhancement` (multiple versions: v7–v13)
- Disassembly: `Grinding`, `Synthesize`
- Summoning: `AuraSummon`, `CostumeSummon`, `RuneSummon`
- Slots: `UnlockCombinationSlot`, `RapidCombination`

### Market & Trading
- Sell/buy: `RegisterProduct`, `BuyProduct`, `CancelProductRegistration`, `ReRegisterProduct`
- Legacy: `Sell`, `Buy`, `SellCancellation`, `UpdateSell`
- Models: `Order` (base) → `FungibleOrder`, `NonFungibleOrder`; `Product` → `ItemProduct`, `FavProduct`

### Economy & Staking
- Staking: `Stake`, `ClaimStakeReward` — token locking for rewards
- Assets: `TransferAsset`, `TransferAssets`, `BurnAsset`, `MintAssets`, `IssueToken`
- Rewards: `DailyReward`, `ClaimGifts`, `ClaimItems`, `ClaimPatrolReward`

### Guild System (`Action/Guild/`, `Model/Guild/`)
- Management: `MakeGuild`, `JoinGuild`, `QuitGuild`, `RemoveGuild`, `MoveGuild`
- Moderation: `BanGuildMember`, `UnbanGuildMember`
- Rewards: `ClaimGuildReward`, `ClaimReward`, `ClaimUnbonded`
- Modules: `GuildModule`, `GuildBanModule`, `GuildParticipantModule`, `GuildDelegateeModule`, `GuildMemberCounterModule`

### Validator Delegation (`Action/ValidatorDelegation/`)
PoS blockchain operations: `PromoteValidator`, `DelegateValidator`, `UndelegateValidator`, `SlashValidator`, `UnjailValidator`, `SetValidatorCommission`, `AllocateReward`, `RecordProposer`, `UpdateValidators`

### Avatar & Account
- `CreateAvatar`, `MigrateAgentAvatar`, `RetrieveAvatarAssets`
- `ChargeActionPoint` — restore stamina
- `UnlockWorld`, `UnlockEquipmentRecipe` — progression unlocks

### Garage System (`Action/Garages/`)
Distributed item storage: `LoadIntoMyGarages`, `UnloadFromMyGarages`, `BulkUnloadFromGarages`, `DeliverToOthersGarages`

### Coupons & Codes
- `IssueCoupons`, `RedeemCoupon`, `TransferCoupons` (in `Action/Coupons/`)
- `RedeemCode`, `AddRedeemCode`

### Admin / System
- `InitializeStates`, `PatchTableSheet`, `PatchTableSheetCompressed`
- `SetAddressState`, `RemoveAddressState`, `RenewAdminState`
- `ActivateCollection`, `GrantItems`

### State Modules (`Module/`)
Key modules operating on `IWorld`: `AvatarModule`, `ArenaModule`, `AgentModule`, `ActionPointModule`, `AdventureBossModule`, `CollectionModule`, `CombinationSlotStateModule`, `DailyRewardModule`, `GiftModule`, `InfiniteTowerModule`, `PatrolRewardModule`, `RelationshipModule`, `RuneStateModule`, `LegacyModule` (backwards-compatible state access)

### Table Data & CSV
Game balance data is defined in CSV files (`Lib9c/TableCSV/`) parsed by corresponding `*Sheet` classes (`Lib9c/TableData/`). Key categories:
- **Skill/Buff**: `SkillSheet`, `StatBuffSheet` (BuffSheet.csv), `ActionBuffSheet`, `SkillBuffSheet`, `SkillActionBuffSheet`, `BuffLinkSheet`, `BuffLimitSheet`, `EnemySkillSheet`
- **Item/Equipment**: `EquipmentItemSheet`, `EquipmentItemRecipeSheet`, `EquipmentItemOptionSheet`, `ConsumableItemSheet`, `MaterialItemSheet`, `CostumeItemSheet`
- **Enhancement**: `EnhancementCostSheetV3`, `CrystalEquipmentGrindingSheet`
- **World/Stage**: `StageSheet`, `StageWaveSheet`, `WorldSheet`, `WorldUnlockSheet`
- **Character**: `CharacterSheet`, `CharacterLevelSheet`
- **Summoning**: `EquipmentSummonSheet`, `RuneSummonSheet`, `CostumeSummonSheet`
- **Staking**: `StakeRegularRewardSheet` (V1–V10), `StakePolicySheet`
- **Adventure Boss**: `AdventureBossFloorSheet`, `AdventureBossFloorWaveSheet`
- **Quest**: `CollectQuestSheet`, `CombinationQuestSheet`, `WorldQuestSheet`, etc.
- **Event**: `EventScheduleSheet`, `EventDungeonStageSheet`

## Key Conventions

### Determinism Requirement
All game logic executed on-chain must be deterministic. The `Lib9c.Common.ruleset` enforces warnings for culture-specific operations (SonarAnalyzer rules S1449, S4026, S4056, S4057) that could break consensus.

### Adding a New Action
1. Create the action class in `Lib9c/Action/` inheriting `ActionBase`
2. Add `[ActionType("action_name")]` attribute
3. Implement `PlainValue`, `LoadPlainValue`, and `Execute`
4. Add the type to the `Serialize_With_MessagePack` test in `.Lib9c.Tests/Action/ActionEvaluationTest.cs`
5. Write tests in `.Lib9c.Tests/`

### Adding a New Skill Category
When adding a new attack-type `SkillCategory` enum value:
1. Add the enum value in `Lib9c/Model/Skill/SkillCategory.cs`
2. Create skill class + arena variant (`Model/Skill/`, `Model/Skill/Arena/`)
3. Create battle status class + arena variant (`Model/BattleStatus/`, `Model/BattleStatus/Arena/`)
4. Add cases to all 3 switch blocks in `SkillFactory.cs` (`Get`, `GetV1`, `GetForArena`)
5. Add `Co*` interface methods to `IStage.cs` and `IArena.cs`
6. **Add to `OnPostSkill` attack skill filter** in both `CharacterBase.cs` and `ArenaCharacter.cs` — this is the Vampiric/Bleed post-processing filter and is easy to miss

### Code Documentation
All code changes and additions require English XML documentation comments (`/// <summary>`).

### Tests
- All tests go in `.Lib9c.Tests/`
- Always run tests after writing them to verify correctness
- All prior tests must continue to pass

### Code Style
- 4-space indentation, Allman brace style, 100-char line limit
- Public members: first-word capitalized
- Analysis enforced: SonarAnalyzer, StyleCop, Libplanet.Analyzers

### Action Versioning
Actions are versioned (e.g., `ItemEnhancement7` through `ItemEnhancement13`) for backward compatibility. New versions coexist with old ones; use the latest version for new code.

### Branching
- Base branch: `development`
- Branch naming: `{feature|bugfix}/{description}/{optional-suffix}`

### Local Libplanet Development
Set `LibplanetDirectory` in `Directory.Build.props` to use a local Libplanet checkout. Do not commit this change.
