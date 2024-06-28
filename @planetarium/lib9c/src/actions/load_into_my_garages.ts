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

/**
 * The arguments of the `LoadIntoMyGarages` action.
 */
export type LoadIntoMyGaragesArgs = {
  /**
   * The address of the avatar to load the items.
   */
  avatarAddress?: Address;

  /**
   * The list of pairs where the first element is 'fungible id', the item's hash and the second element is the amount to load into garages.
   */
  fungibleIdAndCounts?: [HashDigest<"SHA256">, bigint][];

  /**
   * The list of pairs where the first element is the address to load into garages from and the second element is the how many fungible asset value to load into garages from the specified address.
   */
  fungibleAssetValues?: [Address, FungibleAssetValue][];

  /**
   * The memo of the action. If it is not provided, it is set to `null`.
   */
  memo?: string;
} & GameActionArgs;

/**
 * The `LoadIntoMyGarages` action is used to load the fungible assets and items into the signer's garages.
 * @see DeliverToOtherGarages
 */
export class LoadIntoMyGarages extends GameAction {
  protected readonly type_id: string = "load_into_my_garages";

  public readonly avatarAddress: Address | null;
  public readonly fungibleAssetValues: [Address, FungibleAssetValue][] | null;
  public readonly fungibleIdAndCounts: [HashDigest<"SHA256">, bigint][] | null;
  public readonly memo: string | null;

  /**
   * Create a new `LoadIntoMyGarages` action.
   * @param params The arguments of the `LoadIntoMyGarages` action.
   * @example To load NCG into the signer's garages:
   * ```typescript
   * new LoadIntoMyGarages({
   *   fungibleAssetValues: [[agentAddress, fav(NCG, 2)]],
   * })
   * ```
   * @example To load items into the signer's garages:
   * ```typescript
   * new LoadIntoMyGarages({
   *   avatarAddress,
   *   fungibleIdAndCounts: [[fungibleIdA, 1n]],
   * })
   * ```
   */
  constructor({
    avatarAddress,
    fungibleAssetValues,
    fungibleIdAndCounts,
    memo,
    id,
  }: LoadIntoMyGaragesArgs) {
    super({ id });

    this.avatarAddress = avatarAddress || null;
    this.fungibleAssetValues = fungibleAssetValues || null;
    this.fungibleIdAndCounts = fungibleIdAndCounts || null;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "l",
        [
          this.fungibleAssetValues !== null
            ? this.fungibleAssetValues.map((xs) => [
                xs[0].toBytes(),
                encodeFungibleAssetValue(xs[1]),
              ])
            : null,
          this.avatarAddress !== null ? this.avatarAddress?.toBytes() : null,
          this.fungibleIdAndCounts !== null
            ? this.fungibleIdAndCounts?.map((xs) => [xs[0].toBytes(), xs[1]])
            : null,
          this.memo,
        ],
      ],
    ];

    return new BencodexDictionary(params);
  }
}
