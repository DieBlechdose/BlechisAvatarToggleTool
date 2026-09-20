# Blechi Packages

VPM-Listing mit drei einzeln installierbaren Unity-Tools für VRChat-Avatare.

**Listing-Link für den VCC:**

```
https://dieblechdose.github.io/BlechisAvatarToggleTool/index.json
```

Im VCC unter *Settings → Packages → Add Repository* einfügen. Danach erscheinen die drei Packages einzeln und können pro Projekt an- und abgewählt werden:

| Package | Menü in Unity |
| --- | --- |
| Blechi Hierarchy System (`de.dieblechdose.avatartoggletool`) | Tools → Blechi Hierarchy System |
| Blechi Avatar Performance Analyzer (`de.dieblechdose.performanceanalyzer`) | Tools → Blechi Avatar Performance Analyzer |
| Blechi Unity Monitor (`de.dieblechdose.unitymonitor`) | Tools → Blechi Unity Monitor |

## Neue Version veröffentlichen

1. Im Ordner des Packages unter `Packages/` die Version in der `package.json` erhöhen.
2. Committen und pushen.
3. Die GitHub Action „Release Packages“ baut das Zip, erstellt das Release und trägt die Version in die `index.json` ein.
