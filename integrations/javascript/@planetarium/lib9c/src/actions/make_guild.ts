import type { Address } from "@planetarium/account";
import { BencodexDictionary, type Value } from "@planetarium/bencodex";
import { PolymorphicAction } from "./common.js";

export type MakeGuildArgs = {
  validatorAddress: Address;
};

export class MakeGuild extends PolymorphicAction {
  protected readonly type_id: string = "make_guild";

  private readonly validatorAddress: Address;

  constructor({ validatorAddress }: MakeGuildArgs) {
    super();

    this.validatorAddress = validatorAddress;
  }

  protected plain_value(): Value {
    const validatorAddressKey = "va" as const;

    return new BencodexDictionary([
      [validatorAddressKey, this.validatorAddress.toBytes()],
    ]);
  }
}
