import { describe } from "vitest";
import { MigrateDelegation } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress } from "./fixtures.js";

describe("MigrateDelegation", () => {
  runTests("valid case", [
    new MigrateDelegation({
      target: agentAddress,
    }),
  ]);
});
