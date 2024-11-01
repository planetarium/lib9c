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
 * The list of claim data pairs.
 * The first element of the pair is the address of the avatar to claim the item,
 * and the second element is the list of the amount of item tokens to convert.
 */
export type ClaimData = [Address, FungibleAssetValue[]][];

/**
 * The arguments of the `ClaimItems` action.
 */
export type ClaimItemsArgs = {
  /**
   * The payload of the action.
   */
  claimData: ClaimData;

  /**
   * The memo of the action. If it is not provided, it is set to `null`.
   */
  memo?: string;
} & GameActionArgs;

/**
 * The `ClaimItems` action is used to convert an item token to an item.
 */
export class ClaimItems extends GameAction {
  protected readonly type_id: string = "claim_items";

  /**
   * The payload of the action.
   */
  public readonly claimData: ClaimData;

  /**
   * The memo of the action.
   */
  public readonly memo: string | null;

  /**
   * Create a new `ClaimItems` action.
   * @param params The arguments of the `ClaimItems` action.
   * @example
   * ```typescript
   * new ClaimItems({
        claimData: [
          [
            Address.fromHex("0x2cBaDf26574756119cF705289C33710F27443767"),
            [
              fav(
                {
                  ticker: "FAV__CRYSTAL",
                  decimalPlaces: 18,
                  minters: null,
                  maximumSupply: null,
                  totalSupplyTrackable: false,
                },
                1,
              ),
            ],
          ],
        ],
      })
   * ```
   */
  constructor({ claimData, memo, id }: ClaimItemsArgs) {
    super({ id });

    this.claimData = claimData;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "cd",
        this.claimData.map(([address, fungibleAssetValues]) => [
          address.toBytes(),
          fungibleAssetValues.map((fav) => encodeFungibleAssetValue(fav)),
        ]),
      ],
    ];

    if (this.memo !== null) {
      params.push(["m", this.memo]);
    }
    return new BencodexDictionary(params);
  }
}
