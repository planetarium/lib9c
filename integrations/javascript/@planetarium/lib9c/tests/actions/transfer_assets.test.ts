import { describe } from "vitest";
import { MINTERLESS_NCG, NCG, TransferAssets, fav } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress } from "./fixtures.js";

describe("TransferAssets", () => {
  describe("odin", () => {
    runTests("valid case", [
      new TransferAssets({
        sender: agentAddress,
        recipients: [],
      }),
      new TransferAssets({
        sender: agentAddress,
        recipients: [[agentAddress, fav(NCG, 2)]],
      }),
      new TransferAssets({
        sender: agentAddress,
        recipients: [
          [agentAddress, fav(NCG, 2)],
          [agentAddress, fav(NCG, 2)],
          [agentAddress, fav(NCG, 2)],
        ],
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new TransferAssets({
        sender: agentAddress,
        recipients: [],
      }),
      new TransferAssets({
        sender: agentAddress,
        recipients: [[agentAddress, fav(MINTERLESS_NCG, 2)]],
      }),
      new TransferAssets({
        sender: agentAddress,
        recipients: [
          [agentAddress, fav(MINTERLESS_NCG, 2)],
          [agentAddress, fav(MINTERLESS_NCG, 2)],
          [agentAddress, fav(MINTERLESS_NCG, 2)],
        ],
      }),
    ]);
  });
});
