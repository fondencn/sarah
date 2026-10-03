using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sarah.Rules.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptRulesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromptRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Guidance = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TimerHour = table.Column<int>(type: "integer", nullable: true),
                    TimerMinute = table.Column<int>(type: "integer", nullable: true),
                    TimerWeekdays = table.Column<long>(type: "bigint", nullable: true),
                    TimerInterval = table.Column<int>(type: "integer", nullable: true),
                    TimerFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimerUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromptRules_IsEnabled",
                table: "PromptRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_PromptRules_Name",
                table: "PromptRules",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PromptRules_SortOrder",
                table: "PromptRules",
                column: "SortOrder");

            migrationBuilder.Sql(@"
INSERT INTO ""PromptRules"" (
    ""Id"", ""Name"", ""Guidance"", ""IsEnabled"", ""SortOrder"",
    ""TimerHour"", ""TimerMinute"", ""TimerWeekdays"", ""TimerInterval"", ""TimerFromUtc"", ""TimerUntilUtc"",
    ""CreatedAtUtc"", ""UpdatedAtUtc""
) VALUES
(1, 'Keyfob41 Schalter 1 schaltet LED 30 an/aus', 'Wenn ein ClickedEvent von Node 41 mit SceneId 1 kommt, schalte Lampe Node 30 um.', true, 1, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(2, 'Keyfob41 Schalter 2 schaltet Lampe 21 an/aus', 'Wenn ein ClickedEvent von Node 41 mit SceneId 2 kommt, schalte Lampe Node 21 um.', true, 2, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(3, 'Tuere Arbeitszimmer offen ohne bekannte Person', 'Wenn die Tuer im Arbeitszimmer offen ist und keine bekannte Person zuhause ist, melde das per Sprache.', true, 3, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(4, 'Haustuere offen ohne bekannte Person', 'Wenn die Haustuer offen ist und keine bekannte Person zuhause ist, gib eine sehr laute Warnung aus, starte alert1.wav und aktiviere die Szene RedAlert.', true, 4, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(5, 'Haustuere geschlossen beendet Alarm', 'Wenn die Haustuer geschlossen wird und jemand zuhause ist, stoppe die Szene RedAlert und stoppe die Audio-Wiedergabe.', true, 5, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(6, 'Lampe 14 bei Praesenz im Arbeitszimmer', 'Wenn im Arbeitszimmer Praesenz erkannt wird und die Helligkeit unter 40 liegt, setze Lampe 14 auf Helligkeit 255 und Farbe #FFBC1F.', true, 6, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(7, 'Morgens Guten Morgen sagen', 'Wenn Christian morgens zwischen 7 und 10 Uhr im Arbeitszimmer anwesend ist, gib einmal pro Tag eine Begruessung mit Uhrzeit, aktuellem Wetter und Wettervorhersage auf speaker1 aus.', true, 7, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(8, 'Lampe 14 aus bei keiner Praesenz', 'Wenn im Arbeitszimmer keine Praesenz mehr erkannt wird, schalte Lampe 14 aus.', true, 8, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(9, 'Trockner fertig', 'Wenn WallPlug Node 251 auf aus/faellt, sage: Der Waeschetrockner ist fertig.', true, 9, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(10, 'Waschmaschine fertig', 'Wenn WallPlug Node 252 auf aus/faellt, sage: Die Waschmaschine ist fertig.', true, 10, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(11, 'Kaffeemaschine fertig', 'Wenn WallPlug Node 20 auf aus/faellt, schalte Node 20 aus und sage: Der Kaffee ist fertig.', true, 11, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(12, 'Kaffeemaschine an bei Christian Heimkehr', 'Wenn Christian zwischen 7 und 11 Uhr nach Hause kommt, schalte Steckdose Node 20 ein und sage, dass die Kaffeemaschine eingeschaltet wird.', true, 12, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(13, 'Luftqualitaet Arbeitszimmer', 'Wenn die Luftqualitaet im Arbeitszimmer kritisch ist und dort jemand anwesend ist, gib eine passende Luftqualitaetsansage aus.', true, 13, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(14, 'Feueralarm Node 38', 'Wenn Rauchmelder Node 38 Feueralarm meldet, sage laut, dass Rauchmelder 38 Feueralarm meldet.', true, 14, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(15, 'Tracker SOS Lukas', 'Wenn Tracker Node 247 den SOS-Knopf drueckt, sage: Warnung: Lukas hat den SOS Knopf seines Trackers gedrueckt.', true, 15, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(16, 'Tracker SOS Christian', 'Wenn Tracker Node 246 den SOS-Knopf drueckt, sage: Warnung: Christian hat den SOS Knopf seines Trackers gedrueckt.', true, 16, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(17, 'Tracker SOS Hannah', 'Wenn Tracker Node 245 den SOS-Knopf drueckt, sage: Warnung: Hannah hat den SOSKnopf ihres Trackers gedrueckt.', true, 17, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(18, 'Mittagspause Erinnerung', 'Um 12:00 Uhr an Werktagen erinnere an die Mittagspause auf speaker1 und setze Lampe 14 auf rot.', true, 18, 12, 0, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(19, 'Ende Mittagspause Erinnerung', 'Um 14:00 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, erinnere an das Weiterarbeiten auf speaker1 und schalte Lampe 14 aus.', true, 19, 14, 0, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(20, 'Daily Erinnerung 8:00', 'Um 8:00 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, sage: Es ist Zeit fuers Daily, und setze Lampe 14 auf blau.', true, 20, 8, 0, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(21, 'Daily Ende 8:20', 'Um 8:20 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, sage: Das Daily sollte nun zu Ende sein, und setze Lampe 14 auf gruen.', true, 21, 8, 20, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(22, 'Lampe Wohnzimmer aus 6:45', 'Um 6:45 Uhr an Werktagen schalte Lampe 24 aus.', true, 22, 6, 45, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(23, 'Daily Lampe aus 8:21', 'Um 8:21 Uhr an Werktagen schalte Lampe 14 aus.', true, 23, 8, 21, 31, 0, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(24, 'Batteriewarnungen', 'Wenn BatteryWarningEvent kritische Batterien meldet, gib eine Sprachausgabe mit den betroffenen Geraeten aus.', true, 24, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(25, 'DoorMonitor Meldungen', 'Wenn DoorMonitorAlertEvent ein Fenster oder eine Tuer meldet, gib die passende Sprachausgabe aus. Bei IsLoud=true verwende sehr laute Lautstaerke.', true, 25, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(26, 'Wetterwarnungen', 'Wenn WeatherWarningEvent eine Warnung enthaelt, gib eine Wetterwarnung per Sprache aus.', true, 26, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00'),
(27, 'Wettervorhersage abends', 'Wenn zwischen 18 und 19 Uhr eine Wettervorhersage fuer heute aktualisiert wird, gib die Wettervorhersage per Sprache aus.', true, 27, NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-04-28 00:00:00+00', TIMESTAMPTZ '2026-04-28 00:00:00+00');

SELECT setval(pg_get_serial_sequence('""PromptRules""', 'Id'), (SELECT MAX(""Id"") FROM ""PromptRules""));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromptRules");
        }
    }
}
