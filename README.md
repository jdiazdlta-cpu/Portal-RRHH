# Portal RRHH FZ

Webapp interna de Recursos Humanos para centralizar administracion de colaboradores, expedientes digitales, vencimientos, alertas y dashboard de RRHH.

Estado actual: backend V1 funcional y validado. El frontend ya tiene base funcional con login, JWT, rutas protegidas, layout principal y dashboard inicial consumiendo endpoints reales.

## Tecnologias

- Backend: ASP.NET Core Web API
- Frontend: React + Vite + TypeScript
- Base de datos: SQL Server
- ORM: Entity Framework Core
- Autenticacion: JWT Bearer con roles
- Archivos: almacenamiento local configurable en `uploads/`

## Estructura

```text
Portal RRHH/
  backend/
    PortalRRHHFZ.slnx
    src/
      PortalRRHHFZ.Api/
      PortalRRHHFZ.Application/
      PortalRRHHFZ.Domain/
      PortalRRHHFZ.Infrastructure/
  frontend/
    src/
      api/
      auth/
      components/
      hooks/
      layouts/
      pages/
      routes/
      types/
      utils/
  uploads/
  docs/
  database/
```

## Configurar SQL Server

La cadena de conexion se configura en:

```text
backend/src/PortalRRHHFZ.Api/appsettings.json
backend/src/PortalRRHHFZ.Api/appsettings.Development.json
```

Configuracion actual de desarrollo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=FMZRRHH009\\LOCALHOST;Database=PortalRRHHFZ;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;"
  }
}
```

La base `PortalRRHHFZ` fue creada manualmente en SQL Server. No cambies esta cadena si ya esta funcionando en tu ambiente.

## Migraciones

Desde `backend`:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src\PortalRRHHFZ.Infrastructure --startup-project src\PortalRRHHFZ.Api
```

La migracion inicial existente es `InitialCreate`. Para crear futuras migraciones:

```powershell
dotnet tool run dotnet-ef migrations add NombreMigracion --project src\PortalRRHHFZ.Infrastructure --startup-project src\PortalRRHHFZ.Api --output-dir Migrations
```

## Ejecutar backend

Desde la raiz del repositorio:

```powershell
cd backend
dotnet restore
dotnet run --project src\PortalRRHHFZ.Api\PortalRRHHFZ.Api.csproj
```

El puerto exacto lo muestra `dotnet run`. En ambiente `Development`, Swagger esta disponible en:

```text
http://localhost:{puerto}/swagger
```

Swagger tiene soporte para Bearer Token. Usa `POST /api/auth/login`, copia `data.token`, presiona `Authorize` y pega solo el token.

## Ejecutar frontend

Desde la raiz del repositorio:

```powershell
cd frontend
npm install
npm run dev
```

Configura la URL de la API copiando `frontend/.env.example` a `frontend/.env` si necesitas personalizarla:

```text
VITE_API_BASE_URL=http://localhost:5004/api
```

Para esta etapa se recomienda levantar el backend en el puerto `5004`:

```powershell
cd backend
dotnet run --project src\PortalRRHHFZ.Api\PortalRRHHFZ.Api.csproj --urls http://localhost:5004
```

Build del frontend:

```powershell
cd frontend
npm run build
```

Vite usa por defecto:

```text
http://localhost:5173
```

Rutas frontend disponibles:

```text
/login
/dashboard
/colaboradores
/alertas
/usuarios
/configuracion
/acceso-denegado
```

Permisos frontend V1:

```text
Admin: dashboard, colaboradores, alertas, usuarios, configuracion
RRHH: dashboard, colaboradores, alertas, configuracion
Supervisor y Consulta: roles reservados, sin opciones operativas principales
```

## Autenticacion

Configuracion JWT:

```json
{
  "Jwt": {
    "Issuer": "PortalRRHHFZ",
    "Audience": "PortalRRHHFZ.Frontend",
    "SecretKey": "PortalRRHHFZ-Development-SecretKey-Change-In-Production-2026",
    "ExpirationMinutes": 60
  }
}
```

Usuario Admin inicial de desarrollo:

```text
Email: admin@portalrrhh.local
Password: Admin123*
Rol: Admin
```

Este usuario se crea automaticamente en ambiente `Development` si no existe. La password se guarda como hash. En produccion se debe cambiar la password inicial y mover `Jwt:SecretKey` a un secreto seguro.

