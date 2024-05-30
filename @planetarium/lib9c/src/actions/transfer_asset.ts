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
import { GameAction, type GameActionArgs } from "./common.js";

/**
 * The arguments of the `TransferAsset` action.
 */
export type TransferAssetArgs = {
  /**
   * The sender of the fungible asset. It must be the same with the signer.
   */
  sender: Address;

  /**
   * The recipient of the fungible asset.
   */
  recipient: Address;

  /**
   * The amount of the fungible asset to transfer.
   */
  amount: FungibleAssetValue;

  /**
   * A memo to be attached to the @see TransferAsset action.
   * In the WNCG bridge case, it should have an Ethereum address to receive WNCG.
   */
  memo?: string;
} & GameActionArgs;

/**
 * The `TransferAsset` action is used to transfer a fungible asset from one address to another.
 */
export class TransferAsset extends GameAction {
  protected readonly type_id: string = "transfer_asset5";

  public readonly sender: Address;
  public readonly recipient: Address;
  public readonly amount: FungibleAssetValue;
  public readonly memo: string | null;

  /**
   * Create a new `TransferAsset` action.
   * @param params The parameters to create the action.
   * @example
   * ```typescript
   * new TransferAsset({
   *   sender: Address.fromHex('0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda', true),
   *   recipient: Address.fromHex('0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa', true),
   *   amount: fav(NCG, 2),
   * })
   * ```
   */
  constructor({ sender, recipient, amount, memo, id }: TransferAssetArgs) {
    super({ id });

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
