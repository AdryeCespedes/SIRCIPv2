# SIRCIP

Calcula las percepciones de Ingresos Brutos que corresponde facturarle a un cliente bajo
Convenio Multilateral, según el régimen SIRCIP de la Comisión Arbitral (COMARB).

Hoy ese cálculo se hace a mano: hay que buscar el CUIT en el padrón mensual que publica
COMARB, interpretar la situación del contribuyente en cada jurisdicción y aplicar las
reglas de alícuotas y sobretasas. Este sistema automatiza la importación de ese padrón y
el cálculo a partir de un CUIT, una fecha, un importe facturado y una provincia de entrega.

Los requerimientos completos están en [PRD.md](PRD.md); las convenciones de código, en
[AGENTS.md](AGENTS.md).

---

## Cómo funciona

### Los proyectos

| Proyecto | Qué es |
|---|---|
| `Sircip.Server` | Web API. Autenticación con JWT, importación del padrón y consultas. |
| `Sircip.Client` | Blazor Server. Las pantallas. Habla con la API por HTTP. |
| `Sircip.Shared` | Contratos y tipos que comparten los dos. |
| `Sircip.Test` | Tests automáticos de la API y del procesamiento del padrón. |

El cliente y el servidor son dos aplicaciones separadas: hay que levantar las dos.

### El padrón

El padrón de COMARB es un `.txt` que puede tener millones de registros, y hay que
consultarlo por CUIT en cada cálculo. En vez de cargarlo en una tabla de base de datos, al
importarlo se convierte en un **archivo binario propio**: registros de ancho fijo ordenados
por CUIT, uno por período.

Buscar es entonces una búsqueda binaria sobre el archivo mapeado en memoria, sin cargarlo
entero. Con un millón de registros, la importación completa tarda alrededor de 1,3 segundos
y cada búsqueda por CUIT menos de 2 microsegundos.

La base de datos solo guarda los usuarios y la **constancia** de cada importación: período,
fecha, quién la hizo y cuántos registros entraron.

### Reglas que conviene conocer antes de usarlo

- **La importación es todo o nada.** Cada línea se valida contra el diseño de registro; si
  una sola no cumple, se rechaza el archivo completo y no queda nada a medias.
- **Un período tiene una sola importación vigente.** Para volver a importarlo hay que
  eliminarlo primero.
- **Eliminar es un borrado lógico.** Se borra el archivo del período, pero la constancia
  queda en el historial marcada como borrada.
- **Los intentos fallidos también quedan registrados**, con el motivo del rechazo.
- **No hay registración de usuarios.** Se dan de alta a mano, con el comando de más abajo.
- **Hay dos roles fijos**: Administrador, que importa y elimina padrones, y Usuario, que
  solo puede pedir cálculos.

---

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (sirve cualquier edición local, incluida Express o Developer)
- La herramienta de EF Core, para crear la base:

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

---

## Puesta en marcha

### 1. Clonar y restaurar

```bash
git clone https://github.com/AdryeCespedes/SIRCIPv2.git
cd SIRCIPv2
dotnet restore
```

### 2. Configurar los secretos

La cadena de conexión y la clave para firmar los tokens **no están en el repositorio**. Se
cargan con user secrets, que quedan fuera del proyecto:

```bash
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost;Database=Sircipv2;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;" \
  --project Sircip.Server

dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project Sircip.Server
```

La clave del JWT tiene que ser de **al menos 32 caracteres**: se firma con HMAC-SHA256 y
con menos que eso el servidor no arranca. En PowerShell, donde no hay `openssl`, sirve:

```powershell
$clave = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
dotnet user-secrets set "Jwt:Key" $clave --project Sircip.Server
```

Si preferís autenticación integrada de Windows en vez de usuario y contraseña, reemplazá el
`User Id` y el `Password` por `Trusted_Connection=True`.

### 3. Crear la base de datos

```bash
dotnet ef database update --project Sircip.Server
```

Crea la base y sus dos tablas, `Usuarios` e `Importaciones`. Las migraciones no se aplican
solas al arrancar: este comando hay que correrlo también después de traer cambios que
agreguen migraciones nuevas.

### 4. Crear el primer usuario

No hay pantalla de registración. El alta es por consola:

```bash
dotnet run --project Sircip.Server -- --seed-admin
```

