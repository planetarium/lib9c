import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `Stake` action.
 */
export type StakeArgs = {
  amount: bigint;
  avatarAddress: Address | null;
} & GameActionArgs;

/**
 * The `Stake` action is used to stake the NCG.
 */
export class Stake extends GameAction {
  protected readonly type_id: string = "stake3";

  /**
   * The amount of NCG to stake.
   */
  public readonly amount: bigint;

  /**
   * The address of avatar to claim reward.
   */
  public readonly avatarAddress: Address | null;

  /**
   * Create a new `Stake` action.
   * @param params The arguments of the `Stake` action.
   */
  constructor({ amount, avatarAddress, id }: StakeArgs) {
    super({ id });

    this.amount = amount;
    this.avatarAddress = avatarAddress || null;
  }

  protected plain_value_internal(): Dictionary {
    if (this.avatarAddress == null) {
      return new BencodexDictionary([["am", this.amount]]);
    }
    return new BencodexDictionary([
      ["am", this.amount],
      ["saa", this.avatarAddress.toBytes()],
    ]);
  }
}
