# 액션

## TransferAsset - 송금

본인이 소유한 `FungibleAssetValue` 를 다른 주소로 넘기고 싶을 때 사용하는 액션입니다.

```typescript
import { TransferAsset, NCG, fav } from "@planetarium/lib9c";
import { Address } from "@planetarium/account";

const action = new TransferAsset({
  sender: Address.fromHex('0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda', true),
  recipient: Address.fromHex('0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa', true),
  amount: fav(NCG, 2),
});
```

이더리움 WNCG 브릿지에 송금하여 이더리움 WNCG로 환전하고 싶다면 다음과 같이 액션을 만드세요. Odin 메인넷 네트워크에서만 유효합니다.

```typescript
import { TransferAsset, NCG, fav } from "@planetarium/lib9c";
import { Address } from "@planetarium/account";

const MY_ADDRESS = Address.fromHex('<YOUR ADDRESS>', true); // [!code warning] Replace with your address.
const ETHEREUM_BRIDGE_ADDRESS = Address.fromHex('0x9093dd96c4bb6b44a9e0a522e2de49641f146223', true);
const AMOUNT = '100.0'; // [!code warning] Replace with the amount you want to bridge.

const action = new TransferAsset({
  sender: MY_ADDRESS,
  recipient: ETHEREUM_BRIDGE_ADDRESS,
  amount: fav(NCG, AMOUNT),
});
```

## TransferAssets - 다중 송금

본인이 소유한 `FungibleAssetValue`들을 다른 여러 주소로 넘기고 싶을 때 사용하는 액션입니다. 아래 예제는 각 주소들에 2 NCG 씩 송금합니다.

```typescript
import { TransferAsses, NCG, fav } from "@planetarium/lib9c";
import { Address } from "@planetarium/account";

const sender = Address.fromHex('0x2cBaDf26574756119cF705289C33710F27443767');
const agentAddresses = [
  Address.fromHex("0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa", true),
  Address.fromHex("0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda", true),
];
const recipients = agentAddresses.map((address) => [address, fav(NCG, 2)]);
const action = new TransferAssets({
  sender,
  recipients,
});
```

## DailyReward - Action Point 충전

```typescript
import { DailyReward } from "@planetarium/lib9c";
import { Address } from "@planetarium/account";

const action = new DailyReward({
  avatarAddress: Address.fromHex('0xDE3873DB166647Cc3538ef64EAA8A0cCFD51B9fE'),
});
```

## Stake - 스테이킹

몬스터 컬렉션(스테이킹)을 진행하고 싶을 때 사용하는 액션입니다. `amount` 만큼 NCG를 예치합니다. 구현 정의는 [NCIP-17]을 참조해주세요.

```typescript
import { Stake } from "@planetarium/lib9c";

const action = new Stake({
  amount: 100n,
});
```

## ClaimStakeReward - 스테이킹 보상 수령

몬스터 컬렉션(스테이킹) 보상을 수령하고 싶을 때 사용하는 액션입니다. `Stake` 액션으로 에치한 NCG에 맞는 보상을 수령합니다. 보상을 받을 `avatarAddress`를 필요로 합니다. 구현 정의는 [NCIP-17]을 참조해주세요.

```typescript
import { ClaimStakeReward } from "@planetarium/lib9c";
import { Address } from "@planetarium/account";

const action = new ClaimStakeReward({
  avatarAddress: Address.fromHex('0xDE3873DB166647Cc3538ef64EAA8A0cCFD51B9fE'),
});
```

[NCIP-17]: https://github.com/planetarium/NCIPs/blob/main/NCIP/ncip-17.md
