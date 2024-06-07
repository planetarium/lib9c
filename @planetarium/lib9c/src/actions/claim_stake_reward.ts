import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `ClaimStakeReward` action.
 */
export type ClaimStakeRewardArgs = {
  /**
   * The address of the avatar to claim the stake reward.
   */
  avatarAddress: Address;
} & GameActionArgs;

export class ClaimStakeReward extends GameAction {
  protected readonly type_id: string = "claim_stake_reward9";

  /**
   * The address of the avatar to claim the stake reward.
   */
  public readonly avatarAddress: Address;

  /**
   * Create a new `ClaimStakeReward` action.
   * @param params The arguments of the `ClaimStakeReward` action.
   */
  constructor({ avatarAddress, id }: ClaimStakeRewardArgs) {
    super({ id });

    this.avatarAddress = avatarAddress;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([["aa", this.avatarAddress.toBytes()]]);
  }
}
