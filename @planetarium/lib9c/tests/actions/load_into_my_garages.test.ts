import { describe } from "vitest";
import {
  LoadIntoMyGarages,
  MINTERLESS_NCG,
  NCG,
  fav,
} from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress, avatarAddress, fungibleIdA } from "./fixtures.js";

describe("LoadIntoMyGarages", () => {
  describe("odin", () => {
    runTests("valid case", [
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(NCG, 2)]],
      }),
      new LoadIntoMyGarages({
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(NCG, 2)]],
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      // With memo
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(NCG, 2)]],
        memo: "MEMO",
      }),
      new LoadIntoMyGarages({
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(NCG, 2)]],
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(MINTERLESS_NCG, 2)]],
      }),
      new LoadIntoMyGarages({
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(MINTERLESS_NCG, 2)]],
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      // With memo
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(MINTERLESS_NCG, 2)]],
        memo: "MEMO",
      }),
      new LoadIntoMyGarages({
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
      new LoadIntoMyGarages({
        fungibleAssetValues: [[agentAddress, fav(MINTERLESS_NCG, 2)]],
        avatarAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
    ]);
  });
});
