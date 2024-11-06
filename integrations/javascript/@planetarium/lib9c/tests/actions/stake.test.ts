import { describe } from "vitest";
import { Stake } from "../../src/index.js";
import { runTests } from "./common.js";

describe("Stake", () => {
  runTests("valid case", [
    new Stake({
      amount: 1n,
    }),
  ]);
});
