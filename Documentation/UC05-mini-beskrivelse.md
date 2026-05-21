# UC05 – Registrér PN-medicin

## Formål
UC05 giver personalet mulighed for at registrere, at PN-medicin er givet til en borger ved behov.

## Smalt scope
Use casen handler kun om den konkrete registrering af PN-medicin givet ved behov. Den bygger ikke et fuldt medicinkatalog, FMK-integration, beholdningsstyring eller avanceret effekt-/opfølgningsflow.

## Runtime-path
UI → PN-medicin client service → API endpoint → PN-medicin server service → database

## Data der gemmes
- Borgerreference
- Afdeling, udledt fra den autentificerede medarbejder
- Aktiv vagttype
- Medicinnavn
- Dosis
- Årsag/behov
- Tidspunkt for medicingivning
- Brugerreference
- Oprettelsestidspunkt

## Afgrænsning
UC06 håndterer den egentlige audit-/historikudbygning. UC05 gemmer dog brugerreference, tidspunkt, afdeling og vagttype, så registreringen senere kan indgå i historik og audit-log.
