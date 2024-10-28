import { NCG, TransferAsset, fav } from "../src/index.js";
import { Address } from "@planetarium/account";
import { Buffer } from "buffer";

const action = new TransferAsset({
  sender: Address.fromHex("0x2cBaDf26574756119cF705289C33710F27443767"),
  recipient: Address.fromHex("0x2cBaDf26574756119cF705289C33710F27443767"),
  amount: fav(NCG, 2),
});

const bytes = action.serialize();
console.log(Buffer.from(bytes).toString("hex"));
