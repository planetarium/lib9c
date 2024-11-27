import { describe } from "vitest";
import { MINTERLESS_NCG, NCG, TransferAsset, fav } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress } from "./fixtures.js";

describe("TransferAsset", () => {
  describe("odin", () => {
    runTests("valid case", [
      new TransferAsset({
        sender: agentAddress,
        recipient: agentAddress,
        amount: fav(NCG, 2),
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new TransferAsset({
        sender: agentAddress,
        recipient: agentAddress,
        amount: fav(MINTERLESS_NCG, 2),
      }),
    ]);
  });
});
