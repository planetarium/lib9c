# 유틸리티

## 통화 (通貨)

### NCG

나인크로니클에서는 'NCG' 라고 줄여부르는 *Nine Chronicles Gold* 통화가 있습니다. 이는 몬스터 컬렉션이라고 불리는 스테이킹에 사용됩니다. 또한 아이템을 구매하거나 강화하거나, 아레나 티켓을 구매하는데 사용되기도 합니다.

```typescript
import { NCG } from "@planetarium/lib9c";
```

하지만 Nine Chronicles 은 Multiplanetary 라는 시스템 하에 Odin, Heimdall 이라는 두 네트워크를 운영하고 있습니다. 둘 다 유효한 메인넷이지만 WNCG 브릿지는 Odin에서만 가능한 특징이 있습니다. Heimdall 에서는 `NCG` 대신 `MINTERLESS_NCG` 를 사용하셔야 합니다.

```typescript
import { MINTELESS_NCG } from "@planetarium/lib9c";
```

### CRYSTAL

'CRYSTAL' 은 아이템 레시피를 해방하고 제작하거나 아레나에 참가하는데 사용되는 통화입니다. 스테이킹 보상을 통해 얻을 수 있습니다.

```typescript
import { CRYSTAL } from "@planetarium/lib9c";
```

### MEAD

'MEAD' 는 나인크로니클 네트워크에서 트랜잭션 수수료로 사용되는 통화입니다.

```typescript
import { MEAD } from "@planetarium/lib9c";
```

### GARAGE

'GARAGE' 은 전송 불가한 재화를 옮길때 사용합니다. [NCIP-16](https://github.com/planetarium/NCIPs/blob/main/NCIP/ncip-16.md)을 참고해주세요.

```typescript
import { GARAGE } from "@planetarium/lib9c";
```

## 유틸리티 함수

### `fav`

본래 `@planetarium/tx` 의 `FungibleAssetValue`를 만들려면 아래와 같은 코드를 작성해야 합니다. 아래 코드는 `10 CRYSTAL` 을 의미하는 `FungibleAssetValue`를 만드는 코드입니다.

```typescript
const fav: FungibleAssetValue = {
  currency: CRYSTAL,
  rawValue: BigInt(new Decimal(10).mul(Decimal.pow(10, CRYSTAL.decimalPlaces)).toString())
}
```

`decimal.js`로 이런 처리를 하는 이유는 `FungibleAssetValue` (이하 `FAV`) 에서 수량을 표시할 때 소수점을 달리 지원하지 않기 때문입니다. 소수점을 지원하지 않는다는 것이 정말 소수점을 지원하지 않는 것은 아니고, 실제 값은 정수로 가진채 `Currency.decimalPlaces` 의 값으로 소수점 위치를 정의합니다.

예를 들어 `2 NCG` 를 만들때 `FAV.rawValue` 는 `200n` 입니다. 왜냐하면 `NCG.decimalPlaces` 가 2 이기 때문에 `00` 부분을 소수점 자리로 쓰고 `2` 부분은 자연수 부분으로 쓰이는 것입니다. 일반적인 소수로 표현하면 `2.00 NCG` 입니다.

`fav`는 이런 코드를 축약해서 사용할 수 있게 해주는 유틸리티 함수입니다. 위 코드를 `fav` 함수로 표현하면 다음과 같습니다.

```typescript
fav(NCG, 2);
```

하지만 부동소수점에는 오차가 있기 때문에 `number` 타입으로 소수 (小數) 를 다루는 것은 권장하지 않습니다. 소수 외에도 수의 크기에도 제한이 있기 때문에 `bigint`나 `string`, `decimal.js` 를 사용해야합니다. 만약 `decimal.js` 를 사용한다면 `.toString()` 으로 `string` 타입으로 만들어 넘겨주세요.

```typescript
import { fav } from "@planetarium/lib9c";
```
