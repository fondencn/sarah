# Raum-Detailseite mit Geräten und Temperatur-Zeitplänen

## User Story

> Als Nutzerin möchte ich auf einer Raum-Detailseite alle Geräte mit ihrem aktuellen Zustand sowie die Temperatur-Zeitpläne dieses Raums sehen und dort Zeitpläne anlegen, ändern und löschen, damit ich einen Raum an einer Stelle überblicken und steuern kann.

## Akzeptanzkriterien

- **Gegeben** die Räume-Seite mit der Raumtabelle, **wenn** ich bei einem Raum auf "Details" klicke, **dann** öffnet sich die Detailseite genau dieses Raums, und der Raumname ist im Titel sichtbar.
- **Gegeben** die Raum-Detailseite, **wenn** sie geladen ist, **dann** sehe ich alle diesem Raum zugeordneten Geräte mit Name, Typ und aktuellem Zustand (z. B. an/aus, Ist-/Solltemperatur, Messwerte). Das sieht genauso aus wie auf der Geräteseite.
- **Gegeben** ein Raum ohne Geräte, **wenn** ich die Detailseite öffne, **dann** sehe ich einen verständlichen Leerzustand statt einer leeren Fläche.
- **Gegeben** ein Gerät in der Raumansicht, **wenn** ich es anklicke, **dann** öffnet sich derselbe Steuerdialog wie auf der Geräteseite, mit denselben Bedienmöglichkeiten.
- **Gegeben** ich habe im Dialog etwas geändert, **wenn** ich ihn schließe, **dann** zeigt die Raumansicht den neuen Gerätezustand an, ohne dass ich die Seite neu laden muss.
- **Gegeben** ein Raum mit Zeitplänen, **wenn** ich die Detailseite öffne, **dann** sehe ich nur die Zeitpläne dieses Raums mit Zieltemperatur und Wiederholung/Zeitpunkten.
- **Gegeben** ein Raum ohne Zeitpläne, **wenn** ich die Detailseite öffne, **dann** sehe ich einen Hinweis und eine Schaltfläche "Zeitplan erstellen".
- **Gegeben** die Detailseite, **wenn** ich einen Zeitplan erstelle, **dann** ist der Raum bereits vorausgewählt und nicht versehentlich änderbar. Nach dem Speichern erscheint der Zeitplan sofort in der Liste.
- **Gegeben** ein bestehender Zeitplan, **wenn** ich ihn bearbeite oder lösche (mit Rückfrage), **dann** ist die Änderung sofort sichtbar.
- **Gegeben** das Erstellen oder Bearbeiten, **wenn** ich eine Temperatur außerhalb 8–30 °C eingebe oder einen Konflikt auslöse, **dann** gelten dieselben Regeln und Meldungen wie in [room-temperature-schedules.md](room-temperature-schedules.md).
- **Gegeben** ich bin auf der Detailseite, **wenn** ich "Zurück" wähle, **dann** lande ich wieder auf der Räume-Seite.
- **Gegeben** ein Smartphone-Bildschirm, **wenn** ich die Detailseite nutze, **dann** bleibt alles lesbar und ohne seitliches Scrollen bedienbar.
- **Gegeben** die Seite lädt oder ein Fehler tritt auf, **wenn** Daten fehlen, **dann** sehe ich eine Ladeanzeige bzw. eine verständliche Fehlermeldung.

## Priorität

- **Must have:** Gerätezustand, Wiederverwendung des Steuerdialogs, Zeitpläne verwalten. Das ist der Kern, damit der Raum zentral bedienbar wird.
- **Could have:** Automatisch aktualisierter Zustand ohne Dialogschließen (Live-Updates).

## Rahmenbedingung

Es soll eine reine UI-Änderung sein. Falls das Backend doch angepasst werden muss, vorher mit Begründung melden. Zu prüfen:

- Gibt es eine Abfrage "Geräte eines Raums"?
- Gibt es eine Abfrage "Zeitpläne eines Raums"?

## Entscheidungen

- Zeitpläne werden als **Liste** dargestellt (keine Kalenderwochenansicht).
- Geräte erscheinen als **Kacheln**.
- Der Gerätezustand (z. B. an/aus, Ist-/Solltemperatur, Messwerte) ist **direkt auf der Kachel** sichtbar.
- Geräte werden **nur über den bestehenden Steuerdialog** bedient, nicht direkt auf der Kachel.
- Geräte-Zuweisung und -Entfernung sind **nicht Teil** dieser Story.


## Implementierungsplan (Douglas)

