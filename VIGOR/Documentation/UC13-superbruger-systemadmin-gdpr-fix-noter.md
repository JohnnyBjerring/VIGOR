# UC13 – Superbruger som systemadmin og GDPR-afgrænsning

## Formål

Denne rettelse præciserer rollemodellen, så Superbruger igen er øverste tekniske rolle, men uden automatisk adgang til borgerdata.

## Implementeret

- Superbruger er sat tilbage som højeste rolle/rank.
- Roller vises i brugeradministrationen med ranknummer og højeste rank øverst:
  - 40 - Superbruger (systemadmin)
  - 30 - Leder
  - 20 - Vagtansvarlig
  - 10 - Personale
- Superbruger kan administrere Leder og lavere roller.
- Leder kan ikke ændre/deaktivere Superbruger og kan ikke tildele Superbruger-rollen.
- Brugere kan fortsat ikke ændre egen rolle eller deaktivere sig selv.
- Pure Superbruger sendes til adminområdet ved login.
- Pure Superbruger fjernes fra borger-/vagt-/medicin-/note-/opgave-/overlap-endpoints.
- Navigationen skjuler vagtvalg, borgere og overlap for brugere uden driftsrolle.

## GDPR-princip

Superbruger forstås som teknisk systemadministrator/webmaster. Rollen må kunne administrere systemets brugere og roller, men bør af hensyn til dataminimering og need-to-know ikke automatisk kunne se borgerdata, medicin, noter, opgaver eller borgerhistorik.

Hvis en person både er teknisk administrator og fagligt/driftsmæssigt ansvarlig, bør vedkommende tildeles en relevant driftsrolle separat.

## Runtime-regel

Admin-flow:

AdminUsers.razor → UserAdminClientService → UserAdminApiController → UserAdminService → Identity tables

Drifts-/borgerflow:

Kræver Leder, Vagtansvarlig eller Personale. Superbruger alene er ikke nok.
