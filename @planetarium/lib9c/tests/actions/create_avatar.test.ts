import { describe } from "vitest";
import { uuidToGuidBytes } from "../../src/actions/common.js";
import { CreateAvatar } from "../../src/actions/create_avatar.js";
import { runTests } from "./common.js";

describe("CreateAvatar", () => {
  describe("odin", () => {
    runTests("valid case", [
      new CreateAvatar({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        index: 0n,
        hair: 0n,
        ear: 0n,
        lens: 0n,
        tail: 0n,
        name: "gae-ddong-i",
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new CreateAvatar({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        index: 0n,
        hair: 0n,
        ear: 0n,
        lens: 0n,
        tail: 0n,
        name: "gae-ddong-i",
      }),
    ]);
  });
});