### Machbarkeit

**Reine UI-Änderung: Ja.** Backend und generierte API-Clients bleiben unverändert.

- **Geräte eines Raums:** Es gibt keinen dedizierten Endpunkt, aber `DeviceDto` enthält `roomId`. `devicesGetAllGETApiDevices()` liefert alle Geräte inkl. Unter-DTOs (`lamp`, `wallPlug`, `thermostat` usw.); die Raumdetailseite filtert clientseitig auf `roomId === :id`. Bei einem Heim-System mit wenigen Dutzend Geräten ist das unkritisch. (`GET /devices/room/{roomId}/summary` und `.../avgtemperature` liefern nur Aggregate, nicht die Geräteliste.)
- **Zeitpläne eines Raums:** Es gibt keinen Raumfilter. `apiRulesAlarmsGet()` liefert alle Alarme; Temperaturpläne haben `contentType === TemperatureSchedule (1)` und `contentJson.roomId`. Filterung clientseitig, analog zu `alarms.component.ts`.
- **Validierung und Konflikte (8–30 °C, gleicher Zeitpunkt im selben Raum):** Bereits serverseitig im Rules-Service (`RoomTemperatureScheduleRules.HasConflict`, `RulesController` liefert `400`/`409` mit `{ message }`). Das Alarm-Modal zeigt diese Meldung bereits an (`onSaveError`). Damit gelten automatisch dieselben Regeln und Meldungen.
- Backend-Änderung wäre nur als spätere Optimierung sinnvoll (`GET /api/devices?roomId=` bzw. `GET /api/rules/alarms?roomId=`), ist aber nicht nötig. Falls Chris es später wünscht, wird vorher abgestimmt.

### Prüfung ExtendedProperties

1. **Definition/Befüllung:** `ExtendedPropertyDto { Key, Value }` ist in `DeviceDto.ExtendedProperties` (`Libs/Sarah.API/DTOs/DeviceDto.cs`) und in `DashboardItemDto` definiert. Befüllt wird es im DeviceService nur in `DevicesController.BuildExtendedProperties(nodeId)`; das Dashboard (`DashboardService.MapToDtoAsync`) kopiert es pro Gerät aus `GET /api/devices/{id}`. Keys je Gerätetyp: Lampe `IsOn`, `Color`, `Brightness`; Steckdose `IsOn`, `Meter_W`; Thermostat `Temperature`, `TemperatureSetpoint`, `Battery`; Türsensor `IsOpen`, `LastOpenDurationMinutes`, `LastStateChanged`; Multisensor `Presence`, `Luminance`, `Temperature`, `RelativeHumidity`, `CO2`, `VOC`, `Battery`. Werte sind Strings (`True`/`False`, invariante Zahlen).
2. **Geräteliste:** `GET /api/devices` (`GetAll`) befüllt `ExtendedProperties` **nicht**. Es liefert nur `Info`, `Lamp`, `WallPlug`, `Thermostat` (nur Soll), `AirQuality`, `DoorSensor`, `Battery`. Nur `GET /api/devices/{id}` liefert `ExtendedProperties`. Folge: Die Geräteseite und damit die Raumdetailseite bekommen es aus der Liste nicht.
3. **TS-Client:** Ja, `DeviceDto.extendedProperties?: Array<ExtendedPropertyDtoModel> | null` ist generiert (`services/api/device-service/model/deviceDto.ts`). Der Listenaufruf `devicesGetAllGETApiDevices()` ist aber als `Observable<any>` typisiert; Feld ist zur Laufzeit leer/`null`.
4. **Dashboard-Widgets:** `DashboardItemViewModel` (`home/home.component.ts`) liest per `extendedProperties?.find(x => x.key === ...)` (u. a. `IsOn`, `Temperature`, `TemperatureSetpoint`, `CO2`, `IsOpen`, `Battery`). Der Steuerdialog parst `extendedProperties` aus `devicesGetDeviceByIdGETApiDevicesId`.

**Entscheidung (von Chris getroffen): reine UI, keine Backend-Änderung.**

