# VIGOR – Digitalt overlapsystem

**VIGOR** (Visual Integrated Governance & Overlap Registry) er et digitalt overlapsystem til bostedet Slottet. Systemet skal erstatte papir-/Excel-baserede overlapsskemaer og give personalet et mere struktureret overblik over borgere, status, medicin og opgaver ved vagtskifte.

Projektet er udviklet som en del af 3. semester på datamatikeruddannelsen og bruges som grundlag for eksamen i Systemudvikling II, Programmering II og Teknologi II.

---

## Formål

Formålet med VIGOR er at understøtte en travl døgnbemandet hverdag, hvor personalet skal kunne overlevere vigtig information hurtigt og sikkert.

Systemet har fokus på:

- login og rollebaseret adgang
- overblik over borgere pr. afdeling
- trafiklys-/risikostatus for borgere
- registrering af fast medicin
- tydelig opdeling mellem klient, API, service og database
- sporbarhed i designet, så historik og audit kan udbygges senere

---

## Aktuel status

Følgende use cases er implementeret eller under færdiggørelse:

| Use case | Status | Bemærkning |
|---|---|---|
| UC01 – Log ind | Implementeret | Login via API/JWT og rollebaseret adgang |
| UC02 – Se borgere på afdeling | Implementeret | Serveren udleder sikker afdeling ud fra login-brugeren |
| UC03 – Opdatér borgerstatus / risikoprofil | Implementeret | Trafiklysstatus kan opdateres via UI/API/service |
| UC04 – Registrér fast medicin | Under færdiggørelse | Fast medicin kan registreres som givet, rettes og annulleres |

UC04 er bevidst afgrænset til **fast medicin og aktuel registrering**. Systemet bygger ikke en fuld medicinmotor i denne use case.

Ikke en del af UC04:

- PN-medicin
- fuld audit-/historikvisning
- FMK-integration
- avanceret frekvensmotor
- rapportering/statistik

Disse områder håndteres i senere use cases.

---

## Arkitektur

Løsningen er opdelt i flere projekter for at holde lav kobling og tydelige ansvarsområder.

| Projekt | Ansvar |
|---|---|
| `VIGOR.Shared` | Fælles modeller, DTO'er, interfaces, services og Blazor-komponenter |
| `VIGOR.Web` | ASP.NET Core Web API, Identity, forretningslogik og dataadgang |
| `VIGOR.MAUI` | .NET MAUI Blazor Hybrid-klient |
| `VIGOR.Web.Client` | Reserveret til fremtidig kiosk-/webklient |
| `VIGOR.UnitTests` | Unit tests for centrale services og controllere |

Den primære runtime-path for UC04 er:

```text
CitizensList.razor
→ IFixedMedicationApi
→ FixedMedicationClientService
→ CitizensApiController
→ IFixedMedicationService
→ FixedMedicationService
→ AppDbContext / SQLite
```

Arkitekturen følger projektets principper:

- GRASP
- SOLID
- KISS
- DRY
- Dependency Injection
- MVC-tankegang

---

## Teknologier

- .NET 10
- .NET MAUI Blazor Hybrid
- ASP.NET Core Web API
- ASP.NET Identity
- JWT authentication
- Entity Framework Core
- SQLite
- xUnit til unit tests
- Git/GitHub til versionsstyring

---

## Metode

Projektet følger en iterativ metode baseret på **Scrum + XP**.

Scrum bruges til:

- backlog og prioritering
- valg af én eller få use cases pr. iteration
- review og retrospektiv

XP-principper bruges til:

- simpelt design
- små stabile ændringer
- refaktorering
- tests og build
- ensartet kodestruktur
- løbende feedback

Hver iteration følger denne arbejdsrytme:

```text
Use case / backlog
→ analyse
→ design
→ implementering
→ test og manuel runtime-verifikation
→ commit/push
→ opdatering af levende artefakter
```

En iteration betragtes først som færdig, når den faktiske funktionalitet virker i runtime, og relevante diagrammer/dokumentation er opdateret ved behov.

---

## Testbrugere

Seeddata opretter følgende testbrugere:

| Rolle | Email | Kode |
|---|---|---|
| Leder | `admin@vigor.dk` | `Admin1234` |
| Vagtansvarlig | `vagtansvarlig@vigor.dk` | `Test1234` |
| Personale | `personale@vigor.dk` | `Test1234` |
| Ingen rolle / denied-test | `norole@vigor.dk` | `Test1234` |

Bemærk:

- Leder og Vagtansvarlig er tilknyttet Afdeling A.
- Personale er tilknyttet Afdeling B.
- Afdelingsadgang håndhæves på serveren.

---

## Opsætning og kørsel

### Forudsætninger

- .NET 10 SDK
- Visual Studio med MAUI workload, hvis MAUI-klienten skal køres
- Git

### Restore, build og test

Fra solution-roden:

```bash
dotnet restore VIGOR.slnx
dotnet build VIGOR.slnx
dotnet test VIGOR/VIGOR.UnitTests/VIGOR.UnitTests.csproj
```

### Kør Web API

```bash
dotnet run --project VIGOR/VIGOR.Web/VIGOR.Web.csproj
```

Ved opstart migrerer databasen automatisk og opretter seeddata, hvis databasen ikke allerede er oprettet.

### Kør MAUI-klient

MAUI-klienten køres bedst fra Visual Studio på Windows via projektet:

```text
VIGOR.MAUI
```

---

## Dokumentation

Dokumentation og diagrammer ligger i:

```text
Documentation/
Documentation/Diagrammer/
```

Vigtige dokumenter:

- `VIgor_Systemudviklingsmetode.pdf`
- `VIGOR_SystemudviklingII_Scrum_XP_metodemodel_Final.drawio`
- `VIGORv6.pdf`

Diagrammer og rapport behandles som levende artefakter og opdateres løbende, når funktionalitet og design ændrer sig.

---

## Kendte afgrænsninger

Den aktuelle løsning er et eksamensprojekt og ikke et færdigt produktionssystem.

Bevidste afgrænsninger:

- UC04 gemmer aktuel medicinregistrering, men ikke fuld append-only audit endnu.
- PN-medicin er ikke implementeret endnu.
- Historik/audit-log er planlagt som separat use case.
- FMK-integration er uden for nuværende scope.
- `VIGOR.Web.Client` er reserveret til senere brug.

---

## Projektfokus

Projektets vigtigste fokus er ikke kun at levere funktionalitet, men også at vise en struktureret systemudviklingsproces med:

- tydelig use case-prioritering
- sporbarhed fra krav til kode og test
- lagdelt arkitektur
- testbar kode
- realistisk scope-afgrænsning
- løbende dokumentation
