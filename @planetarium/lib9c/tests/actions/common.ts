import { Buffer } from "buffer";
import { join } from "node:path";
import { execa } from "execa";
import { expect, test } from "vitest";
import type { PolymorphicAction } from "../../src/index.js";

export function runTests(name: string, cases: PolymorphicAction[]) {
  for (let i = 0; i < cases.length; i++) {
    test(`${name} ${i}`, async () => {
      const action = cases[i];
      console.log(action);
      const bytes = action.serialize();
      console.log(Buffer.from(bytes).toString("hex"));

      const { exitCode } = await execa(
        "dotnet",
        [
          "run",
          "--no-build",
          "--project",
          join(
            __dirname,
            "..",
            "..",
            "..",
            "..",
            ".Lib9c.Tools",
            "Lib9c.Tools.csproj",
          ),
          "--",
          "action",
          "analyze",
          "-",
        ],
        {
          input: bytes,
        },
      );

      expect(exitCode).eq(0);
    });
  }
}
