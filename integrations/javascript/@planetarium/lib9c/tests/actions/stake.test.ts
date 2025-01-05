import { describe } from "vitest";
import { Stake } from "../../src/index.js";
import { runTests } from "./common.js";
import { avatarAddress } from "./fixtures.js";

describe("Stake", () => {
  runTests("valid case", [
    new Stake({
      amount: 1n,
      avatarAddress: avatarAddress,
    }),
    new Stake({
      amount: 1n,
      avatarAddress: null,
    }),
  ]);
});
