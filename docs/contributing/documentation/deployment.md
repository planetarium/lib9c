# Deployment

Lib9c automatically deploys documents with GitHub Actions.

Documents are published to `<ref>` (e.g., `development`, `1.18.0`, etc) directory in `gh-pages` branch. when tags or branches are pushed. But tags or branches, starting with `v` are ignored because they are already reserved for other purpose.

After documents were published, you can see your docs at `https://lib9c.nine-chronicles.dev/<ref>`.

For details, you can see [`publish-docs` workflow](https://github.com/planetarium/lib9c/blob/development/.github/workflows/publish-docs.yml)
