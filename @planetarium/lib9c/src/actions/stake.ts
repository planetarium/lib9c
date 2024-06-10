import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction } from "./common.js";

export class Stake extends GameAction {
  protected readonly type_id: string = "stake3";

  public readonly amount: bigint;

  constructor({
    amount,
    id,
  }: {
    amount: bigint;
    id?: Uint8Array;
  }) {
    super(id);

    this.amount = amount;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([["am", this.amount]]);
  }
}
