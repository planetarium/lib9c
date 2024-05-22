import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction } from "./common.js";

export class DailyReward extends GameAction {
  protected readonly type_id: string = "daily_reward7";

  public readonly avatarAddress: Address;

  constructor({
    avatarAddress,
    id,
  }: {
    avatarAddress: Address;
    id?: Uint8Array;
  }) {
    super(id);

    this.avatarAddress = avatarAddress;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([["a", this.avatarAddress.toBytes()]]);
  }
}
