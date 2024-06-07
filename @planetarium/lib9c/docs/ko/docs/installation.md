# 설치

`@planetarium/lib9c` 라이브러리는 *[jsr]* 라는 패키지 레지스트리에 배포되었습니다. 그리고 npm에 있는 jsr 패키지를 통해 설치할 수 있습니다. API 문서 또한 jsr에 의해 자동 생성 되므로 [패키지 페이지][jsr-docs] 에서 확인하실 수 있습니다.


[jsr]: https://jsr.io/
[jsr-docs]: https://jsr.io/@planetarium/lib9c@0.2.0-dev.202406030026370315+cb90675a71818491e09469b27a6ad4f19f8e3ce2/doc

## Node

Node.js의 경우 사용하고 계신 패키지 매니저에 따라 아래와 같이 사용할 수 있습니다.

```bash
# pnpm
npx jsr add --pnpm @planetarium/lib9c

# Yarn
npx jsr add --yarn @planetarium/lib9c

# npm
npx jsr add @planetarium/lib9c
```

## Deno

Deno의 경우 Deno CLI로 설치할 수 있습니다.

```bash
# deno add @planetarium/lib9c@<version>
deno add @planetarium/lib9c@0.2.0-dev.202406030026370315+cb90675a71818491e09469b27a6ad4f19f8e3ce2
```
