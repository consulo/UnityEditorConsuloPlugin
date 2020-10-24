# How to use it?

Need add package to package list in `manifest.json`

> Packages/manifest.json

## Before 2019.3 (due certificate problem - use http protocol)
```json
{
  "scopedRegistries": [
    {
      "name": "Main",
      "url": "http://upm.consulo.io/",
      "scopes": [
        "com.consulo"
      ]
    }
  ],
  "dependencies": {
    "com.consulo.ide": "2.2.0"
  }
}
```

## After 2019.3

```json
{
  "scopedRegistries": [
    {
      "name": "Main",
      "url": "https://upm.consulo.io/",
      "scopes": [
        "com.consulo"
      ]
    }
  ],
  "dependencies": {
    "com.consulo.ide": "2.2.0"
  }
}
```