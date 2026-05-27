# UC14 – Public anonym oversigtsskærm

## Status
UC14 er implementeret som en smal public/kiosk-visning med anonymiseret statusoverblik.

## Runtime-path

```text
PublicOverview.razor
→ PublicOverviewClientService
→ PublicOverviewApiController
→ PublicOverviewService
→ ApplicationDbContext
→ PublicOverviewDto / PublicCitizenStatusDto
```

## Implementeret funktionalitet

- `PublicOverviewDto`
- `PublicCitizenStatusDto`
- `IPublicOverviewApi`
- `PublicOverviewClientService`
- `IPublicOverviewService`
- `PublicOverviewService`
- `PublicOverviewApiController`
- Public/kiosk UI på `/public-overview`
- Link fra login-siden til anonym oversigtsskærm
- DI-registrering i MAUI, Web.Client og Web API
- Service-tests for anonymiseret output
- Controller-tests for anonymt endpoint uden login

## GDPR-afgrænsning

Public view er ikke den interne overlapvisning uden login. Public view bruger egne DTO'er og returnerer kun anonym statusdata.

Endpointet returnerer ikke:

- borgernavne
- citizenId
- departmentId
- afdelingsnavne
- medicinnavne
- PN-medicin
- noter
- opgavetekster
- brugerreferencer
- auditbeskrivelser

## Endpoint

```text
GET /api/public/overview
```

Endpointet er markeret med `AllowAnonymous`, men returnerer kun anonymiserede data.

## DTO-output

Public overview viser:

- samlet antal borgere
- antal røde/gule/grønne statusser
- anonym statusliste med labels som `Borger 1`, `Borger 2`, osv.
- status og opmærksomhedsniveau
- genereringstidspunkt

## Tests

Der er tilføjet tests, der kontrollerer:

- at endpointet kan kaldes uden autentificeret bruger
- at statuscounts returneres korrekt
- at output ikke indeholder borgernavne, afdelingsnavne, medicinnavne, noter, opgaver eller brugerreferencer

## Manuel test

- Åbn `/public-overview` uden login.
- Bekræft at siden loader.
- Bekræft at der kun vises anonymiserede labels som `Borger 1`.
- Bekræft at der ikke vises navne, medicin, noter, opgaver eller brugeroplysninger.
- Bekræft at statuscounts og anonym statusliste vises korrekt.
