# How to use it?

Need add package to package list in `manifest.json`

> Packages/manifest.json

```json
{
  "dependencies": {
    "com.consulo.ide": "https://github.com/consulo/UnityEditorConsuloPlugin.git#2.6.0"
  }
}
```

## For advanced users

Package can use be used as git repository - but be careful, master is dev branch.

```json
  "dependencies": {
    "com.consulo.ide": "https://github.com/consulo/UnityEditorConsuloPlugin.git"
  }
```

# Links
  * [Help](https://consulo.help/plugins/consulo.unity3d/)
