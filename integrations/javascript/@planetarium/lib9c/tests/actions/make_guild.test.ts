import { describe } from "vitest";
import { MakeGuild } from "../../src/index.js";
import { runTests } from "./common.js";
import { validatorAddress } from "./fixtures.js";

describe("MakeGuild", () => {
  runTests("valid case", [
    new MakeGuild({
      validatorAddress: validatorAddress,
    }),
  ]);
});
