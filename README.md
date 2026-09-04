# Life Bucket List

Eine persönliche Desktop-App, um alles zu tracken, was du gesehen, gespielt, bereist oder erlebt hast:
Reiseziele, Aktivitäten, Filme, Serien, Videospiele, Konzerte.

Einzelnutzer-App, keine Cloud, kein Login. Alle Daten liegen lokal auf deinem Rechner.

## Stack & Architektur

- **.NET 7 + Avalonia UI 11** (MVVM, `CommunityToolkit.Mvvm`) – eine echte, installierbare Windows-Desktop-App
  (kein Browser/WebView), auf Basis von Avalonia auch grundsätzlich cross-platform (macOS/Linux-Desktop
  liefen mit demselben Code, ohne Zusatzaufwand).
- **SQLite** (`Microsoft.Data.Sqlite`) als lokale Datenhaltung unter `%AppData%\LifeBucketList\data.db`.
- **JSON-Export/Import** für manuelle Backups bzw. Übertragung zwischen Geräten.
- **TMDb** (Filme/Serien) und **IGDB/Twitch** (Videospiele) für die optionale, automatische Cover-Suche
  – siehe [Cover-Suche einrichten](#cover-suche-einrichten).
- **Natural Earth** (öffentliche, gemeinfreie Geodaten) für die Länderumrisse auf der Reiseziele-Karte.
- Warum dieser Stack: In der Zielumgebung war nur das .NET-SDK vorinstalliert (kein Flutter/Node/Rust).
  Avalonia erzeugt eine echte native Desktop-App, ohne große zusätzliche SDK-Downloads (Android
  Studio, Xcode, Flutter-SDK) zu benötigen – siehe Abschnitt [Mobile](#mobile-ausblick) unten.

### Schichten (Projekte)

```
src/
  LifeBucketList.Domain   Modelle, Repository-Interfaces, reine Business-Logik
                          (Validierung, Filtern/Sortieren, Dashboard-Statistik, Länderliste) – keine Abhängigkeiten
  LifeBucketList.Data     SQLite-Repositories, JSON-Backup-Service, TMDb/IGDB-Cover-Suche (inkl.
                          Twitch-OAuth2-Token-Handling), Cover-Bild-Cache, API-Key-Auflösung
                          (.env-Datei, JSON-Datei, Umgebungsvariable)
  LifeBucketList.App      Avalonia-UI: Views (XAML), ViewModels, eigene Controls (Sterne, Weltkarte
                          mit echten Länderumrissen, deutschsprachiger Datums-Picker), DI-Composition-Root

tests/
  LifeBucketList.Domain.Tests   Unit-Tests für Validierung, Filter/Sort, Dashboard-Berechnung, Länderdaten
  LifeBucketList.Data.Tests     Unit-Tests für SQLite-Repositories, JSON-Export/Import, Migrationen,
                                 TMDb/IGDB-Suche, Twitch-Token-Handling (Caching/Ablauf) und
                                 API-Key-Auflösung (mit gefaktem HTTP-Handler, ohne echte Netzwerkzugriffe)
  LifeBucketList.App.Tests      Integrations-/UI-Tests (Avalonia.Headless): komplette Abläufe
                                 Eintrag anlegen/bearbeiten/löschen/bewerten, Filtern, Live-Cover-Suche
                                 (inkl. Debounce/Race-Conditions), Weltkarte, Backup-Roundtrip, sowie
                                 echte simulierte Klicks auf Sterne-Bewertung und Datumsfeld
```

`IDialogService` entkoppelt die ViewModels von echten Fenstern/Dateidialogen, sodass die
Kern-Abläufe (inkl. simuliertem Nutzer-Input) ohne echte UI getestet werden können.

## Voraussetzungen

- Windows 10/11
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) oder neuer (zum Bauen/Entwickeln;
  zum reinen Ausführen der veröffentlichten `.exe`/des Installers wird **kein** .NET benötigt)

## Starten (Entwicklung)

```bash
dotnet run --project src/LifeBucketList.App
```

## Tests ausführen

```bash
dotnet test
```

Alle 332 Tests (Domain, Data, App) sollten grün sein.

## Installierbare Version bauen

Zwei Optionen, je nachdem wie "richtig installiert" es sein soll:

**Portable Version** – eine einzelne, eigenständige `.exe` (enthält die .NET-Runtime, kein separates
.NET-Setup auf dem Zielrechner nötig):

```bash
dotnet publish src/LifeBucketList.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Das Ergebnis liegt in `publish/LifeBucketList.App.exe` (+ ein paar native Grafik-/SQLite-DLLs im
selben Ordner, die aus technischen Gründen nicht in die Single-File gepackt werden können – der
ganze `publish`-Ordner muss zusammenbleiben). Taucht so nicht in der Windows-Suche auf, da nichts
registriert/verknüpft wird.

**Echter Installer (MSI)** – mit Startmenü-Eintrag, Icon und Auffindbarkeit über die Windows-Suche,
gebaut mit dem [WiX Toolset](https://wixtoolset.org/) (v5, als .NET-Tool):

```bash
dotnet tool install --global wix --version 5.0.2      # einmalig
wix extension add WixToolset.UI.wixext/5.0.2           # einmalig
powershell -File installer\build.ps1
```

Das Ergebnis liegt in `installer-output\LifeBucketList.msi`. Doppelklick installiert die App nach
`C:\Program Files\Life Bucket List` und legt eine Startmenü-Verknüpfung an (mit dem App-Icon) –
danach ist "Life Bucket List" über die Windows-Suche auffindbar, genau wie jede andere installierte
App. Deinstallation läuft ganz normal über "Apps & Features".

Warum WiX statt eines anderen Installer-Frameworks: WiX ist ein reines NuGet-verteiltes .NET-Tool
(kein separates großes SDK wie Visual Studio/Inno Setup nötig) und erzeugt ein echtes MSI mit
Standard-Windows-Integration (Startmenü, Registrierung, "Apps & Features").

## Cover-Suche einrichten

Für die Kategorien **Filme**, **Serien**, **Videospiele** und **Konzerte** (im Künstler-Modus)
erscheinen beim Anlegen/Bearbeiten eines Eintrags automatisch passende Cover-Vorschläge, sobald du
einen Titel eingibst (Live-Suche mit kurzer Verzögerung, kein Klick nötig). Das läuft über drei
kostenlose APIs:

- **[TMDb](https://www.themoviedb.org/)** für Filme & Serien
- **[IGDB](https://api-docs.igdb.com/)** (Twitch-basiert) für Videospiele
- **[Spotify](https://developer.spotify.com/)** für Konzerte-Künstler

### Kostenlosen API-Zugang besorgen

1. **TMDb:** Account auf themoviedb.org anlegen → Profil → Einstellungen → API → "API-Schlüssel
   anfordern" (Typ "Developer" reicht, kostenlos, kein Zahlungsmittel nötig).
2. **IGDB:** Auf [dev.twitch.tv/console/apps](https://dev.twitch.tv/console/apps) eine (kostenlose)
   Twitch-App registrieren → man erhält eine **Client-ID** und ein **Client-Secret**. Die App holt
   sich damit selbst automatisch ein Zugriffstoken (OAuth2 Client-Credentials-Flow bei
   `id.twitch.tv`) und erneuert es, sobald es abläuft – dafür ist nichts manuell einzustellen.
3. **Spotify:** Im [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) eine
   (kostenlose) App anlegen → man erhält eine **Client-ID** und ein **Client-Secret**. Genau wie bei
   IGDB holt sich die App damit selbst automatisch ein Zugriffstoken (OAuth2 Client-Credentials-Flow
   bei `accounts.spotify.com`) und erneuert es bei Bedarf.

### Zugangsdaten eintragen

Drei Wege – alle optional, einer reicht (in dieser Reihenfolge ausgewertet):

1. **Umgebungsvariablen:** `LBL_TMDB_API_KEY`, `LBL_IGDB_CLIENT_ID`, `LBL_IGDB_CLIENT_SECRET`,
   `LBL_SPOTIFY_CLIENT_ID`, `LBL_SPOTIFY_CLIENT_SECRET`.
2. **`.env`-Datei** im Projekt-Root (praktisch für die Entwicklung; wird beim Start gesucht, indem
   ausgehend vom Programmverzeichnis nach oben durchsucht wird):
   ```
   LBL_TMDB_API_KEY=dein-tmdb-key
   LBL_IGDB_CLIENT_ID=deine-client-id
   LBL_IGDB_CLIENT_SECRET=dein-client-secret
   LBL_SPOTIFY_CLIENT_ID=deine-spotify-client-id
   LBL_SPOTIFY_CLIENT_SECRET=dein-spotify-client-secret
   ```
3. **Lokale JSON-Datei** (praktisch für die installierte App):
   `%AppData%\LifeBucketList\apikeys.json`:
   ```json
   {
     "TmdbApiKey": "dein-tmdb-key",
     "IgdbClientId": "deine-client-id",
     "IgdbClientSecret": "dein-client-secret",
     "SpotifyClientId": "deine-spotify-client-id",
     "SpotifyClientSecret": "dein-spotify-client-secret"
   }
   ```

Weder `.env` noch `apikeys.json` werden jemals eingecheckt (beide stehen in `.gitignore`) – die
Zugangsdaten stehen nirgends hart codiert im Quelltext.

**Ohne Zugangsdaten oder ohne Internet:** Die Cover-Suche zeigt einen dezenten Hinweis ("Kein
API-Key hinterlegt" bzw. "momentan nicht erreichbar") und der Eintrag lässt sich trotzdem ganz normal
ohne Cover speichern – nichts blockiert die Kernfunktion.

Gefundene Cover werden lokal unter `%AppData%\LifeBucketList\covers` zwischengespeichert, damit sie
auch offline weiter angezeigt werden, nachdem sie einmal geladen wurden.

## Weltkarte (Reiseziele)

Bei der Kategorie **Reiseziele** gibt es ein Land-Auswahlfeld (Dropdown mit ca. 190 Ländern) sowie
eine Kartenansicht oberhalb der Eintragsliste: Länder, zu denen mindestens ein Eintrag existiert,
werden als zusammenhängende Fläche in der Akzentfarbe hervorgehoben – alle anderen Länder sind als
neutrale, dünn umrandete Umrisse zu sehen.

Die Länderumrisse stammen aus einem gebündelten, gemeinfreien Datensatz von
**[Natural Earth](https://www.naturalearthdata.com/)** (1:110m-Auflösung "Admin 0 – Countries", bewusst
stark vereinfacht, passend zum schlichten Stil der App) und werden zur Laufzeit aus
[`countries-110m.geojson`](src/LifeBucketList.App/Assets/countries-110m.geojson) geladen und auf eine
einfache Weltkarten-Projektion projiziert – vollständig offline, kein Netzwerkzugriff nötig. Die
Zuordnung zu Einträgen läuft über den ISO-3166-1-alpha-2-Ländercode.

## DACH-Karte (Konzerte)

Bei der Kategorie **Konzerte** gibt es analog eine kleine Karte für Deutschland, Österreich und die
Schweiz: ein Auftritt kann entweder einem bestimmten Künstler/Interpreten oder – über den
"Art"-Umschalter im Bearbeitungsfenster – einem Festival ohne einzelnen Hauptact zugeordnet werden.
Beide Modi tragen zusätzlich einen freien Veranstaltungsort/Venue-Text und eine Region (die 16
deutschen Bundesländer plus Österreich/Schweiz als Ganzes); Regionen mit mindestens einem Eintrag
werden auf der Karte hervorgehoben, genau wie bei der Reiseziele-Weltkarte.

Die Bundesland-Umrisse stammen ebenfalls von Natural Earth, diesmal aus der feineren
1:10m-Auflösung "Admin 1 – States, Provinces" (die 110m/50m-Stufen decken nur US-/Kanada-Regionen ab)
– lokal auf Deutschland gefiltert und als [`germany-states-10m.geojson`](src/LifeBucketList.App/Assets/germany-states-10m.geojson)
gebündelt (~300 KB statt der ~40 MB Rohdatei). Österreich und die Schweiz werden direkt aus den
bereits gebündelten Weltkarten-Umrissen übernommen statt einen zweiten Datensatz mitzuliefern.

## Daten & Backup

- Datenbank: `%AppData%\LifeBucketList\data.db` (wird beim ersten Start automatisch angelegt und
  mit den sechs festen Kategorien befüllt; Schema-Erweiterungen für ältere Datenbanken – z. B. neue
  Spalten für Cover/Land/Konzerte oder eine neu hinzugekommene Kategorie – laufen beim Start
  automatisch und verlustfrei mit).
- **Exportieren**/**Importieren** (Buttons oben rechts) schreiben/lesen eine JSON-Datei mit allen
  Kategorien und Einträgen (inkl. Cover-URL und Land) – zum manuellen Sichern oder Übertragen auf
  ein anderes Gerät. Ein Import ersetzt alle aktuell gespeicherten Einträge (mit Sicherheitsabfrage);
  Einträge werden dabei anhand des Kategorienamens (nicht der internen ID) den lokalen, festen
  Kategorien zugeordnet, damit ein Import auf einem anderen Rechner/einer frischen Installation
  funktioniert.

## Mobile-Ausblick

Für v1 ist nur die Windows-Desktop-App gebaut (wie angefordert, Mobile ist kein Muss für v1). Da
Domain- und Data-Schicht reines, UI-unabhängiges .NET sind und die UI-Schicht auf Avalonia basiert
(das auch Android/iOS-Head-Projekte unterstützt), ließe sich eine Mobile-Version grundsätzlich mit
denselben `Domain`-/`Data`-Projekten realisieren – dafür wäre lediglich ein zusätzliches
`LifeBucketList.Android`/`.iOS`-Head-Projekt nötig sowie das Android-SDK bzw. Xcode, die in der
aktuellen Build-Umgebung nicht installiert sind.

## Getroffene Standardentscheidungen

Details, die offen waren und dokumentiert statt nachgefragt wurden:

- **Kategorien sind fest**, in der Reihenfolge Reiseziele, Aktivitäten, Filme, Serien, Videospiele,
  Konzerte – nicht mehr vom Nutzer erweiter-, umbenenn- oder löschbar. Eine bereits vorhandene
  Kategorie "Spiele" (aus einer älteren Version) wird beim Start automatisch und ohne Datenverlust in
  "Videospiele" umbenannt (gleiche ID, alle Einträge bleiben verknüpft); die Tab-Reihenfolge wird bei
  jedem Start auf den aktuellen Stand migriert, falls sie sich seit der letzten Version geändert hat.
  Eine neu hinzugekommene feste Kategorie (wie Konzerte) wird beim Start auch in einer bereits
  bestehenden, nicht-leeren Datenbank automatisch ergänzt statt nur bei einer komplett leeren.
- **Speicherformat:** SQLite für die laufende App (robust, abfragefähig), JSON nur für den
  Export/Import (menschenlesbar, einfach zu versionieren/verschicken).
- **Bewertung:** 1–5 Sterne, optional; ein erneuter Klick auf den aktuell niedrigsten aktiven Stern
  setzt die Bewertung zurück auf "keine Bewertung".
- **Import-Verhalten:** Vollständiges Ersetzen der Einträge (kein Merge) – einfacher, klar
  kommuniziertes Verhalten für den Backup/Transfer-Anwendungsfall.
- **Land statt Stadt für die Karte:** Der ursprüngliche Prompt erwähnte "Land- oder Stadt-Angabe"; ich
  habe mich für ein Land-Dropdown statt Freitext-Stadtangabe entschieden, weil das eine zuverlässige
  Zuordnung zur Karte garantiert (kein fehleranfälliges Stadt→Land-Geocoding nötig). Der Titel des
  Eintrags bleibt weiterhin frei (z. B. "Sommerurlaub Toskana" oder "Kyoto").
- **Theme-Umschalter-Bug:** Die App folgte beim Start `RequestedThemeVariant="Default"` (System-Theme),
  hat den Umschalter-Zustand aber nie danach synchronisiert – daher war er anfangs immer auf "Hell",
  selbst wenn tatsächlich Dunkelmodus aktiv war. Fix: Beim Start wird das tatsächlich aktive Theme
  einmalig aufgelöst und explizit gesetzt (nicht mehr "Default"), und der Umschalter wird direkt
  darauf synchronisiert.
- **Windows-SDK-Version:** WiX 7 (aktuellste Version) benötigt .NET 8; die Build-Umgebung hat nur das
  .NET 7 SDK. Deshalb WiX 5.0.2, die neueste mit .NET 7 kompatible Version.
- **Eigener Datums-Picker:** Avalonias eingebautes `DatePicker`-Steuerelement zeigt die Platzhalter
  "day"/"month"/"year" fest codiert auf Englisch an – das reagiert nachweislich nicht auf die
  Kultureinstellung (empirisch mit einem Testrender geprüft) und lässt sich auch nicht sauber
  zentrieren. Die App verwendet daher `GermanDatePicker` (ein Button mit deutschem Datumstext,
  zentriert, öffnet einen `Calendar`-Flyout) – Avalonias `Calendar`-Steuerelement ist tatsächlich
  kultursensitiv und zeigt deutsche Monats-/Wochentagsnamen korrekt an.
- **Pflichtfelder:** Titel und Kategorie sind mit "*" markiert; alle anderen Felder sind ohne
  Sternchen automatisch als optional erkennbar, daher wurde der Text "(optional)" entfernt.
- **Eigenes App-Logo:** Statt eines fremden/generischen Icons verwendet die App ein selbst generiertes
  Logo (`Assets/app-logo.ico`) – ein einfaches Checklisten-Motiv (zwei abgehakte, ein offener Punkt) im
  bestehenden Blauton `#0A84FF`, passend zum Bucket-List-Thema. Erzeugt programmatisch (kein
  Fremdmaterial), eingebunden über `ApplicationIcon` im Projekt und als Fenster-/Installer-Icon.
