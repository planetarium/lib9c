import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Value } from "@planetarium/bencodex";
import { PolymorphicAction } from "./common.js";

export type MigrateDelegationArgs = {
  target: Address;
};

export class MigrateDelegation extends PolymorphicAction {
  protected readonly type_id: string = "migrate_delegation";

  private readonly target: Address;

  constructor({ target }: MigrateDelegationArgs) {
    super();

    this.target = target;
  }

  protected plain_value(): Value {
    const targetKey = "t" as const;

    return new BencodexDictionary([[targetKey, this.target.toBytes()]]);
  }
}
