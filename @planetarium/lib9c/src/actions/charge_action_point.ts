import type { Address } from "@planetarium/account";
import { BencodexDictionary, Dictionary, type Value } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `ChargeActionPoint` action.
 */
export type ChargeActionPointArgs = {
  avatarAddress: Address;
} & GameActionArgs;

/**
 * The `ChargeActionPoint` action is used to charge action points for an avatar.
 */
export class ChargeActionPoint extends GameAction {
  protected readonly type_id: string = "charge_action_point3";

  public readonly avatarAddress: Address;

  /**
   * Create a new `ChargeActionPoint` action.
   * @param params The arguments of the `ChargeActionPoint` action.
   */
  constructor({ avatarAddress, id }: ChargeActionPointArgs) {
    super({ id });
    this.avatarAddress = avatarAddress;
  }

  /**
   * Serialize the action data to Bencodex format.
   */
  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([
      ["avatarAddress", this.avatarAddress.toBytes()],
    ]);
}
}