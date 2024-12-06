import { afterEach } from "node:test";
import { Decimal } from "decimal.js";
import { afterAll, beforeEach, describe } from "vitest";
import { MEAD, MintAssets, NCG, fav } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress, fungibleIdA } from "./fixtures.js";

describe("MintAssets", () => {
  beforeEach(() => {
    Decimal.set({
      toExpPos: 900000000000000,
    });
  });

  afterEach(() => {
    Decimal.set({
      defaults: true,
    });
  });

  afterAll(() => {
    Decimal.set({
      defaults: true,
    });
  });

  runTests("valid case", [
    new MintAssets({
      mintSpecs: [],
      memo: null,
    }),
    new MintAssets({
      mintSpecs: [],
      memo: "memo",
    }),
    new MintAssets({
      mintSpecs: [
        {
          recipient: agentAddress,
          amount: fav(MEAD, 100_0000n),
        },
      ],
      memo: "memo",
    }),
    new MintAssets({
      mintSpecs: [
        {
          recipient: agentAddress,
          amount: fav(NCG, 10n),
        },
      ],
      memo: "memo",
    }),
    new MintAssets({
      mintSpecs: [
        {
          recipient: agentAddress,
          fungibleItemId: fungibleIdA,
          count: 1n,
        },
      ],
      memo: "memo",
    }),
    new MintAssets({
      mintSpecs: [
        {
          recipient: agentAddress,
          fungibleItemId: fungibleIdA,
          count: 1n,
        },
        {
          recipient: agentAddress,
          amount: fav(MEAD, 100_0000n),
        },
      ],
      memo: "memo",
    }),
    new MintAssets({
      mintSpecs: [
        {
          recipient: agentAddress,
          fungibleItemId: fungibleIdA,
          count: 1n,
          amount: fav(MEAD, 100_0000n),
        },
      ],
      memo: "memo",
    }),
  ]);
});
