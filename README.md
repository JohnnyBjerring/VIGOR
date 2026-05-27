# VIGOR – Digitalt overlapsystem

**VIGOR** (*Visual Integrated Governance & Overlap Registry*) er et digitalt overlapsystem til bostedet Slottet. Systemet er udviklet som en afgrænset eksamensprototype, der skal vise, hvordan en Excel-baseret arbejdsgang kan erstattes af en mere struktureret, sikker og sporbar digital løsning.

GitHub-repository: <https://github.com/JohnnyBjerring/VIGOR>

Projektet er udviklet som en del af 3. semester på datamatikeruddannelsen og anvendes som grundlag for eksamen i Systemudvikling II, Programmering II og Teknologi II.

---

## Formål

Formålet med VIGOR er at understøtte personalets arbejde ved vagtskifte på et bosted, hvor der er behov for hurtigt overblik, korrekt medicinregistrering og sporbarhed.

Systemet har fokus på:

- login og rollebaseret adgang
- visning af borgere pr. afdeling
- trafiklys-/risikostatus for borgere
- registrering af fast medicin
- registrering af PN-medicin ved behov
- valg af aktiv vagtkontekst
- historik/audit-log for centrale handlinger
- tydelig opdeling mellem UI, client service, API, server-service og database

---

## Aktuel status

Følgende use cases er implementeret i den aktuelle prototype:

| Use case | Status | Bemærkning |
|---|---|---|
| UC01 – Log ind | Implementeret | Login via API/JWT og rollebaseret adgang |
| UC02 – Se borgere på afdeling | Implementeret | Serveren udleder afdeling ud fra den indloggede bruger |
| UC03 – Opdatér borgerstatus / risikoprofil | Implementeret | Trafiklysstatus kan opdateres via UI/API/service |
| UC04 – Registrér fast medicin | Implementeret | Fast medicin kan registreres som givet, tidspunkt kan vælges/rettes, og registrering kan annulleres |
| UC05 – Registrér PN-medicin | Implementeret | PN-medicin kan registreres med medicin, dosis, årsag, tidspunkt, brugerreference, afdeling og vagt |
| UC06 – Se historik / audit-log | Implementeret | Audit-events oprettes og vises pr. borger |
| UC07 – Vælg vagt | Implementeret | Aktiv vagt kan vælges som dagvagt, aftenvagt eller nattevagt |

UC08–UC15 er ikke fuldt implementeret i den aktuelle prototype og behandles som fremtidige udvidelser.

---

## Bevidste afgrænsninger

Den aktuelle løsning er en eksamensprototype og ikke et færdigt produktionssystem.

Bevidste afgrænsninger:

- Systemet er ikke et komplet fagsystem.
- Der er ingen integration til FMK eller regionale systemer.
- Der er ingen fuld medicinmotor eller avanceret frekvensmotor.
- Public/anonym oversigtsskærm er ikke fuldt implementeret.
- Noter, opgaver, personale-/telefonfordeling og statistik er fremtidige udvidelser.
- GDPR behandles på design- og projektniveau og er ikke en juridisk driftsgodkendelse.

---

## Arkitektur

Løsningen er opdelt i flere projekter for at holde lav kobling og tydelige ansvarsområder.

| Projekt | Ansvar |
|---|---|
| `VIGOR.Shared` | Fælles modeller, DTO'er, enums, interfaces, client services og Blazor-komponenter |
| `VIGOR.Web` | ASP.NET Core Web API, Identity, server-services, dataadgang og EF Core-migrationer |
| `VIGOR.MAUI` | .NET MAUI Blazor Hybrid-klient |
| `VIGOR.Web.Client` | Reserveret til mulig fremtidig web-/kioskklient |
| `VIGOR.UnitTests` | Unit tests for services, controllerlogik og centrale beslutningspunkter |

Den gennemgående runtime-path er:

```text
UI / Blazor-komponent
→ Client service
→ API-controller
→ Server-service
→ ApplicationDbContext
→ Database
```

Audit-log indgår som en tværgående mekanisme i de centrale runtime-paths, hvor serveren opretter audit-events efter succesfuld validering og gemning.

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
- xUnit
- Moq
- Git/GitHub

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

- Testbrugerne er kun til udvikling og demonstration.
- I reel drift skal hver medarbejder have sin egen konto.
- Afdelingsadgang håndhæves på serveren og må ikke styres af klienten alene.

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

## Teststrategi

Projektet anvender en kombination af:

- service-tests
- controller-tests
- manuel runtime-test i MAUI-klienten

De centrale use cases UC01–UC07 er testet efter projektets runtime-first Definition of Done. En use case betragtes først som lukket, når relevant kode bygger, automatiserede tests er grønne, og flowet er manuelt verificeret i den faktiske brugerflade.

---

## Dokumentation

Dokumentation og diagrammer ligger i projektets dokumentationsmapper, eksempelvis:

```text
Documentation/
Documentation/Diagrammer/
```

Vigtige dokumenter og artefakter omfatter blandt andet:

- projektrapporten
- systemudviklingsmetode
- use case-beskrivelser
- ER-diagram
- deployment-diagram
- aktivitetsdiagrammer
- design class diagrammer
- API-kontrakter

Diagrammer og rapport behandles som levende artefakter og opdateres løbende, når funktionalitet og design ændrer sig.

---

## Metode

Projektet følger en iterativ metode baseret på Scrum + XP.

Scrum bruges til:

- backlog og prioritering
- valg af én eller få use cases pr. iteration
- statusopfølgning
- planlægning af næste leverance

XP-principper bruges til:

- simpelt design
- små stabile ændringer
- refaktorering
- tests og build
- ensartet kodestruktur
- løbende feedback fra runtime-test

Hver iteration følger denne arbejdsrytme:

```text
Krav/scope
→ analyse
→ design/kontrakt
→ implementering
→ test
→ dokumentation
→ status
```

Projektets vigtigste arbejdsregel er:

> Koden er sandheden.

Det betyder, at en funktion først beskrives som færdig, når den reelt virker i den faktiske runtime-path.
