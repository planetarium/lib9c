# How to build documents with DocFX

The documents are published using [DocFX](https://dotnet.github.io/docfx/).

## Install DocFX

This project uses `tool-manifest` to specify tools' versions used in
this project. You can install tools with the below command:

```
dotnet tool restore
```

## Preview

```
dotnet docfx docfx.json -m pdf=false --serve
```

If you want to build PDF, just run without `-m pdf=false` option:

```
dotnet docfx docfx.json --serve
```

## Publish

```
dotnet docfx docfx.json
```

## Q&A

### About multilingual (translation)

Since DocFX doesn't support multilingual (translation) feature like vitepress, I recommend to write in English, in one language.

### About `toc.yml` files

`toc.yml` file is a yaml file to describe *Table of Contents*. To see its spec, look around https://dotnet.github.io/docfx/docs/table-of-contents.html docs.