- Die Raumdetailseite lädt die Geräte des Raums (Filter `roomId` aus `devicesGetAllGETApiDevices()`) und ruft pro Raumgerät zusätzlich `devicesGetDeviceByIdGETApiDevicesId(id)` parallel per `forkJoin` auf. Der Zustand wird aus `extendedProperties` gelesen (inkl. Ist-Temperatur und Messwerte).
- Die Dashboard-Key-Namen sind **verbindlich** für den gemeinsamen Status-Helper: `IsOn`, `Color`, `Brightness`, `Meter_W`, `Temperature`, `TemperatureSetpoint`, `Battery`, `IsOpen`, `LastOpenDurationMinutes`, `LastStateChanged`, `Presence`, `Luminance`, `RelativeHumidity`, `CO2`, `VOC`. Werte sind Strings (`True`/`False`, invariante Zahlen).
- Der Status-Helper nutzt `extendedProperties` als Quelle; die Geräteseite (Listendaten ohne `extendedProperties`) fällt weiterhin auf die Unter-DTOs zurück. Verhalten dort unverändert.
- Verworfen: Variante B (nur Listendaten) und C (Backend `GetAll` erweitern).

### Betroffene Dateien/Komponenten (sarah.client/src/app)

- `rooms/rooms.component.html`: Button "Details" in der Aktionsspalte (`routerLink` auf `/rooms/:id`). Derzeit existiert kein Details-Button.
- `app-routing.module.ts`: neue Route `rooms/:id`.
- `app.module.ts`: neue Komponente deklarieren (ggf. Modul-Imports prüfen).
- **Neu:** `rooms/room-detail/room-detail.component.{ts,html,css,spec.ts}`.
- `devices/device-control-modal/*`: unverändert wiederverwendet (`show(id, name, typeName)`); `<app-device-control-modal>` wird in der neuen Seite eingebettet.
- `alarms/alarm-edit-modal/alarm-edit-modal.component.{ts,html}`: kleine Erweiterung (siehe Schritte), `alarmSaved`-Event wird wiederverwendet.
- i18n-Dateien (de/en): neue Schlüssel (`rooms.details`, `roomDetail.*`).
- Bereits vorhandene `home/details/room/roomdetails` ist ein Dashboard-Widget und wird **nicht** verändert oder wiederverwendet (anderer Zweck, `@Input id`).

### Routing

- `/rooms/:id` → `RoomDetailComponent`; `id` aus `ActivatedRoute.paramMap`. Ungültige ID oder 404 von `apiRoomsIdGet` → Fehlermeldung mit Link zurück.
- "Zurück"-Button: `routerLink="/rooms"` (bzw. `Location.back()` mit Fallback).
- Titel: Raumname.

### Schritte

1. Route und leere `RoomDetailComponent` anlegen; Details-Button in der Raumtabelle ergänzen.
2. Laden: `forkJoin` über `apiRoomsIdGet`, `devicesGetAllGETApiDevices`, `apiRulesAlarmsGet`; danach zweiter `forkJoin` über `devicesGetDeviceByIdGETApiDevicesId` für alle Raumgeräte (parallel, Einzelfehler pro Gerät per `catchError` abfangen, Kachel zeigt dann "Zustand nicht verfügbar" statt die ganze Seite zu blockieren). Ladeanzeige und Fehlerzustand (Bootstrap-Spinner/Alert wie in den anderen Seiten).
3. Geräte als Kacheln (Bootstrap-Cards, responsiv `col-12 col-sm-6 col-lg-4`) rendern: Name, Typ, Zustand. Leerzustand bei 0 Geräten (dann entfällt der zweite `forkJoin`).
4. Status-Helper: Statuslogik aus `DevicesComponent` (`getStatusCategory`, `getLampIsOn`, `getWallplugIsOn`, `getDoorIsOpen`, `getThermostatSetpoint`, `getAirQualityCO2`) in `devices/device-state.helpers.ts` extrahieren und in beiden Seiten nutzen. Quelle: `extendedProperties` mit den verbindlichen Dashboard-Keys (siehe Entscheidung), Fallback auf Unter-DTOs für die Geräteseite. Reines Refactoring ohne Verhaltensänderung der Geräteseite; Raumkacheln zeigen zusätzlich Ist-/Soll-Temperatur und Messwerte.
5. Kachelklick → `deviceControlModal.show(...)`. Nach Schließen (`hidden.bs.modal`) nur die Einzelzustände der Raumgeräte per parallelem `forkJoin` (`devicesGetDeviceByIdGET...`) neu laden, nicht die gesamte Seite. Dazu in `DeviceControlModalComponent` ein `@Output() closed` ergänzen (additiv, Geräteseite bleibt unberührt). Optional (Could): periodisches Polling.
6. Zeitpläne filtern (`contentType === 1` und `contentJson.roomId === id`) und als Liste rendern: Zieltemperatur, Wiederholung/Zeitpunkte (Wochentage + Uhrzeit aus `serializedRecurrence`/`alarmTime`, lokal formatiert, Aktiv-Status). Leerzustand mit Button "Zeitplan erstellen".
7. Zeitplan erstellen/bearbeiten: `<alarm-edit-modal>` einbetten. Neue Methode `openTemperatureScheduleForRoom(roomId)` setzt Raum vor und deaktiviert das Raum-Select (`lockRoom`-Flag, auch beim Bearbeiten). Beim Speichern ist `roomId` bereits im Formular, und das Modal sendet `Number(roomId)`. Das Feld muss beim Senden trotz `disabled` mitgeschickt werden (`getRawValue()` bzw. `lockedRoomId`), sonst fehlt `roomId` in `form.value`.
8. Löschen: Bestätigung über `DialogService.showConfirmDialog`, danach `apiRulesAlarmsIdDelete` und Liste neu laden. Das bestehende Modal hat einen eigenen Löschen-Button; für die Liste wird ein Löschen-Icon mit Rückfrage ergänzt.
9. Mobile: Keine Tabellen mit horizontalem Scroll; Zeitplanliste als Card-/List-Group, Kacheln einspaltig auf `xs`.
10. Übersetzungen und Feinschliff.