- **Endgültige Entfernung der Kategorie "Test":** Die alte "Test"-Kategorie stammte noch aus der Zeit
  frei anlegbarer Kategorien und blieb als Datenbank-Leiche zurück, obwohl Kategorien seit Runde 2 fest
  sind. Fix in der Datenschicht (`SqliteConnectionFactory`), nicht nur in der UI: Bei jedem Start werden
  alle Kategorien (samt Einträgen), die keiner der fünf festen Kategorien entsprechen, endgültig
  gelöscht – idempotent, kein Risiko für zukünftige Starts.
- **Icon-Erzeugung ohne PNG-Kompression:** `System.Drawing.Icon` (die von .NET unter Windows genutzte
  ICO-Bibliothek) hat einen bekannten Bug beim Laden PNG-komprimierter ICO-Frames
  (`ArgumentOutOfRangeException` bei `ToBitmap()`/`Icon(path, w, h)`, obwohl der Explorer dieselbe Datei
  anstandslos anzeigt). `app-logo.ico` wird deshalb mit klassischen unkomprimierten 32-bit-DIB-Frames
  (16/32/48/256 px) erzeugt statt mit PNG-Frames – dadurch überall verlässlich ladbar.
- **Installer-Update-Problem gelöst:** Ein MSI mit unveränderter `Version` und WiX' Standardeinstellung
  `AllowSameVersionUpgrades="no"` installiert beim erneuten Ausführen über eine bestehende Installation
  hinweg **nichts** – die alten Dateien bleiben unbemerkt liegen. `Product.wxs` setzt jetzt
  `AllowSameVersionUpgrades="yes"`, `build.ps1` räumt `publish/`/`installer-output/` vor jedem Build auf,
  und die Versionsnummer wurde auf 1.5.0 angehoben, damit ein `LifeBucketList.msi` immer die zuletzt
  gebaute Version installiert.
