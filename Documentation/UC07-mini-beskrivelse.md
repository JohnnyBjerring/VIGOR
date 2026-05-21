# UC07 – Vælg vagt

## Smalt scope

UC07 etablerer brugerens aktive vagtkontekst for den aktuelle arbejdssession.
Efter login vælger brugeren én af de tre vagttyper:

- Dagvagt
- Aftenvagt
- Nattevagt

Valget bruges som runtime-kontekst, så senere overlap-, note- og opgaveflows kan kobles til den valgte vagt.

## Afgrænsning

UC07 opretter ikke en fuld vagtplan eller databasebaseret overlap-session. Der oprettes heller ikke historik/audit-log for selve vagtvalget i denne iteration.

Det bevidste designvalg er at holde UC07 smal og KISS-orienteret:

- Vagtvalg valideres på serveren.
- Bruger og afdeling udledes fra auth-context på serveren.
- Klienten gemmer den aktive vagtkontekst i scoped session/runtime-state.
- Borgeroversigten kræver en aktiv vagtkontekst og viser den valgte vagt.

## Runtime-path

```text
Login
  ↓
StartPageResolver sender Personale og Vagtansvarlig til /shift/select
  ↓
Bruger vælger Dagvagt, Aftenvagt eller Nattevagt
  ↓
Klienten kalder POST /api/shifts/select
  ↓
Serveren validerer vagttype, bruger og afdeling
  ↓
Serveren returnerer ActiveShiftContextDto
  ↓
Klienten gemmer ActiveShiftContextDto i ActiveShiftContextState
  ↓
Brugeren sendes til /citizens
  ↓
Borgeroversigten viser aktiv vagt
```

## UX-forbedring

Vagtvalgssiden foreslår en vagt ud fra lokalt klokkeslæt:

- 07:00–14:59 → Dagvagt
- 15:00–22:59 → Aftenvagt
- 23:00–06:59 → Nattevagt

Forslaget er kun en hjælp. Brugeren skal stadig vælge manuelt, fordi faktiske vagtskifter kan ske før eller efter de planlagte tidspunkter.

## Fremtidig udvidelse

Når UC08, UC09 og UC10 implementeres, kan ActiveShiftContext udvides eller erstattes af en databasebaseret ShiftSession/Overlap-entitet, eksempelvis med:

- ShiftSessionId
- DepartmentId
- ShiftType
- StartedAtUtc
- StartedByUserId
- ClosedAtUtc

Det er bevidst holdt ude af UC07 for at undgå, at vagtvalg udvikler sig til en fuld vagtplan-/overlapmotor for tidligt.

## Test

UC07 dækkes af:

- Unit tests for ShiftTypeExtensions
- Unit tests for ActiveShiftContextState
- Unit tests for ShiftSelectionService
- Controller tests for ShiftsApiController
- Manuel runtime-test via login → vagtvalg → borgeroversigt
