# Base de datos

Base esperada: `PortalRRHHFZ`.

Cadena configurada para desarrollo:

```text
Server=localhost;Database=PortalRRHHFZ;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;
```

Si SSMS usa otra instancia, actualiza `backend/src/PortalRRHHFZ.Api/appsettings.Development.json` y el `DesignTimeDbContextFactory`.
