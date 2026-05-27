# VIGOR – Digitalt overlapsystem

**VIGOR** (*Visual Integrated Governance & Overlap Registry*) er et digitalt overlapsystem til bostedet Slottet. Systemet er udviklet som en afgrænset eksamensprototype, der viser, hvordan et papir-/Excel-baseret overlapsskema kan erstattes af en mere struktureret, sikker og sporbar digital løsning.

GitHub-repository: <https://github.com/JohnnyBjerring/VIGOR>

Projektet er udviklet som en del af 3. semester på datamatikeruddannelsen og anvendes som grundlag for eksamen i Systemudvikling II, Programmering II og Teknologi II.

---

## Formål

Formålet med VIGOR er at understøtte personalets arbejde ved vagtskifte på et bosted, hvor der er behov for hurtigt overblik, korrekt medicinregistrering og sporbarhed.

Systemet har fokus på:

- login og rollebaseret adgang
- valg af aktiv vagtkontekst: dagvagt, aftenvagt eller nattevagt
- visning af borgere pr. afdeling
- trafiklys-/risikostatus for borgere
- registrering af fast medicin
- registrering af PN-medicin ved behov
- oprettelse af noter og opgaver
- tildeling af personale og arbejdstelefoner
- samlet overlapvisning til vagtskifte
- anonym public oversigtsskærm uden persondata
- statistik for ledelse/systemoverblik
- historik/audit-log for centrale borgerrelaterede handlinger
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
| UC08 – Se overlap | Implementeret | Samlet overlapvisning med status, medicin, opgaver, noter, tildelt personale, telefoner og historik |
| UC09 – Opret note | Implementeret | Noter kan oprettes på borger og indgår i borgerdetalje/overlap |
| UC10 – Opret og afslut opgave | Implementeret | Opgaver kan oprettes, vises som aktive og markeres som afsluttet |
| UC11 – Tildel personale til borger | Implementeret | Vagtansvarlig/Leder kan tildele og fjerne aktivt personale på borger |
| UC12 – Tildel telefon til personale | Implementeret | Arbejdstelefoner kan oprettes, tildeles og fjernes fra personale |
| UC13 – Administrér brugere og roller | Implementeret | Leder/Superbruger kan oprette brugere, ændre roller og aktivere/deaktivere brugere |
| UC14 – Public anonym oversigtsskærm | Implementeret | Public/kiosk-visning uden login, kun med anonymiserede statusdata |
| UC15 – Statistik og rapporter | Implementeret | Simpel statistikside med datofilter og aggregerede nøgletal |

---

## Bevidste afgrænsninger

Den aktuelle løsning er en eksamensprototype og ikke et færdigt produktionssystem.

Bevidste afgrænsninger:

- Systemet er ikke et komplet fagsystem.
- Der er ingen integration til FMK eller regionale systemer.
- Der er ingen fuld medicinmotor eller avanceret frekvensmotor.
- Statistikmodulet er en simpel første version uden eksport/rapportgenerator.
- Arbejdstelefoner og personaletildeling er praktisk driftsfunktionalitet, ikke fuld vagtplanlægning.
- Brugeradministration er en smal adminfunktion og ikke et komplet enterprise permission-system.
- System-/adminhandlinger har ikke separat system-audit endnu; den eksisterende audit-log er primært borgerrelateret.
- Public/anonym oversigtsskærm viser kun dataminimerede statusdata og må ikke bruges som intern arbejdsvisning.
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

Audit-log indgår som en tværgående mekanisme i de centrale borgerrelaterede runtime-paths, hvor serveren opretter audit-events efter succesfuld validering og gemning.

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

Seeddata/testopsætning anvender følgende brugere til demonstration og test:

| Rolle / formål | Email | Kode | Forventet adgang |
|---|---|---|---|
| Leder | `admin@vigor.dk` | `Admin1234` | Administration, brugere, statistik, borgere, overlap og vagtvalg |
| Vagtansvarlig | `vagtansvarlig@vigor.dk` | `Test1234` | Vagtvalg, borgere, overlap, personaletildeling og arbejdstelefoner |
| Personale | `personale@vigor.dk` | `Test1234` | Vagtvalg, borgere, medicin, noter, opgaver og overlap |
| Superbruger / systemadmin | `JTB@Vigor.dk` | `Johnny1234` | System-/adminadgang og teknisk administration |
| Ingen rolle / denied-test | `norole@vigor.dk` | `Test1234` | Test af afvist adgang uden rolle |

Bemærk:

- Testbrugerne er kun til udvikling og demonstration.
- I reel drift skal hver medarbejder have sin egen konto.
- Afdelingsadgang håndhæves på serveren og må ikke styres af klienten alene.
- Superbruger er teknisk/systemmæssig administrator og bør ikke automatisk have faglig adgang til borgerdata uden relevant driftsrolle.

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

### Typisk demo-flow

```text
1. Start Web API
2. Start MAUI-klient
3. Log ind med en testbruger
4. Vælg aktiv vagt, hvis brugeren er en driftsrolle
5. Gennemgå borgeroversigt, borgerdetalje, overlap, arbejdstelefoner, statistik eller brugeradministration afhængigt af rolle
6. Åbn public/anonym oversigt fra login/public-link uden login
```

---

## Teststrategi

Projektet anvender en kombination af:

- service-tests
- controller-tests
- navigation-/auth-tests
- manuel runtime-test i MAUI-klienten

De centrale use cases UC01–UC15 er implementeret og testet efter projektets runtime-first Definition of Done. En use case betragtes først som lukket, når relevant kode bygger, automatiserede tests er grønne, og flowet er manuelt verificeret i den faktiske brugerflade.

Testene dækker blandt andet:

- login og rollebaseret navigation
- adgangskontrol og server-side afdelingsvalidering
- borgerstatus
- fast medicin og PN-medicin
- noter og opgaver
- historik/audit-events
- overlapvisning
- personaletildeling
- arbejdstelefoner
- brugeradministration og rollehierarki
- public anonym oversigt uden persondata
- statistik for leder/systemoverblik

---

## Dokumentation

Dokumentation og diagrammer ligger i projektets dokumentationsmapper, eksempelvis:

```text
Documentation/
Documentation/Diagrammer/
VIGOR/Documentation/
```

Vigtige dokumenter og artefakter omfatter blandt andet:

- projektrapporten
- systemudviklingsmetode
- use case-beskrivelser og implementation-noter
- ER-diagram
- deployment-diagram
- aktivitetsdiagrammer
- design class diagrammer
- systemsekvens-/sekvensdiagrammer
- API-kontrakter
- HTML/UI-prototyper og screenshots

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
