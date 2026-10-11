# Wiederkehrende Raumtemperatur-Zeitpläne

## User Story

Als Nutzer:in möchte ich wiederkehrende Zieltemperaturen für einzelne Räume planen und verwalten, damit die gewünschte Temperatur zu den passenden Zeitpunkten automatisch eingestellt wird.

## Umfang und Verhalten

- Nutzer:innen können Zeitpläne erstellen, ansehen, ändern und löschen.
- Ein Zeitplan verknüpft einen Raum und eine Zieltemperatur in Celsius mit wiederkehrenden einzelnen Zeitpunkten. Für Wiederholungen und Kalender wird das bestehende Alarm-Schedule-Modul wiederverwendet. Start-/Endzeiträume sind nicht vorgesehen.
- Zieltemperaturen sind inklusive auf **8–30 °C** begrenzt.
- Zwei Zeitpläne stehen in Konflikt, wenn sie für denselben Raum mindestens einen gemeinsamen geplanten Zeitpunkt haben.
- Bei einem geplanten Temperaturanstieg wird die Änderung unterdrückt, wenn die aktuelle Außentemperatur **20 °C oder höher** ist. Gleichheit bei 20 °C unterdrückt den Anstieg ausdrücklich. Geplante Absenkungen werden nie aufgrund der Außentemperatur unterdrückt. Ist die Außentemperatur nicht verfügbar, wird diese Regel ignoriert und die geplante Zieltemperatur ausgeführt.
- Die Bedienoberfläche ist intuitiv und folgt den vorhandenen Mustern des Alarm-Schedule-Moduls.

## Akzeptanzkriterien

1. **Zeitpläne verwalten**  
   **Gegeben** ein Nutzerkonto und verfügbare Räume, **wenn** Nutzer:innen einen Zeitplan erstellen, ansehen, ändern oder löschen, **dann** werden diese Aktionen für den gewählten Raum und Zeitplan ausgeführt und die Änderung ist in der Zeitplanübersicht sichtbar.

2. **Wiederholungen und Zeitpunkte**  
   **Gegeben** die Zeitplan-Erstellung, **wenn** Nutzer:innen Wiederholungen und einzelne Zeitpunkte konfigurieren, **dann** stehen dafür die Wiederholungs- und Kalenderfunktionen des Alarm-Schedule-Moduls zur Verfügung und es gibt keine Start-/Endzeitraum-Eingabe.

3. **Temperaturbereich**  
   **Gegeben** ein Zeitplan wird erstellt oder geändert, **wenn** eine Zieltemperatur eingegeben wird, **dann** werden Werte von 8 °C bis einschließlich 30 °C akzeptiert und Werte außerhalb dieses Bereichs abgewiesen.

4. **Konflikte**  
   **Gegeben** ein bestehender Zeitplan für einen Raum, **wenn** Nutzer:innen einen wiederkehrenden Zeitplan für denselben Raum erstellen oder bearbeiten und dieser mindestens einen gemeinsamen geplanten Zeitpunkt mit dem bestehenden Zeitplan hat, **dann** wird das Erstellen oder Speichern abgewiesen und eine klare Meldung weist auf den Zeitplankonflikt hin. Bei unterschiedlichen Räumen liegt bei gleichem Zeitpunkt kein Konflikt vor.

5. **Unterdrückung eines Anstiegs**  
   **Gegeben** der geplante Zielwert liegt über der aktuellen Raum-Solltemperatur, **wenn** die Außentemperatur zum geplanten Zeitpunkt mindestens 20 °C beträgt, **dann** wird die Erhöhung nicht ausgeführt.

6. **Absenkung und Außentemperaturgrenze**  
   **Gegeben** eine geplante Absenkung oder eine Außentemperatur unter 20 °C, **wenn** der Zeitpunkt erreicht wird, **dann** wird der geplante Zielwert ausgeführt. Eine Erhöhung wird bei genau 20 °C unterdrückt.

7. **Außentemperatur nicht verfügbar**  
   **Gegeben** die Außentemperatur ist zum geplanten Zeitpunkt nicht verfügbar, **wenn** der Zeitplan ausgelöst wird, **dann** wird der geplante Zielwert unabhängig davon ausgeführt, ob er eine Erhöhung oder Absenkung ist.

8. **Bedienmuster**  
   **Gegeben** Nutzer:innen verwalten Raumtemperatur-Zeitpläne, **wenn** sie die Oberfläche bedienen, **dann** können sie die zentralen Aktionen mit den vertrauten Mustern des Alarm-Schedule-Moduls finden und ausführen.

## MoSCoW-Priorität

- **Must:** Wiederkehrende Zeitpläne verwalten, einzelne Zeitpunkte und bestehende Alarm-Schedule-Funktionen nutzen, eine intuitive Oberfläche mit wiederverwendeten Alarm-Schedule-Bedienmustern bereitstellen, den Temperaturbereich durchsetzen, Konflikte verhindern und die Außentemperaturregel wie beschrieben anwenden.
