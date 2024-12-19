import { describe } from "vitest";
import { HackAndSlash, RuneSlotInfo, uuidToGuidBytes } from "../../src/index.js";
import { runTests } from "./common.js";
import { avatarAddress } from "./fixtures.js";

describe("HackAndSlash", () => {
  describe("odin", () => {
    runTests("valid case", [
      new HackAndSlash({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
        worldId: BigInt(1),
        stageId: BigInt(1),
        stageBuffId: null,
        apStoneCount: BigInt(1),
        totalPlayCount: BigInt(1),
        costumes: [uuidToGuidBytes("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")],
        equipments: [uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")],
        foods: [uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")],
        runeInfos: [new RuneSlotInfo(BigInt(1), BigInt(1))],
      }),
    ]);
  });
});
