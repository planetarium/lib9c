import { Buffer } from "buffer";
import {
  BencodexDictionary,
  type Dictionary,
  RecordView,
  type Value,
  encode,
} from "@planetarium/bencodex";
import { v4 as uuidv4 } from "uuid";

export function generateGuid(): Uint8Array {
  const uuid = uuidv4();
  return uuidToGuidBytes(uuid);
}

export function uuidToGuidBytes(uuid: string): Uint8Array {
  const source = Buffer.from(uuid.replace(/-/g, ""), "hex");
  const buffer = Buffer.alloc(16);

  // Match byte-order.
  buffer[0] = source[3];
  buffer[1] = source[2];
  buffer[2] = source[1];
  buffer[3] = source[0];
  buffer[4] = source[5];
  buffer[5] = source[4];
  buffer[6] = source[7];
  buffer[7] = source[6];

  source.copy(buffer, 8, 8, 16);

  return buffer;
}

export abstract class PolymorphicAction {
  serialize(): Uint8Array {
    return encode(this.bencode());
  }

  bencode(): Value {
    return new RecordView(
      {
        type_id: this.type_id,
        values: this.plain_value(),
      },
      "text",
    );
  }

  protected abstract readonly type_id: string;

  protected abstract plain_value(): Value;
}

/**
 * The arguments for the `GameAction` constructor.
 */
export type GameActionArgs = {
  /**
   * The unique identifier of the action.
   * If it is not provided, a new GUID will be generated.
   */
  id?: Uint8Array;
};

export abstract class GameAction extends PolymorphicAction {
  id: Uint8Array;

  constructor({ id }: GameActionArgs) {
    super();

    if (id !== undefined && id.length !== 16) {
      throw new RangeError("'id' must be 16-length.");
    }

    this.id = id || generateGuid();
  }

  protected override plain_value(): Value {
    return new BencodexDictionary([
      ["id", this.id],
      ...this.plain_value_internal(),
    ]);
  }

  protected abstract plain_value_internal(): Dictionary;
}