El frontend guarda el token JWT y el usuario autenticado en `localStorage` para desarrollo local. Si la API responde `401`, la sesion local se limpia y el usuario vuelve a `/login`.

Login:

```powershell
Invoke-RestMethod -Method POST http://localhost:{puerto}/api/auth/login `
  -ContentType "application/json" `
  -Body '{"email":"admin@portalrrhh.local","password":"Admin123*"}'
```

Endpoint autenticado:

```powershell
Invoke-RestMethod http://localhost:{puerto}/api/auth/me `
  -Headers @{ Authorization = "Bearer {token}" }
```

## Roles V1

- `Admin`: administra usuarios y accede a modulos operativos V1.
- `RRHH`: accede a modulos operativos V1 excepto administracion de usuarios.
- `Supervisor`: reservado para fases futuras, sin permisos operativos principales en V1.
- `Consulta`: reservado para fases futuras, sin permisos operativos principales en V1.

## Endpoints principales

Publicos o de diagnostico:

```text
GET  /api/health
GET  /api/db-test
POST /api/auth/login
GET  /api/auth/me
```

Usuarios, solo `Admin`:

```text
GET    /api/usuarios
GET    /api/usuarios/{id}
POST   /api/usuarios
PUT    /api/usuarios/{id}
PATCH  /api/usuarios/{id}/activar
PATCH  /api/usuarios/{id}/desactivar
PUT    /api/usuarios/{id}/reset-password
```

Catalogos, `Admin` y `RRHH`:

```text
GET /api/catalogos/roles
GET /api/catalogos/empresas
GET /api/catalogos/departamentos
GET /api/catalogos/cargos
GET /api/catalogos/tipos-contrato
GET /api/catalogos/estatus-colaborador
GET /api/catalogos/motivos-salida
GET /api/catalogos/tipos-documento
```

Empresas, departamentos y cargos, `Admin` y `RRHH`:

```text
GET/POST/PUT/PATCH /api/empresas
GET/POST/PUT/PATCH /api/departamentos
GET/POST/PUT/PATCH /api/cargos
```

Colaboradores, `Admin` y `RRHH`:

```text
GET    /api/colaboradores
GET    /api/colaboradores/{id}
GET    /api/colaboradores/{id}/perfil
POST   /api/colaboradores
PUT    /api/colaboradores/{id}
PATCH  /api/colaboradores/{id}/activar
PATCH  /api/colaboradores/{id}/desactivar
GET    /api/colaboradores/{id}/historial
```

Documentos, `Admin` y `RRHH`:

```text
GET    /api/colaboradores/{id}/documentos
POST   /api/colaboradores/{id}/documentos
GET    /api/documentos/{id}
GET    /api/documentos/{id}/descargar
PUT    /api/documentos/{id}
PATCH  /api/documentos/{id}/desactivar
```

Alertas, `Admin` y `RRHH`:

```text
GET   /api/alertas
GET   /api/alertas/resumen
PATCH /api/alertas/{id}/gestionar
PATCH /api/alertas/{id}/ignorar
POST  /api/alertas/recalcular
```

Dashboard, `Admin` y `RRHH`:

```text
GET /api/dashboard/resumen
GET /api/dashboard/vencimientos
GET /api/dashboard/colaboradores-por-estatus
GET /api/dashboard/colaboradores-por-departamento
GET /api/dashboard/altas-bajas
GET /api/dashboard/ultimos-movimientos
```

## Uploads

La ruta local de almacenamiento se configura en:

```json
{
  "FileStorage": {
    "RootPath": "uploads"
  }
}
```

Los documentos se guardan fisicamente en:

```text
uploads/colaboradores/{colaboradorId}/
```

En base de datos se guarda solo metadata y ruta relativa. La app no elimina archivos fisicos al desactivar documentos.

Extensiones permitidas:

```text
.pdf, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx
```

Tamano maximo en desarrollo: 10 MB.

## Pruebas rapidas

Backend:

```powershell
cd backend
dotnet build
```

Frontend:

```powershell
cd frontend
npm run build
```

Con la API levantada:

```text
GET  /api/health
GET  /api/db-test
POST /api/auth/login
GET  /api/auth/me
GET  /api/catalogos/tipos-documento
GET  /api/dashboard/resumen
```

Para endpoints protegidos agrega:

```text
Authorization: Bearer {token}
```
