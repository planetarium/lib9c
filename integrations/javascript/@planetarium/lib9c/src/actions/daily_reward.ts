import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

export type DailyRewardArgs = {
  avatarAddress: Address;
} & GameActionArgs;

/**
 * The `DailyReward` action is used to claim the daily reward.
 */
export class DailyReward extends GameAction {
  protected readonly type_id: string = "daily_reward7";

  /**
   * The address of the avatar to claim the daily reward.
   */
  public readonly avatarAddress: Address;

  /**
   * Create a new `DailyReward` action.
   * @param params The arguments of the `DailyReward` action.
   */
  constructor({ avatarAddress, id }: DailyRewardArgs) {
    super({ id });

    this.avatarAddress = avatarAddress;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([["a", this.avatarAddress.toBytes()]]);
  }
}
