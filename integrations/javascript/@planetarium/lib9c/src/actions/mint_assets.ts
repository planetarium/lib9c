import { Buffer } from "buffer";
import type { Address } from "@planetarium/account";
import type { Value } from "@planetarium/bencodex";
import { type FungibleAssetValue, encodeCurrency } from "@planetarium/tx";
import type { HashDigest } from "../models/hashdigest.js";
import { PolymorphicAction } from "./common.js";

interface IFungibleAssetValues {
  recipient: Address;
  amount: FungibleAssetValue;
}

type FungibleItemId = HashDigest<"SHA256">;
interface IFungibleItems {
  recipient: Address;
  fungibleItemId: FungibleItemId;
  count: bigint;
}

export type MintSpec =
  | IFungibleAssetValues
  | IFungibleItems
  | (IFungibleAssetValues & IFungibleItems);
function encodeMintSpec(value: IFungibleAssetValues | IFungibleItems): Value {
  if ((value as IFungibleAssetValues).amount !== undefined) {
    const favs = value as IFungibleAssetValues;
    return [
      favs.recipient.toBytes(),
      [encodeCurrency(favs.amount.currency), favs.amount.rawValue],
      null,
    ];
  }

  // else
  const fis = value as IFungibleItems;
  return [
    fis.recipient.toBytes(),
    null,
    [fis.fungibleItemId.toBytes(), fis.count],
  ];
}

export type MintAssetsArgs = {
  mintSpecs: MintSpec[];
  memo: string | null;
};

export class MintAssets extends PolymorphicAction {
  protected readonly type_id: string = "mint_assets";

  private readonly mintSpecs: (IFungibleAssetValues | IFungibleItems)[];
  private readonly memo: string | null;

  constructor({ mintSpecs, memo }: MintAssetsArgs) {
    super();

    this.mintSpecs = mintSpecs;
    this.memo = memo;
  }

  protected plain_value(): Value {
    return [this.memo, ...this.mintSpecs.map(encodeMintSpec)];
  }
}
