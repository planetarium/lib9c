import { BencodexDictionary, Value, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";
import type { Address } from "@planetarium/account";
import { CreateAvatarArgs } from "./create_avatar.js";
import { RuneSlotInfo } from "../models/rune_slot_info.js";


export type HackAndSlashArgs = {
  costumes: Array<Uint8Array>;
  equipments: Array<Uint8Array>;
  foods: Array<Uint8Array>;
  runeInfos: Array<RuneSlotInfo>;
  worldId: bigint;
  stageId: bigint;
  stageBuffId: bigint | null;
  avatarAddress:Address;
  totalPlayCount:bigint;
  apStoneCount:bigint;
} & GameActionArgs;

export class HackAndSlash extends GameAction {
  protected readonly type_id: string = "hack_and_slash22";

  public readonly avatarAddress: Address;
  public readonly worldId: bigint;
  public readonly stageId: bigint;
  public readonly costumes: Array<Uint8Array>;
  public readonly equipments: Array<Uint8Array>;
  public readonly foods: Array<Uint8Array>;
  public readonly totalPlayCount: bigint;
  public readonly apStoneCount: bigint;
  public readonly runeInfos: Array<RuneSlotInfo>;
  public readonly stageBuffId: bigint | null;


  constructor({ avatarAddress, worldId, stageId, equipments, foods, totalPlayCount, apStoneCount, runeInfos, stageBuffId, costumes, id }: HackAndSlashArgs) {
    super({ id });
    this.costumes = costumes;
    this.avatarAddress = avatarAddress;
    this.worldId = worldId;
    this.stageId = stageId;
    this.equipments = equipments;
    this.foods = foods;
    this.totalPlayCount = totalPlayCount;
    this.apStoneCount = apStoneCount;
    this.runeInfos = runeInfos;
    this.stageBuffId = stageBuffId;
  }

  protected plain_value_internal(): Dictionary {
    var data = new BencodexDictionary([
      ["avatarAddress", this.avatarAddress.toBytes()],
      ["equipments", this.equipments.map((equipment) => equipment)],
      ["costumes", this.costumes.map((costume) => costume)],
      ["foods", this.foods.map((food) => food)],
      ["r", this.runeInfos.map((rune) => rune.serialize())],
      ["worldId", this.worldId.toString()],
      ["stageId", this.stageId.toString()],
      ["totalPlayCount", this.totalPlayCount.toString()],
      ["apStoneCount", this.apStoneCount.toString()]
    ]);

    if (this.stageBuffId !== null) {
      (data as any)["stageBuffId"] = this.stageBuffId.toString();
    }

 
    return data
  }



}


//stageBuffId