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
dotnet docfx build --serve
```

## Publish

```
dotnet docfx build
```
