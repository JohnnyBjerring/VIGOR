# UC13 – Rollehierarki og egen rollebeskyttelse

## Formål
Denne rettelse lukker et sikkerhedshul i UC13 brugeradministration.

En bruger må ikke kunne:

- ændre sin egen rolle
- forfremme sig selv
- degradere sig selv
- ændre/deaktivere en bruger med højere rolle end sin egen
- tildele en rolle, der ligger højere end den rolle brugeren selv har

## Rollehierarki
Rollehierarkiet er afgrænset simpelt:

1. Personale
2. Vagtansvarlig
3. Leder
4. Superbruger

En bruger kan kun administrere brugere og roller på eget eller lavere niveau. Egen bruger er altid beskyttet mod rolleændring og aktivering/deaktivering.

## Server-side regler
Serveren håndhæver nu reglerne i `UserAdminService`, så UI ikke er eneste beskyttelse.

### Opret bruger
Ved oprettelse må den valgte rolle ikke være højere end den aktuelle administrators egen rolle.

### Ret rolle
Serveren afviser:

- rolleændring på egen bruger
- ændring af en bruger med højere rolle end den aktuelle administrator
- tildeling af en rolle over administratorens egen rolle

### Aktivér/deaktivér bruger
Serveren afviser:

- aktivering/deaktivering af egen bruger
- aktivering/deaktivering af en bruger med højere rolle end den aktuelle administrator

## UI-regler
Admin UI skjuler/deaktiverer handlinger, som brugeren ikke må udføre:

- Egen rolle-dropdown er deaktiveret.
- Brugere med højere rolle kan ikke ændres fra UI.
- Rollelisten ved oprettelse/ændring viser kun roller på eget eller lavere niveau.
- Aktiv/deaktiv-knappen deaktiveres for egen bruger og højere roller.

## Runtime-path

AdminUsers.razor
→ UserAdminClientService
→ UserAdminApiController
→ UserAdminService
→ ASP.NET Identity-tabeller

## Testfokus

- Leder kan ikke oprette Superbruger.
- Leder kan ikke ændre en Superbruger.
- Bruger kan ikke ændre egen rolle.
- Bruger kan ikke deaktivere egen bruger.
- Superbruger kan fortsat administrere lavere roller.