- **Konzerte-Datenmodell statt Land wiederverwendet:** `Entry.CountryCode` ist per Doc-Kommentar und
  im Code ausdrücklich "Reiseziele only". Statt es für Konzerte zweckzuentfremden, bekommt `Entry` drei
  eigene, nullable Spalten – `Venue` (Freitext-Ort), `RegionCode` (Bundesland/AT/CH) und `IsFestival`
  (persistierter Modus-Flag, damit ein Wiederbearbeiten den ursprünglichen Künstler-/Festival-Modus
  korrekt wiederherstellt) – nach demselben additiven Muster wie die bereits vorhandenen
  `CoverImageUrl`/`CountryCode`-Spalten.
- **Cross-Plattform-Vorbereitung ohne echten Mobile-Build:** Domain/Data waren schon vor dieser Runde
  UI-unabhängig; für diese Runde wurde bewusst kein Android/iOS-Head-Projekt angelegt (iOS lässt sich
  ohne macOS/Xcode in dieser Umgebung ohnehin nicht bauen/verifizieren) – die Architektur bleibt
  mobil-tauglich, ein späterer Head kann `Domain`/`Data` unverändert wiederverwenden.
- **Eigenes Design-System statt Copy eines bestehenden:** Für das professionelle Redesign wurde bewusst
  kein 1:1-Klon einer bekannten App/eines Frameworks gewählt, sondern ein eigenes, konsistentes System
  (Radius-/Typografie-Skala, wiederverwendbare Farbtoken, gezielte Fluent-Resource-Overrides für
  ComboBox/Calendar/ToggleSwitch, durchgängige Hover-/Pressed-/Fokus-/Disabled-Zustände, handgezeichnete
  Vektor-Icons statt Emoji) direkt auf der bestehenden Apple-artigen Basis aufgebaut.
