# AGENTS.md

## Propósito
SIRCIP calcula las percepciones de Ingresos Brutos bajo Convenio Multilateral a partir del padrón mensual de COMARB. Automatiza la importación del padrón y el cálculo por CUIT, fecha, importe y provincia de entrega.

## Stack
- .NET 8 (LTS)
- Blazor Server (`Sircip.Client`) + Web API (`Sircip.Server`)
- SQL Server (instancia local de desarrollo) — usuarios y autenticación
- Hash de contraseñas: BCrypt.Net-Next
- Padrón: archivo binario propio con registros de ancho fijo ordenados por CUIT, acceso vía `MemoryMappedFile` + búsqueda binaria (sin motor externo)
- Grillas de datos en el cliente: MudBlazor

## Convenciones de código
- Inyección de dependencias con constructor explícito (campos `readonly` asignados en el cuerpo del constructor), no con primary constructors de C# 12 (`class Foo(IBar bar)`).
- Los nombres de carpeta van en inglés (`Models`, `Exceptions`, `Services`, `Contracts`, `Validations`), salvo las que nombran un concepto del dominio que no se traduce (`Padron`). El código en sí —clases, métodos, variables, comentarios y nombres de tests— va en español.
- El namespace tiene que coincidir con la ruta de la carpeta: `Sircip.Server/Padron/Services/` → `namespace Sircip.Server.Padron.Services`.
- Dentro de cada área funcional, separar por tipo de archivo en `Models/`, `Exceptions/` y `Services/`.
- La solución tiene que compilar sin ningún warning. Los warnings se arreglan, nunca se silencian: no usar `#pragma warning disable`, `<NoWarn>` ni `SuppressMessage`. Verificar con `dotnet build Sircip.sln` antes de dar por terminado un cambio.
- Los listados de datos se muestran con grillas de MudBlazor (`MudDataGrid`), no con tablas HTML armadas a mano. Cada columna tiene que permitir ordenar y filtrar.

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
