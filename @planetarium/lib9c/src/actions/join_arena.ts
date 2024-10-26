import type { Address } from "@planetarium/account";
import {
  BencodexDictionary,
  type Dictionary,
  type Value,
} from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

export type JoinArenaArgs = {
  avatarAddress: Address;
  championshipId: bigint;
  round: bigint;
  costumes: Array<Uint8Array>;
  equipments: Array<Uint8Array>;
  runeInfos: Array<RuneSlotInfo>;
} & GameActionArgs;

export class RuneSlotInfo {
  public readonly slotIndex: bigint;
  public readonly runeId: bigint;

  constructor(slotIndex: bigint, runeId: bigint) {
    this.slotIndex = slotIndex;
    this.runeId = runeId;
  }

  public serialize(): Value[] {
    return [this.slotIndex.toString(), this.runeId.toString()];
  }

  static fromSerialized(data: {
    slotIndex: string;
    runeId: string;
  }): RuneSlotInfo {
    return new RuneSlotInfo(BigInt(data.slotIndex), BigInt(data.runeId));
  }
}

export class JoinArena extends GameAction {
  protected readonly type_id: string = "join_arena4";

  public readonly avatarAddress: Address;
  public readonly championshipId: bigint;
  public readonly round: bigint;
  public readonly costumes: Array<Uint8Array>;
  public readonly equipments: Array<Uint8Array>;
  public readonly runeInfos: Array<RuneSlotInfo>;

  constructor({
    avatarAddress,
    championshipId,
    round,
    costumes,
    equipments,
    runeInfos,
    id,
  }: JoinArenaArgs) {
    super({ id });
    this.avatarAddress = avatarAddress;
    this.championshipId = championshipId;
    this.round = round;
    this.costumes = costumes;
    this.equipments = equipments;
    this.runeInfos = runeInfos;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([
      ["avatarAddress", this.avatarAddress.toBytes()],
      ["championshipId", this.championshipId.toString()],
      ["round", this.round.toString()],
      ["costumes", this.costumes.map((costume) => costume)],
      ["equipments", this.equipments.map((equipment) => equipment)],
      ["runeInfos", this.runeInfos.map((rune) => rune.serialize())],
    ]);
  }
}
