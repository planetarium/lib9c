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

/**
 * The arguments of the `TransferAssets` action.
 */
export type TransferAssetsArgs = {
  /**
   * The sender of the fungible assets. It must be the same with the signer.
   */
  sender: Address;

  /**
   * The list of pairs where the first element is the recipient's address and the second element is the amount of fungible assets to transfer.
   */
  recipients: [Address, FungibleAssetValue][];

  /**
   * A memo to be attached to the @see TransferAssets action.
   */
  memo?: string;
};

/**
 * The `TransferAssets` action is used to transfer fungible assets from one address to multiple recipients at once.
 */
export class TransferAssets extends PolymorphicAction {
  protected readonly type_id: string = "transfer_assets3";

  public readonly sender: Address;
  public readonly recipients: [Address, FungibleAssetValue][];
  public readonly memo: string | null;

  /**
   * Create a new `TransferAssets` action.
   * @param params The parameters to create the action.
   * @example Send 2 NCG to two addresses:
   * ```typescript
   * const agentAddresses = [
   *   Address.fromHex("0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa", true),
   *   Address.fromHex("0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda", true),
   * ];
   * new TransferAssets({
   *   sender: agentAddress,
   *   recipients: agentAddresses.map((address) => [address, fav(NCG, 2)])
   * }),
   * ```
   */
  constructor({ sender, recipients, memo }: TransferAssetsArgs) {
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
