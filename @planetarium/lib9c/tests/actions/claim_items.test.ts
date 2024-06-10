import { describe } from "vitest";
import { ClaimItems, fav } from "../../src/index.js";
import { runTests } from "./common.js";
import { agentAddress } from "./fixtures.js";

describe("ClaimItems", () => {
  describe("odin", () => {
    runTests("valid case", [
      new ClaimItems({
        claimData: [
          [
            agentAddress,
            [
              fav(
                {
                  ticker: "FAV__CRYSTAL",
                  decimalPlaces: 18,
                  minters: null,
                  maximumSupply: null,
                  totalSupplyTrackable: false,
                },
                1,
              ),
            ],
          ],
        ],
      }),
      new ClaimItems({
        claimData: [
          [
            agentAddress,
            [
              fav(
                {
                  ticker: "FAV__CRYSTAL",
                  decimalPlaces: 18,
                  minters: null,
                  maximumSupply: null,
                  totalSupplyTrackable: false,
                },
                1,
              ),
            ],
          ],
        ],
        memo: "memo",
      }),
    ]);
  });
});
