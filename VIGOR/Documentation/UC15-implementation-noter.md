# UC15 – Statistik og rapporter

## Status
UC15 er implementeret som en simpel statistikside uden eksport.

## Implementeret funktionalitet
- Simpel statistikside: `/statistics`
- Antal statusændringer
- Antal registreringer af fast medicin som givet
- Antal PN-medicinregistreringer
- Antal opgaver oprettet
- Antal opgaver afsluttet
- Antal åbne opgaver i valgt periode
- Simpelt datofilter med fra/til-dato
- Lederadgang til statistik for egen afdeling
- Superbrugeradgang til anonym systemstatistik
- Ingen eksport i første version
- Service-tests
- Controller-tests

## Rolle- og GDPR-afgrænsning
Leder er driftsrolle og får statistik for egen afdeling.

Superbruger er teknisk/systemmæssig administrator og får ikke automatisk adgang til borgerdata. I UC15 kan Superbruger dog se aggregerede systemtal, fordi DTO'en ikke indeholder borgernavne, afdelingsnavne, medicinnavne, noter, opgavetekster eller brugerreferencer.

Personale og Vagtansvarlig har ikke adgang til statistikmodulet i denne første version.

## Runtime-path
Statistics.razor
→ StatisticsClientService
→ StatisticsApiController
→ StatisticsService
→ AppDbContext
→ AuditEvents + CitizenTasks

## Bevidst afgrænsning
- Ingen eksport
- Ingen avancerede grafer
- Ingen borger-, medarbejder- eller medicindetaljer
- Ingen afdelingsvælger for Superbruger
- Ingen fuld rapportgenerator
- UI-polering tages samlet til sidst

## Diagram-gæld
UC15 påvirker især:
- DCD / klassediagram: StatisticsClientService, StatisticsApiController, StatisticsService, IStatisticsService, IStatisticsApi
- Sekvensdiagram: statistik runtime-path
- Sikkerhed/GDPR-beskrivelse: Superbruger ser kun anonym/aggregeret statistik
