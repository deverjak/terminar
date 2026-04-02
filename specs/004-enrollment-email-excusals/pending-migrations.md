# Pending EF Core Migrations

The following migration must be run after the Excusal and ExcusalCredit entities have been added to RegistrationsDbContext:

```bash
dotnet ef migrations add AddExcusalAndCredit \
  --project src/Terminar.Modules.Registrations \
  --startup-project src/Terminar.Api \
  --context RegistrationsDbContext
```

This migration covers:
- `Excusal` aggregate table with unique index on (RegistrationId, SessionId)
- `ExcusalCredit` aggregate table with text[] and uuid[] array columns
- `ExcusalCreditAuditEntries` owned table
