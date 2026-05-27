# UC12 – Tildel telefon til personale

## Status
UC12 er implementeret som en praktisk COULD-funktion med smalt scope.

## Implementeret funktionalitet
- Oprettet `WorkPhone` entity/model.
- Oprettet `PhoneAssignment` entity/model.
- Oprettet DTO'er:
  - `WorkPhoneDto`
  - `PhoneAssignmentDto`
  - `CreateWorkPhoneRequest`
  - `AssignWorkPhoneRequest`
  - `PhoneAssignableEmployeeDto`
- Oprettet `IWorkPhoneService` / `WorkPhoneService`.
- Oprettet `IWorkPhoneApi` / `WorkPhoneClientService`.
- Oprettet `WorkPhonesApiController`.
- Tilføjet `WorkPhones` og `PhoneAssignments` til `AppDbContext`.
- Tilføjet migration `20260525030000_AddWorkPhone`.
- Registreret service og client service i DI.
- Udvidet brugeradministration med arbejdstelefoner:
  - opret telefon
  - tildel telefon til medarbejder
  - fjern aktiv telefontildeling
  - vis telefon på bruger/personale
- Udvidet borger/personalevisning med aktiv telefon på tildelt personale.
- Udvidet overlapvisningen, så tildelt personale vises med aktiv arbejdstelefon.
- Tilføjet service-tests for arbejdstelefoner.
- Tilføjet controller-tests for arbejdstelefoner.

## Runtime-path
`AdminUsers.razor → WorkPhoneClientService → WorkPhonesApiController → WorkPhoneService → ApplicationDbContext → WorkPhones + PhoneAssignments`

Telefonoplysninger vises desuden i overlap-flowet:

`Overlap.razor → OverlapClientService → OverlapApiController → OverlapService → ApplicationDbContext → CitizenStaffAssignments + PhoneAssignments`

## Regler
- En arbejdstelefon kan kun have én aktiv medarbejdertildeling ad gangen.
- En medarbejder kan kun have én aktiv arbejdstelefon ad gangen.
- Ny tildeling deaktiverer automatisk tidligere aktiv tildeling for samme telefon eller samme medarbejder.
- Fjernelse af telefon sletter ikke historikken, men markerer tildelingen inaktiv.
- Telefontildeling knyttes til medarbejder og afdeling, ikke direkte til borger.

## Audit
Der er ikke tilføjet borgerbaseret audit-event til UC12, fordi den eksisterende `AuditEvent`-model er knyttet til borgerhistorik via `CitizenId`. Telefontildeling er en system-/driftsadministrativ handling og bør på sigt logges i en separat system-audit, hvis det ønskes.

## Manuel test
- Log ind som Leder/Superbruger.
- Åbn brugeradministration.
- Opret en arbejdstelefon.
- Tildel telefonen til en medarbejder.
- Bekræft at telefonen vises i brugerlisten.
- Bekræft at telefonen vises på tildelt personale på borger.
- Bekræft at telefonen vises i overlapvisningen ved tildelt personale.
- Fjern telefonen fra medarbejderen.
- Bekræft at aktiv tildeling forsvinder.
- Kør unit/controller-tests.
