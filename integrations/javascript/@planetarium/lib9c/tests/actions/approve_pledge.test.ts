import { describe } from "vitest";
import { ApprovePledge } from "../../src/index.js";
import { runTests } from "./common.js";
import { patronAddress } from "./fixtures.js";

describe("ApprovePledge", () => {
  describe("odin", () => {
    runTests("valid case", [
      new ApprovePledge({
        patronAddress,
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new ApprovePledge({
        patronAddress,
      }),
    ]);
  });
});
