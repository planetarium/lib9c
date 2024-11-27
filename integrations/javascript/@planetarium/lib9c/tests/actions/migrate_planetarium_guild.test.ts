import { describe } from "vitest";
import { MigratePlanetariumGuild } from "../../src/index.js";
import { runTests } from "./common.js";

describe("MigratePlanetariumGuild", () => {
  runTests("valid case", [new MigratePlanetariumGuild()]);
});
