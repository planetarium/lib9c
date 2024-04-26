## Prerequisites

You should install `pnpm` first. You should install nodejs. If you already installed it, you can enable `pnpm` with `corepack`. For details, you should see https://pnpm.io/installation.

```
corepack enable pnpm
```

## Install dependencies

```
pnpm install
```

## Build

```
pnpm build
```

## Test

`@planetarium/lib9c` uses `Lib9c.Tools action analyze` command to check whether implemented actions make valid bencodex value. You should build the .NET project first. **If the .NET Lib9c project is changed, you must build `Lib9c.Tools` project again.**

```
dotnet build ../../.Lib9c.Tools/Lib9c.Tools.csproj
```

If the build proceeded successfully, you can run the below command.

```
pnpm test
```

## Lint

```
pnpm fmt
```

## How to make a new action

- Make a new file under `src/actions` directory. (e.g., `src/actions/transfer_asset.ts`)
- Implement an action. Note: check the action you're work is based on `GameAction` or `ActionBase`.
  ```typescript
  import {
    type Dictionary,
  } from "@planetarium/bencodex";

  export class TransferAsset extends GameAction {
    // Replace with your action's type_id.
    protected readonly type_id: string = "transfer_asset5";

    constructor({
      // Recommend to receive arguments as object format and to use destructuring assignment.
    }) {}

    protected plain_value_internal(): Dictionary {
      // Write a code to return the action's plain value.
    }
  }
  ```
- Re-export your action on the `src/index.js`
  ```typescript
  // Replace with the file and the action class you made.
  export { TransferAsset } from "./actions/transfer_asset.js"
  ```
- Make a new test file under `test/actions` directory. (e.g., `test/actions/transfer_asset.test.ts`)
- Write a test for your action.
  ```typescript
  import { describe } from "vitest";
  import { runTests } from "./common.js"
  import { TransferAsset } from "../../src/index.js"

  // Replace 'TransferAsset' with a new action you made.
  describe("TransferAsset", () => {
    runTests("valid case", [
      new TransferAsset({}),
    ]);
  });
  ```
- Run `pnpm test` command to check whether the action is implemented correctly. If the test is failed with `Failed to initiate an action with the stdin` message, check your action's implementation one more time.
