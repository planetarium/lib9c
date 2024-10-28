import type { Value } from "@planetarium/bencodex";

export class RuneSlotInfo {
  public readonly slotIndex: bigint;
  public readonly runeId: bigint;

  constructor(slotIndex: bigint, runeId: bigint) {
    this.slotIndex = slotIndex;
    this.runeId = runeId;
  }

  public serialize(): Value[] {
    return [this.slotIndex.toString(), this.runeId.toString()];
  }

  static deserialize(data: {
    slotIndex: string;
    runeId: string;
  }): RuneSlotInfo {
    return new RuneSlotInfo(BigInt(data.slotIndex), BigInt(data.runeId));
  }
}
