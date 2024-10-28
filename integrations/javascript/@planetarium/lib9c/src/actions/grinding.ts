import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `Grinding` action.
 */
export type GrindingArgs = {
  avatarAddress: Address;
  equipmentIds: Array<Uint8Array>;
  chargeAp: boolean;
} & GameActionArgs;

export class Grinding extends GameAction {
  protected readonly type_id: string = "grinding2";

  public readonly avatarAddress: Address;
  public readonly equipmentIds: Array<Uint8Array>;
  public readonly chargeAp: boolean;

  constructor({ avatarAddress, equipmentIds, chargeAp, id }: GrindingArgs) {
    super({ id });
    this.avatarAddress = avatarAddress;
    this.equipmentIds = equipmentIds;
    this.chargeAp = chargeAp;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([
      ["a", this.avatarAddress.toBytes()],
      ["e", this.equipmentIds.map((id) => id)],
      ["c", this.chargeAp],
    ]);
  }
}
