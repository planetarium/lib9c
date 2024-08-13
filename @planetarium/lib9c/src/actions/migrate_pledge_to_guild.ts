import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Value } from "@planetarium/bencodex";
import { PolymorphicAction } from "./common.js";

export type MigratePledgeToGuildArgs = {
  target: Address;
};

export class MigratePledgeToGuild extends PolymorphicAction {
  protected readonly type_id: string = "migrate_pledge_to_guild";

  private readonly target: Address;

  constructor({ target }: MigratePledgeToGuildArgs) {
    super();

    this.target = target;
  }

  protected plain_value(): Value {
    const targetKey = "t" as const;

    return new BencodexDictionary([[targetKey, this.target.toBytes()]]);
  }
}
