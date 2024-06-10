import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction } from "./common.js";

export class ClaimStakeReward extends GameAction {
  protected readonly type_id: string = "claim_stake_reward9";

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
    return new BencodexDictionary([["aa", this.avatarAddress.toBytes()]]);
  }
}
