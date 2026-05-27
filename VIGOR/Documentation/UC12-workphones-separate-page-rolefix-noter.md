# UC12 – Arbejdstelefoner separat side + rollejustering

## Formål
UC12 er justeret, så telefontildeling ikke længere ligger inde i den fulde brugeradministration. Det passer bedre til casen, hvor arbejdstelefoner er en praktisk driftsopgave ved vagten.

## Ændring
Der er oprettet en separat side:

```text
/work-phones
```

## Roller

```text
Leder:
- Kan oprette arbejdstelefoner
- Kan tildele/fjerne telefoner fra medarbejdere
- Kan se aktive telefontildelinger

Vagtansvarlig:
- Kan tildele/fjerne telefoner fra medarbejdere
- Kan se aktive telefontildelinger
- Kan ikke oprette selve telefonnumrene

Superbruger:
- Kan oprette selve arbejdstelefonerne som systemopsætning
- Kan ikke fordele telefoner til medarbejdere uden driftsrolle

Personale:
- Har ikke adgang til administrationssiden for arbejdstelefoner
- Kan fortsat se relevante telefonoplysninger via borger-/overlapvisninger, hvor det er relevant
```

## Runtime-path

```text
WorkPhones.razor
→ WorkPhoneClientService
→ WorkPhonesApiController
→ WorkPhoneService
→ ApplicationDbContext
→ WorkPhones + PhoneAssignments
```

## Sikkerheds-/GDPR-note
Telefontildeling er flyttet ud af brugeradministration, så Vagtansvarlig kan udføre den praktiske vagtopgave uden adgang til fuld bruger- og rolleadministration.

Superbruger behandles fortsat som teknisk/systemmæssig administrator. Derfor kan Superbruger oprette telefonnumre, men ikke fordele telefoner til navngivne medarbejdere uden en egentlig driftsrolle.

## Manuel test

```text
[ ] Log ind som Vagtansvarlig
[ ] Bekræft at menuen viser Arbejdstelefoner
[ ] Bekræft at Vagtansvarlig kan tildele telefon til medarbejder
[ ] Bekræft at Vagtansvarlig kan fjerne telefontildeling
[ ] Bekræft at Vagtansvarlig ikke kan oprette ny arbejdstelefon
[ ] Log ind som Leder
[ ] Bekræft at Leder kan oprette arbejdstelefon
[ ] Bekræft at Leder kan tildele/fjerne telefon
[ ] Log ind som Superbruger uden driftsrolle
[ ] Bekræft at Superbruger kan oprette arbejdstelefon
[ ] Bekræft at Superbruger ikke kan tildele/fjerne telefon til medarbejder
[ ] Bekræft at Brugeradministration ikke længere indeholder telefonstyringssektionen
[ ] Kør unit tests
```
