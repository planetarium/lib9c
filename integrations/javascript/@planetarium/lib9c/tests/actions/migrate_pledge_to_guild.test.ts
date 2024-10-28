import { describe } from "vitest";
import { MigratePledgeToGuild } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress } from "./fixtures.js";

describe("MigratePledgeToGuild", () => {
  describe("odin", () => {
    runTests("valid case", [
      new MigratePledgeToGuild({
        target: agentAddress,
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new MigratePledgeToGuild({
        target: agentAddress,
      }),
    ]);
  });
});
