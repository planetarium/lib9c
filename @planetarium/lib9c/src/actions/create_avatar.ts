import { BencodexDictionary, type Dictionary } from "@planetarium/bencodex";
import { GameAction, type GameActionArgs } from "./common.js";

export type CreateAvatarArgs = {
  index: bigint;
  hair: bigint;
  lens: bigint;
  ear: bigint;
  tail: bigint;
  name: string;
} & GameActionArgs;

/**
 * The `CreateAvatar` action is used to create a new avatar that owned by agent.
 */
export class CreateAvatar extends GameAction {
  protected readonly type_id: string = "create_avatar11";

  public readonly index: bigint;
  public readonly hair: bigint;
  public readonly lens: bigint;
  public readonly ear: bigint;
  public readonly tail: bigint;
  public readonly name: string;

  constructor({ id, index, hair, lens, ear, tail, name }: CreateAvatarArgs) {
    super({ id });

    this.index = index;
    this.hair = hair;
    this.lens = lens;
    this.ear = ear;
    this.tail = tail;
    this.name = name;
  }

  protected plain_value_internal(): Dictionary {
    return new BencodexDictionary([
      ["index", this.index],
      ["hair", this.hair],
      ["lens", this.lens],
      ["ear", this.ear],
      ["tail", this.tail],
      ["name", this.name],
    ]);
  }
}
