import type { Value } from "@planetarium/bencodex";
import { PolymorphicAction } from "./common.js";

export class MigratePlanetariumGuild extends PolymorphicAction {
  protected readonly type_id: string = "migrate_planetarium_guild";

  protected plain_value(): Value {
    return null;
  }
}
