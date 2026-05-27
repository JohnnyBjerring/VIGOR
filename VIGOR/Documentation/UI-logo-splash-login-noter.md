# UI-opdatering: VIGOR logo, splashscreen og login

## Formål
Denne ændring indfører VIGORs nye visuelle identitet i appens opstart og login-flow.

## Ændringer
- Tilføjet `VigorLogo.svg` som branding-asset.
- Tilføjet `VigorSplashScreen.png` som splashscreen-asset.
- Opdateret MAUI `index.html`, så appen viser VIGOR splashscreen ved opstart.
- Splashscreen vises minimum ca. 1 sekund, så brugeren når at se startskærmen.
- Opdateret login-siden, så den matcher den nye professionelle VIGOR-stil.
- Login bruger glas-/kortlayout, navy farver, logo, splashscreen-baggrund og tydelig adgang til anonym public view.

## Berørte filer
- `VIGOR.MAUI/wwwroot/index.html`
- `VIGOR.MAUI/wwwroot/branding/VigorLogo.svg`
- `VIGOR.MAUI/wwwroot/branding/VigorSplashScreen.png`
- `VIGOR.MAUI/Components/Pages/Login.razor`
- `VIGOR.MAUI/Components/Pages/Login.razor.css`

## Testforslag
- Start MAUI-appen og bekræft, at splashscreen vises ved opstart.
- Bekræft at splashscreen forsvinder automatisk efter ca. 1 sekund.
- Bekræft at login-siden vises med nyt logo og ny styling.
- Test login med gyldig bruger.
- Test login med ugyldig bruger.
- Test link til anonym oversigtsskærm fra login.
