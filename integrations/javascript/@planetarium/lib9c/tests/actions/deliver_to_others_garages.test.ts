import { describe } from "vitest";
import {
  DeliverToOtherGarages,
  MINTERLESS_NCG,
  NCG,
  fav,
} from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress, fungibleIdA } from "./fixtures.js";

describe("DeliverToOthersGarages", () => {
  describe("odin", () => {
    runTests("valid case", [
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(NCG, 2)],
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(NCG, 2)],
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      // With memo
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(NCG, 2)],
        memo: "MEMO",
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(NCG, 2)],
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(MINTERLESS_NCG, 2)],
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(MINTERLESS_NCG, 2)],
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
      }),
      // With memo
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(MINTERLESS_NCG, 2)],
        memo: "MEMO",
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
      new DeliverToOtherGarages({
        recipientAgentAddress: agentAddress,
        fungibleAssetValues: [fav(MINTERLESS_NCG, 2)],
        fungibleIdAndCounts: [[fungibleIdA, 1n]],
        memo: "MEMO",
      }),
    ]);
  });
});
