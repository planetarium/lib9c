import { describe } from "vitest";
import { MakeGuild } from "../../src/index.js";
import { runTests } from "./common.js";

describe("MakeGuild", () => {
  runTests("valid case", [new MakeGuild()]);
});
