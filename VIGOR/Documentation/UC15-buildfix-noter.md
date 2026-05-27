# UC15 buildfix – Statistik og rapporter

Denne rettelse samler de manglende UC15-filer i korrekt solution-mappestruktur og retter Razor-namespace for `[Authorize]`.

## Rettet
- `StatisticsOverviewDto` er inkluderet under `VIGOR.Shared/DTOs`.
- `IStatisticsApi` er inkluderet under `VIGOR.Shared/Interfaces/Services`.
- `StatisticsClientService` er inkluderet under `VIGOR.Shared/Services`.
- `StatisticsApiController` er inkluderet under `VIGOR.Web/Controllers/Api`.
- `Statistics.razor` har nu korrekt `@using Microsoft.AspNetCore.Authorization`.
- `_Imports.razor` har også `@using Microsoft.AspNetCore.Authorization`, så Authorization-attributter kan bruges i shared Razor-komponenter.

## Bemærkning
Denne zip er pakket med den samme ydre struktur som den uploadede solution: `VIGOR/...`.
Udpak den derfor fra mappen, der indeholder `VIGOR`-mappen.