### Wiederverwendung

- `DeviceControlModalComponent` (Steuerdialog komplett).
- Zustandsanzeige-Logik der `DevicesComponent` (via Extraktion).
- `AlarmEditModalComponent` (Formular, Validierung 8–30 °C, Wochentage/Zeitpunkte, Fehlermeldungen vom Server), `AlarmContentTypeModel`.
- `RoomsClient`, `DevicesClient`, `RulesClient` (generierte Clients), `DialogService`, `LoggingService`, `translate`-Pipe.

### Testplan (Karma/Jasmine, `npm test`)

- `room-detail.component.spec.ts`: Raumtitel aus Route; Geräte werden nach `roomId` gefiltert; Leerzustand ohne Geräte; Kachelklick ruft `deviceControlModal.show` mit korrekten Parametern; Neuladen der Geräte nach Schließen; nur Temperaturpläne des Raums werden gelistet (andere Räume und Text-Alarme ausgeblendet); Leerzustand und "Zeitplan erstellen" öffnet Modal mit vorausgewähltem Raum; Löschen mit Rückfrage (abgebrochen/bestätigt); Lade- und Fehlerzustand.
- `alarm-edit-modal.component.spec.ts`: neuer Fall `openTemperatureScheduleForRoom` (Raum vorbelegt, gesperrt, `roomId` im gespeicherten `contentJson`; Temperatur 7/31 ungültig; Serverfehler 409 wird angezeigt).
- `rooms.component.spec.ts`: Details-Button verlinkt auf `/rooms/:id`.
- `devices.component.spec.ts` und Spec der Helfer: Refactoring ohne Verhaltensänderung (bestehende Tests grün).
- Manuelle Prüfung im Browser auf Smartphone-Breite (kein seitliches Scrollen).

### Risiken

- **Clientseitige Filterung:** Lädt alle Geräte/Alarme; bei sehr vielen Einträgen ineffizient. Mit aktueller Größe vertretbar.
- **N Aufrufe (Variante A):** Pro Laden/Neuladen entstehen N Einzelaufrufe (N = Geräte des Raums, typischerweise wenige). Mitigation: Aufrufe laufen parallel (`forkJoin`), nur Raumgeräte werden abgefragt, Einzelfehler blockieren die Seite nicht, und nach Dialog-Schluss werden nur die Gerätezustände neu geladen. Bei später spürbarer Last ist eine Backend-Optimierung (`ExtendedProperties` in `GetAll`) möglich, aber nur nach separater Abstimmung.
- **Typisierung:** `devicesGetAllGETApiDevices()` ist als `Observable<any>` typisiert; im Helper defensiv behandeln (Typ-Guard).
- **Aktualität nach Dialog:** Dialogänderungen werden erst nach dem Schließen sichtbar (laut Story akzeptiert); Live-Updates sind Could.
- **Bearbeiten außerhalb der Seite:** Ein Raumwechsel im bestehenden Modal wird durch Sperrung des Select verhindert; die Alarm-Kalenderseite bleibt unverändert.
- **Zeitzonen:** Anzeige der Zeitpunkte muss lokale Zeit nutzen (wie im Modal), sonst Abweichungen bei Sommerzeit.
- **Modal-Duplikate:** `alarm-edit-modal` und `app-device-control-modal` nutzen feste DOM-IDs; nur je eine Instanz pro Seite einbetten.
