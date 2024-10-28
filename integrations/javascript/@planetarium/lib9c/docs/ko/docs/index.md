---
outline: deep
---

# 소개

`@planetarium/lib9c` 라이브러리는 Nine Chronicles 네트워크와 상호작용하기 위해 필요한 트랜잭션을 만드는 것을 돕기 위해 만들어졌습니다. 웹에서 사용하기 쉽게 JavaScript/TypeScript를 이용하여 작성되었습니다. 만약 당신이 Nine Chronicles 네트워크 위에서 서비스를 만들거나 자동화 봇을 만들때 굉장히 유용할 것입니다.

부족한 액션들이 있을 수 있지만 커뮤니티에서 필요에 따라 임의로 추가하고 기여해주길 희망합니다. 관련 문서도 준비되어 있으며 만약 어렵다면 디스코드를 통해 도움을 요청해주세요.

## 연관된 라이브러리

Nine Chronicles 네트워크는 Planetarium 에서 만든 Libplanet 이라는 .NET 기반 블록체인 라이브러리를 활용하여 만들어졌습니다. 그렇기에 Libplanet 팀에서 만든 `@planetarium/tx`, `@planetarium/account` 라이브러리를 활용하여 트랜잭션을 만들고 서명하는 것이 좋습니다. 해당 라이브러리들의 사용법에 대해서는 각 라이브러리의 문서를 참고 부탁드립니다.

## 자바스크립트를 사용할 때 주의점

본 라이브러리는 TypeScript으로 작성된 라이브러리 입니다. 타입 체킹을 통해 충분히 검증된 값이 넘어오기를 기대합니다. JavaScript에는 이런 타입 체킹이 없고 본 라이브러리에도 동적으로 검사하는 로직이 없으므로, 자바스크립트를 사용하실 경우 이런 잘못된 값으로 인한 문제를 알아차라기 어려울 수 있음을 인지해주시길 바랍니다.

## 디스코드

[![Planetarium Dev][planetarium-dev-badge]][planetarium-dev-invite-link]

이 라이브러리는 Planetarium 조직의 DX 팀에 의해 만들어졌습니다. Planetarium Dev 디스코드 서버에 상주하고 있으며 질문에 있다면 서버의 'NINE CHRONICLES > #general', 'NINE CHRONICLES > #lib9c' 등의 채널에 남겨주시길 바랍니다.


[planetarium-dev-badge]: https://img.shields.io/discord/928926944937013338?color=6278DA&label=Planetarium-dev&logo=discord&logoColor=white
[planetarium-dev-invite-link]: https://discord.com/invite/RYJDyFRYY7
