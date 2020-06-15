# How to use it?

Need add package to package list in `manifest.json`

> Packages/manifest.json

## Before 2019.3
```json
{
  "scopedRegistries": [
    {
      "name": "Main",
      "url": "https://maven.consulo.io/repository/unity",
      "scopes": [
        "com.consulo"
      ]
    }
  ],
  "dependencies": {
    "com.consulo.ide": "2.0.0"
  }
}
```

## After 2019.3

```json
{
  "scopedRegistries": [
    {
      "name": "Main",
      "url": "https://maven.consulo.io/repository/unity",
      "scopes": [
        "com.consulo"
      ]
    }
  ],
  "dependencies": {
    "com.consulo.ide": "2.0.0"
  }
}
```