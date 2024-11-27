import { describe } from "vitest";
import { JoinArena, RuneSlotInfo, uuidToGuidBytes } from "../../src/index.js";
import { runTests } from "./common.js";
import { avatarAddress } from "./fixtures.js";

describe("JoinArena", () => {
  describe("odin", () => {
    runTests("valid case", [
      new JoinArena({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
        championshipId: BigInt(1),
        round: BigInt(1),
        costumes: [uuidToGuidBytes("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")],
        equipments: [uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")],
        runeInfos: [new RuneSlotInfo(BigInt(1), BigInt(1))],
      }),
    ]);
  });
  describe("heimdall", () => {
    runTests("valid case", [
      new JoinArena({
        id: uuidToGuidBytes("ae195a5e-b43f-4c6d-8fd3-9f38311a45eb"),
        avatarAddress,
        championshipId: BigInt(1),
        round: BigInt(1),
        costumes: [uuidToGuidBytes("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")],
        equipments: [uuidToGuidBytes("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")],
        runeInfos: [new RuneSlotInfo(BigInt(1), BigInt(1))],
      }),
    ]);
  });
});
