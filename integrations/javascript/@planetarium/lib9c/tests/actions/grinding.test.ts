import { describe } from "vitest";
import { Grinding, uuidToGuidBytes } from "../../src/index.js";

import { runTests } from "./common.js";
import { avatarAddress } from "./fixtures.js";

describe("Grinding", () => {
  describe("grinding", () => {
    runTests("valid case", [
      new Grinding({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
        equipmentIds: [
          uuidToGuidBytes("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
          uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ],
        chargeAp: false,
      }),
    ]);
  });

  describe("AP charge", () => {
    runTests("valid case", [
      new Grinding({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
        equipmentIds: [
          uuidToGuidBytes("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
          uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ],
        chargeAp: true,
      }),
    ]);
  });
});
