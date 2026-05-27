# UC12 buildfix – WorkPhones / telefon-tildeling

## Rettet

- Rettet `CS0165 Use of unassigned local variable 'phoneAssignment'` i `OverlapService`.
- Rettet `CS0165 Use of unassigned local variable 'phoneAssignment'` i `StaffAssignmentService`.
- Inkluderet `WorkPhonesApiController.cs` igen i patchen, så controller-testen kan finde typen efter overwrite.

## Årsag

`phoneAssignment` blev deklareret inde i et null-conditional `TryGetValue`-kald. C# kunne derfor ikke garantere, at variablen altid var initialiseret, hvis dictionary-parameteren var null.

## Løsning

`phoneAssignment` deklareres nu eksplicit som nullable før opslaget:

```csharp
PhoneAssignment? phoneAssignment = null;
if (activePhoneAssignments != null)
{
    activePhoneAssignments.TryGetValue(assignment.EmployeeId, out phoneAssignment);
}
```

## Test

Kør efter overwrite:

```bash
dotnet clean
dotnet build
dotnet test
```
