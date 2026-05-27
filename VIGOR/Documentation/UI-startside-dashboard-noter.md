# UI-opdatering – startside/dashboard

Dato: 26. maj 2026

## Formål

Den første side efter login er opdateret, så VIGOR får en mere professionel startoplevelse baseret på designreferencen `personalestart.html`.

## Implementeret

- Ny fælles Blazor-komponent: `StartDashboard.razor`
- Opdateret administrationsstartside til at bruge dashboarddesignet
- Opdateret personale-/fallback-startside til at bruge dashboarddesignet
- Opdateret vagtansvarlig/fallback-startside til at bruge dashboarddesignet
- Opdateret `Vælg aktiv vagt`-siden med samme visuelle designretning
- Tilføjet fælles CSS til dashboard/startside i `VIGOR.Shared/wwwroot/app.css`

## Rollebaseret visning

Dashboardet viser kun links og handlinger, som den aktuelle rolle må tilgå:

- Personale: vagtvalg, borgere og overlap
- Vagtansvarlig: vagtvalg, borgere, overlap og arbejdstelefoner
- Leder: driftsfunktioner, brugeradministration, arbejdstelefoner og statistik
- Superbruger: teknisk systemadministration, bruger/rolle-administration, arbejdstelefonopsætning og anonym systemstatistik

Ren Superbruger får ikke vist borger-, medicin-, note-, opgave- eller overlaplinks. Det understøtter GDPR-afgrænsningen mellem teknisk systemadministration og faglig/driftsmæssig adgang.

## Bevidst afgrænsning

Startdashboardet er en UI/UX-opgradering og bruger primært navigationskort og rollebaseret adgangsvisning. De eksisterende runtime-flows bevares:

- Driftsroller sendes stadig til vagtvalg som start-flow
- Vagtvalg gemmes fortsat gennem `ShiftSelectionApi`
- Borgere, overlap, arbejdstelefoner, brugeradministration og statistik åbnes via deres eksisterende sider

## Testforslag

- Log ind som Personale og bekræft at første side/vagtvalg har ny visuel stil
- Log ind som Vagtansvarlig og bekræft adgang til arbejdstelefoner
- Log ind som Leder og bekræft adgang til brugere, statistik, arbejdstelefoner og driftslinks
- Log ind som ren Superbruger og bekræft at borger-/vagt-/overlaplinks ikke vises
- Bekræft at vagtvalg stadig gemmer og sender videre til borgeroversigt
