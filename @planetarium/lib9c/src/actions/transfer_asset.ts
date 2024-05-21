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
import { GameAction } from "./common.js";

export class TransferAsset extends GameAction {
  protected readonly type_id: string = "transfer_asset5";

  public readonly sender: Address;
  public readonly recipient: Address;
  public readonly amount: FungibleAssetValue;
  public readonly memo: string | null;

  constructor({
    sender,
    recipient,
    amount,
    memo,
    id,
  }: {
    sender: Address;
    recipient: Address;
    amount: FungibleAssetValue;
    memo?: string;
    id?: Uint8Array;
  }) {
    super(id);

    this.sender = sender;
    this.recipient = recipient;
    this.amount = amount;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      ["sender", this.sender.toBytes()],
      ["recipient", this.recipient.toBytes()],
      ["amount", encodeFungibleAssetValue(this.amount)],
    ];

    if (this.memo !== null) {
      params.push(["memo", this.memo]);
    }

    return new BencodexDictionary(params);
  }
}
