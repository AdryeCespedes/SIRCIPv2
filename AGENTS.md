# AGENTS.md

## Propósito
SIRCIP calcula las percepciones de Ingresos Brutos bajo Convenio Multilateral a partir del padrón mensual de COMARB. Automatiza la importación del padrón y el cálculo por CUIT, fecha, importe y provincia de entrega.

## Stack
- .NET 8 (LTS)
- Blazor Server (`Sircip.Client`) + Web API (`Sircip.Server`)
- SQL Server (instancia local de desarrollo) — usuarios y autenticación
- Hash de contraseñas: BCrypt.Net-Next
- Padrón: archivo binario propio con registros de ancho fijo ordenados por CUIT, acceso vía `MemoryMappedFile` + búsqueda binaria (sin motor externo)

## Cómo correr
```
dotnet restore
# Configurar connection string a SQL Server local (appsettings.Development.json o user-secrets)
# Ejecutar el seed de usuario inicial (no hay auto-registro)
dotnet run --project Sircip.Server
dotnet run --project Sircip.Client
dotnet test Sircip.Test
```

## Qué NO hacer
- No permitir reimportar o modificar parcialmente el padrón de un período ya importado: para volver a importarlo primero hay que eliminarlo (borrado lógico) y luego importarlo de nuevo.
- No agregar pantalla ni lógica de auto-registro de usuarios: los usuarios se dan de alta manualmente en la base de datos.
- No implementar RBAC configurable ni roles/permisos intermedios: solo existen Administrador y Usuario, con permisos fijos.
- No almacenar contraseñas en texto plano.