Te pide nombre de usuario, rol (por defecto Administrador) y contraseña. La contraseña no
se ve mientras la escribís y se guarda con hash bcrypt. Terminado el alta, el proceso sale
sin levantar el servidor.

### 5. Levantar las dos aplicaciones

En dos terminales distintas:

```bash
dotnet run --project Sircip.Server    # API      → http://localhost:5025
dotnet run --project Sircip.Client    # pantallas → http://localhost:5057
```

Entrá a **http://localhost:5057** e iniciá sesión con el usuario que creaste.

Si cambiás el puerto de la API, ajustá `Api:BaseUrl` en `Sircip.Client/appsettings.json`
para que apunte al mismo lugar.

---

## Primer uso: importar un padrón

El archivo del padrón **no se sube desde el navegador**: el Administrador lo deja en el
disco del servidor y el sistema lo lee de ahí. Por seguridad solo se aceptan archivos que
estén dentro de un directorio configurado, y se rechaza cualquier ruta que quede afuera.

Ese directorio se configura en `Sircip.Server/appsettings.json` y por defecto es
`Datos/Importacion`, relativo al directorio del servidor. Se crea solo al arrancar.

```json
"Padron": {
  "DirectorioImportacion": "Datos/Importacion",
  "DirectorioDatos": "Datos/Padrones"
}
```

En `DirectorioDatos` es donde el sistema deja los archivos binarios que genera.

El repositorio incluye un padrón de ejemplo de 942 registros, con los CUIT anonimizados,
para probar sin necesidad de bajar uno real:

```bash
cp Sircip.Test/Datos/padron-ejemplo-202602.txt Sircip.Server/Datos/Importacion/
```

Después, en la pantalla **Importar padrón**: año `2026`, mes `02 · febrero`, archivo
`padron-ejemplo-202602.txt`.

Para un padrón real, el Administrador lo descarga del menú "Descargas" del sistema SIRCIP
dentro del Portal Federal Tributario y lo copia a esa misma carpeta.

---

## La API

Está documentada con Swagger en **http://localhost:5025/swagger** cuando el servidor corre
en Development. Para probar un endpoint protegido: llamá a `POST /api/auth/login`, copiá el
`token` de la respuesta y pegálo en el botón **Authorize** (solo el token, sin escribir
`Bearer`).

| Método y ruta | Rol | Qué hace |
|---|---|---|
| `POST /api/auth/login` | — | Devuelve el token de sesión |
| `GET /api/auth/me` | autenticado | Usuario y rol del token |
| `POST /api/padron/importaciones` | Administrador | Importa el padrón de un período |
| `GET /api/padron/importaciones` | Administrador | Historial completo |
| `GET /api/padron/importaciones/{id}` | Administrador | Una constancia |
| `GET /api/padron/importaciones/{año}/{mes}` | Administrador | El padrón vigente de un período |
| `DELETE /api/padron/importaciones/{año}/{mes}` | Administrador | Borrado lógico del padrón |

---

## Tests

```bash
dotnet test Sircip.Test
```

Cubren la autenticación, la validación del registro del padrón, el formato binario, la
importación completa por la API y el historial. Dos de ellos miden el rendimiento con un
millón de registros y verifican que la importación entre en el minuto que exige el PRD.

---

## Estructura del repositorio

```
Sircip.Server/
  Auth/                 autenticación JWT y alta de usuarios
  Controllers/          endpoints de la API
  Data/                 DbContext
  Migrations/           migraciones de EF Core
  Models/               entidades de base de datos
  Padron/
    Models/             registro del padrón, formato binario, tablas de referencia
    Exceptions/
    Services/           parser, lector y escritor del binario, importación, eliminación
Sircip.Client/
  Components/Pages/     las pantallas
  Services/             clientes HTTP de la API
Sircip.Shared/
  Contracts/            objetos que viajan por la API
  Models/               enums compartidos
  Validations/          validación de CUIT
Sircip.Test/
  Datos/                padrón de ejemplo
```

---

## Estado

Funcionando: autenticación con roles, importación del padrón con validación completa,
historial de importaciones y borrado lógico, todo con sus pantallas.

Pendiente: el cálculo de percepciones, que es el objetivo final del sistema. Las reglas
están especificadas en los anexos B y C del [PRD](PRD.md).
