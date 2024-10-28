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
import type { HashDigest } from "../models/hashdigest.js";
import { GameAction, type GameActionArgs } from "./common.js";

export type DeliverToOtherGaragesArgs = {
  /**
   * The address of the recipient's agent.
   */
  recipientAgentAddress: Address;

  /**
   * The list of fungible asset values to deliver to the recipient's from signer's garages.
   */
  fungibleAssetValues?: FungibleAssetValue[];

  /**
   * The list of pairs where the first element is the item's id and the second element is the amount of it. These will be delivered to the recipient's from signer's garages.
   */
  fungibleIdAndCounts?: [HashDigest<"SHA256">, bigint][];

  /**
   * A memo to be attached to the @see DeliverToOtherGarages action. If it is not provided, it is set to `null`.
   */
  memo?: string;
} & GameActionArgs;

/**
 * The `DeliverToOtherGarages` action is used to deliver fungible assets to other garages.
 * @see LoadIntoMyGarages
 */
export class DeliverToOtherGarages extends GameAction {
  protected readonly type_id: string = "deliver_to_others_garages";

  public readonly recipientAgentAddress: Address;
  public readonly fungibleAssetValues: FungibleAssetValue[] | null;
  public readonly fungibleIdAndCounts: [HashDigest<"SHA256">, bigint][] | null;
  public readonly memo: string | null;

  /**
   * Create a new `DeliverToOtherGarages` action.
   * @param params The arguments of the `DeliverToOtherGarages` action.
   */
  constructor({
    recipientAgentAddress,
    fungibleAssetValues,
    fungibleIdAndCounts,
    memo,
    id,
  }: DeliverToOtherGaragesArgs) {
    super({ id });

    this.recipientAgentAddress = recipientAgentAddress;
    this.fungibleAssetValues = fungibleAssetValues || null;
    this.fungibleIdAndCounts = fungibleIdAndCounts || null;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "l",
        [
          this.recipientAgentAddress.toBytes(),
          this.fungibleAssetValues !== null
            ? this.fungibleAssetValues.map((x) => encodeFungibleAssetValue(x))
            : null,
          this.fungibleIdAndCounts !== null
            ? this.fungibleIdAndCounts.map((xs) => [xs[0].toBytes(), xs[1]])
            : null,
          this.memo,
        ],
      ],
    ];

    return new BencodexDictionary(params);
  }
}
