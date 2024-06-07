import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `Stake` action.
 */
export type StakeArgs = {
  amount: bigint;
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
   * Create a new `Stake` action.
   * @param params The arguments of the `Stake` action.
   */
  constructor({ amount, id }: StakeArgs) {
    super({ id });

    this.amount = amount;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([["am", this.amount]]);
  }
}
