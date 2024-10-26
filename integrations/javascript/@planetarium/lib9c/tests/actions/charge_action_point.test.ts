import { describe } from "vitest";
import { ChargeActionPoint, uuidToGuidBytes } from "../../src/index.js";
import { runTests } from "./common.js";
import { avatarAddress } from "./fixtures.js";

describe("ChargeActionPoint", () => {
  describe("odin", () => {
    runTests("valid case", [
      new ChargeActionPoint({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new ChargeActionPoint({
        id: uuidToGuidBytes("97b69171-6482-4e11-b2c9-1c671615321b"),
        avatarAddress,
      }),
    ]);
  });
});
