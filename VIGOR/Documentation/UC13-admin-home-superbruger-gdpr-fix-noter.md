# UC13 fix – Superbruger adminstartside og GDPR-afgrænsning

## Problem
En ren Superbruger blev sendt til administrationssiden, men siden viste stadig tekst og knapper som om brugeren var Leder. Det betød, at en ren Superbruger kunne se knapper til "Vælg vagt" og "Borgere" på startsiden, selvom rollen efter GDPR-afgrænsningen ikke bør have automatisk adgang til borgerdata.

## Rettelse
`VIGOR.MAUI/Components/Pages/Admin.razor` er gjort rollebevidst.

## Ny adfærd
- Ren Superbruger ser kun link til bruger- og rolleadministration.
- Ren Superbruger ser ikke knapper til vagtvalg eller borgere.
- Startsiden forklarer, at Superbruger er systemadmin og ikke automatisk har adgang til borgerdata.
- Leder kan stadig se administration samt driftshandlinger som vagtvalg og borgere.
- Brugere med både Superbruger og en driftsrolle kan stadig få adgang til relevante driftslinks via driftsrollen.

## Test
- Log ind som ren Superbruger.
- Bekræft at siden viser "Superbruger (systemadmin)".
- Bekræft at kun "Administrér brugere og roller" vises.
- Bekræft at "Vælg vagt" og "Borgere" ikke vises på adminsiden.
- Log ind som Leder.
- Bekræft at Leder stadig kan se relevante driftlinks.
