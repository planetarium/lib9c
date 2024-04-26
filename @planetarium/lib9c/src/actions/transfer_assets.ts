import type { Address } from "@planetarium/account";
import {
  BencodexDictionary,
  type Dictionary,
  type Key,
  type Value,
} from "@planetarium/bencodex";
import {
  type FungibleAssetValue,
  encodeFungibleAssetValue,
} from "@planetarium/tx";
import { PolymorphicAction } from "./common.js";

export class TransferAssets extends PolymorphicAction {
  protected readonly type_id: string = "transfer_assets3";

  public readonly sender: Address;
  public readonly recipients: [Address, FungibleAssetValue][];
  public readonly memo: string | null;

  constructor({
    sender,
    recipients,
    memo,
  }: {
    sender: Address;
    recipients: [Address, FungibleAssetValue][];
    memo?: string;
  }) {
    super();

    this.sender = sender;
    this.recipients = recipients;
    this.memo = memo || null;
  }

  protected plain_value(): Dictionary {
    const params: [Key, Value][] = [
      ["sender", this.sender.toBytes()],
      [
        "recipients",
        this.recipients.map(([address, amount]) => [
          address.toBytes(),
          encodeFungibleAssetValue(amount),
        ]),
      ],
    ];

    if (this.memo !== null) {
      params.push(["memo", this.memo]);
    }

    return new BencodexDictionary(params);
  }
}
