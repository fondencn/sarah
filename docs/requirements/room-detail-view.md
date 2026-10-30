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

## Offene Fragen

- Keine.
