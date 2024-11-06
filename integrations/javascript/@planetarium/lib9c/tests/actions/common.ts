import { Buffer } from "buffer";
import { mkdtemp, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { execa } from "execa";
import { expect, test } from "vitest";
import type { PolymorphicAction } from "../../src/index.js";

export function runTests(name: string, cases: PolymorphicAction[]) {
  for (let i = 0; i < cases.length; i++) {
    test(`${name} ${i}`, async () => {
      const action = cases[i];
      const bytes = action.serialize();

      const command = "dotnet";
      const args = [
        "run",
        "--no-build",
        "--project",
        join(
          __dirname,
          "..",
          "..",
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
      ];
      try {
        const { exitCode } = await execa(command, args, {
          input: Buffer.from(bytes),
        });

        expect(exitCode).eq(0);
      } catch (error: unknown) {
        const e = error as {
          message: string;
        };
        if (/Failed to initiate an action with the stdin/g.test(e.message)) {
          const dumpPrefix = join(tmpdir(), "lib9c.js-testdump-");
          const tempdir = await mkdtemp(dumpPrefix);
          const dumpPath = join(tempdir, "data");
          await writeFile(dumpPath, bytes);
          expect.fail(
            `The test case is failed to instantiate your commit.
You can debug by running the Lib9c.Tools project with following arguments:
'action analyze ${dumpPath}'`,
          );
        } else {
          expect.fail(`Unknown error occurred: ${e.message}`);
        }
      }
    });
  }
}
