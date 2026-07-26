# PRD-001: SIRCIP — Gestionador del nuevo régimen SIRCIP de percepciones de Ingresos Brutos bajo Convenio Multilateral

## Contexto y Problema
Se precisa desarrollar una nueva aplicación que permita manejar el cálculo de percepciones de Ingresos Brutos, en clientes bajo Convenio Multilateral, según el nuevo régimen SIRCIP (Sistema Informático de Recaudación, Control e Información de Percepciones), que es un sistema desarrollado por la Comisión Arbitral (COMARB) para unificar, centralizar y estandarizar el régimen de percepciones de Ingresos Brutos bajo Convenio Multilateral.
El sistema publica un padrón por contribuyente en forma mensual. Actualmente para realizar el cálculo hay que consultar manualmente dicho padrón por CUIT en la página correspondiente y según la respuesta, evaluarla según unas determinadas reglas para disponer finalmente en qué provincias, qué alícuota hay que aplicar y si hay sobretasas, para finalmente calcular el importe de las percepciones de ingresos brutos a facturar al cliente.
Se precisa automatizar tanto la importación de dicho padrón, que puede ser muy grande con millones de registros, y almacenarlo de forma que pueda ser consultado rápidamente, como la obtención a partir de un CUIT, una fecha, un importe facturado y un código de provincia de entrega, la lista de provincias, alícuotas, tipo e importes de percepciones de ingresos brutos a aplicar.
El archivo tiene un formato .TXT y una estructura informada en un documento de diseño de registro disponible.
También hay documentos que explican en función del contenido del padrón cómo se calculan dichos importes de percepciones.

Personas:
Administrador del padrón: Es la persona encargada de descargar el padrón del Portal Federal Tributario cuando está disponible e importarlo en el nuevo sistema. También puede solicitar el cálculo de percepciones de ingresos brutos.
Usuario facturador: Es la persona que está facturando a un cliente y que tiene que calcular los importes de percepciones de ingresos brutos para dicho cliente.

## Objetivos
Calcular las percepciones de ingresos brutos a un cliente en convenio multilateral, según la provincia de entrega del comprobante a facturar, con la exactitud definida en RNF-04 y dentro del tiempo de respuesta definido en RNF-05.

## Requerimientos Funcionales
- RF-01: El sistema debe permitir que un usuario se autentique con nombre de usuario y contraseña.
- RF-02: El sistema debe restringir el acceso a sus funciones según dos roles de usuario fijos, Administrador y Usuario, denegando a los usuarios con rol Usuario el acceso a las funciones reservadas al rol Administrador.
- RF-03: El sistema debe permitir que un usuario con rol de Administrador importe el padrón de un período a partir de un archivo con formato .txt ubicado en el disco del servidor, indicando la ruta del archivo y el mes y año del período.
- RF-04: El sistema debe registrar cada importación del padrón, dejando constancia de la fecha de importación, el período, el usuario y la cantidad de registros importados.
- RF-05: El sistema debe calcular, a partir de un CUIT, una fecha (cuyo año y mes determinan el período de padrón a utilizar), un importe facturado (neto gravado, sin IVA) y una provincia de entrega, aplicando las reglas definidas sobre la información del padrón, las provincias, alícuotas, tipo e importes de percepciones de ingresos brutos a facturar al cliente (ver Anexo B).
- RF-06: El sistema debe devolver una lista vacía de percepciones de ingresos brutos para un CUIT que no esté en el padrón del período indicado, cuando la jurisdicción de entrega no esté adherida a SIRCIP (ver Anexo C).
- RF-07: Si el período derivado de la fecha indicada para el cálculo de las percepciones (ver RF-05) no está importado, el sistema debe devolver un error de padrón inexistente para el cálculo.
- RF-08: Si la importación falla al leer o procesar el archivo del padrón, el sistema debe registrar el error, el usuario importador, el período y la fecha de importación.
- RF-09: El sistema debe permitir eliminar el padrón importado de un período mediante un borrado lógico: la constancia de la importación no se elimina físicamente ni desaparece del historial, sino que se marca con un estado de borrado, y el período pasa a considerarse no importado a los efectos del cálculo de percepciones (ver RF-07) y de una nueva importación (ver RF-03).
- RF-10: El sistema debe tener una página, accesible solo para usuarios con rol Administrador, que permita consultar las importaciones realizadas exitosas y con errores.
- RF-11: El sistema debe validar cada línea del archivo de padrón contra el formato de campos definido (ver Anexo A) durante la importación.
- RF-12: Si al menos una línea del archivo de padrón no cumple el formato de campos definido (ver Anexo A), el sistema debe rechazar la importación completa del archivo, sin persistir ningún registro del período.
- RF-13: El sistema debe calcular una percepción por no inscripto, con alícuota fija del 2% sobre el neto gravado, cuando el CUIT no esté en el padrón del período indicado y la jurisdicción de entrega esté adherida a SIRCIP (ver Anexo C).
- RF-14: El sistema debe rechazar la importación cuando la ruta del archivo indicada (ver RF-03) quede fuera del directorio de importación configurado, sin leer dicho archivo.

## Requerimientos No Funcionales
- RNF-01: La importación del padrón debe realizarse en tiempos menores al minuto para un padrón de un millón de registros.
- RNF-02: El 100% de las contraseñas almacenadas deben usar hash seguro (bcrypt/argon2); ninguna debe persistir en texto plano.
- RNF-03: La sesión debe expirar tras 24 h de inactividad.
- RNF-04: El cálculo de las percepciones debe coincidir con el valor esperado en el 100% de los casos de test definidos.
- RNF-05: El cálculo de las percepciones para una consulta individual no puede demorar más de 2 segundos (p99).

## Criterios de Aceptación
- AC-01 (RF-01): Dado un usuario no autenticado, cuando intenta importar un padrón, entonces el sistema responde HTTP 401.
- AC-02 (RF-01): Dado un usuario no autenticado, cuando intenta pedir el cálculo de percepciones, entonces el sistema responde HTTP 401.
- AC-03 (RF-01): Dado un usuario con credenciales válidas dadas de alta en la base de datos, cuando envía su nombre de usuario y contraseña correctos, entonces el sistema responde HTTP 200, le provee un token de sesión y le permite iniciar sesión.
- AC-04 (RF-02): Dado un usuario con rol Usuario autenticado, cuando intenta acceder a cualquiera de las funciones reservadas al rol Administrador (importar el padrón, eliminar el padrón de un período, o consultar la página de importaciones — verificadas en concreto en AC-05, AC-14 y AC-17), entonces el sistema la deniega con HTTP 403 en todos los casos, dado que solo existen los dos roles fijos y no hay permisos configurables intermedios.
- AC-05 (RF-03): Dado un usuario con rol usuario, cuando intenta importar un archivo de padrón, entonces el sistema responde HTTP 403.
- AC-06 (RF-03): Dado un usuario con rol Administrador autenticado, cuando importa un archivo de padrón válido en formato .txt indicando la ruta del archivo, el mes y el año, entonces el sistema responde HTTP 200 y devuelve la constancia de la importación con el período importado y la cantidad de registros incorporados.
- AC-07 (RF-04): Dado un Administrador que importó exitosamente un padrón para un período, cuando consulta el registro de dicha importación, entonces el sistema responde HTTP 200 y muestra la fecha de importación, el período, el usuario que la realizó y la cantidad de registros importados.
- AC-08 (RF-05): Dado un usuario autenticado, cuando intenta calcular percepciones sin indicar el CUIT, la fecha, un importe mayor a cero o el código de provincia de entrega, entonces el sistema responde HTTP 400, dado que cualquiera de estas condiciones faltantes dispara el mismo resultado.
- AC-09 (RF-06): Dado un usuario autenticado, cuando intenta calcular percepciones para un CUIT que no existe en el padrón del período y la provincia de entrega no está adherida a SIRCIP según el Anexo C (por ejemplo Corrientes), entonces el sistema responde HTTP 200 con una lista vacía de percepciones de ingresos brutos.
- AC-10 (RF-07): Dado un usuario con rol administrador o usuario, cuando intenta calcular percepciones para un período no importado, entonces el sistema responde HTTP 404.
- AC-11 (RF-08): Dado un Administrador que intenta importar un archivo de padrón inválido o corrupto, cuando la importación falla, entonces el sistema responde HTTP 422, registra el error, el usuario importador, el período y la fecha de importación, y ese registro queda disponible para consulta.
- AC-12 (RF-09): Dado un usuario autenticado como administrador, cuando borra el padrón de un período y luego consulta un CUIT de dicho padrón para ese período, entonces el sistema responde HTTP 404.
- AC-13 (RF-09): Dado un usuario autenticado como administrador que borró el padrón de un período, cuando consulta el historial de importaciones, entonces el sistema responde HTTP 200 y muestra dicho padrón marcado como borrado.
- AC-14 (RF-09): Dado un usuario con rol Usuario autenticado, cuando intenta eliminar el padrón de un período, entonces el sistema responde HTTP 403.
- AC-15 (RF-10): Dado un usuario autenticado con rol Administrador, cuando accede a la página de consulta de importaciones, entonces el sistema responde HTTP 200 y muestra el listado de importaciones realizadas, tanto las exitosas como las que tuvieron error.
- AC-16 (RF-10): Dado un Administrador autenticado, cuando consulta la página de importaciones, entonces el sistema responde HTTP 200 y le muestra todas las importaciones realizadas por cualquier Administrador, sean propias o de terceros.
- AC-17 (RF-10): Dado un usuario con rol Usuario autenticado, cuando intenta acceder a la página de consulta de importaciones, entonces el sistema responde HTTP 403.
- AC-18 (RF-12): Dado un archivo de padrón con al menos una línea que no cumple el formato de campos definido en el Anexo A, cuando un Administrador lo importa, entonces el sistema responde HTTP 422, rechaza la importación completa, no persiste ningún registro de dicho período, y registra el error conforme a RF-08.
- AC-19 (RF-11): Dado un archivo de padrón donde todas las líneas cumplen el formato de campos definido en el Anexo A, cuando un Administrador lo importa, entonces el sistema responde HTTP 200 y persiste la totalidad de los registros del período.
- AC-20 (RF-05, RNF-04): Dado el padrón importado con la línea `202603,30100100106,XXXX SA,901,34,C,5555555555555555555432110` (letra de alícuota C = 0.05%), un importe facturado (neto gravado, sin IVA) de $1000 y provincia de entrega Catamarca (código Campo 7 = 2, no inscripto con sobretasa), cuando se solicita el cálculo de percepciones para el CUIT 30100100106, entonces el sistema responde HTTP 200 y devuelve una Percepción IIBB SIRCIP de $0.50 (neto × 0.05%) y una Percepción por sobretasa de Catamarca de $10 (neto × 1%), totalizando $10.50 de percepciones.
- AC-21 (RF-05, RNF-04): Dado el padrón importado con la línea `202603,30100100106,XXXX SA,901,34,C,1555555555545555555512220` (letra de alícuota C = 0.05%), un importe facturado (neto gravado, sin IVA) de $1000 y provincia de entrega Mendoza (código Campo 7 = 4, jurisdicción no adherida a SIRCIP con alta, alícuota local de Mendoza del 1.5% según Anexo B), cuando se solicita el cálculo de percepciones para el CUIT 30100100106, entonces el sistema responde HTTP 200 y devuelve una Percepción IIBB SIRCIP de $0.50 (neto × 0.05%) y una Percepción local de Mendoza de $15 (neto × 1.5%), totalizando $15.50 de percepciones.
- AC-22 (RF-05, RNF-04): Dado el padrón importado con la línea `202603,30100100106,XXXX SA,901,34,C,5555555555555555555432110` (letra de alícuota C = 0.05%), un importe facturado (neto gravado, sin IVA) de $1000 y provincia de entrega Capital Federal (código Campo 7 = 1, inscripto), cuando se solicita el cálculo de percepciones para el CUIT 30100100106, entonces el sistema responde HTTP 200 y devuelve una Percepción IIBB SIRCIP de $0.50 (neto × 0.05%).
- AC-23 (RF-13): Dado un CUIT que no existe en el padrón del período indicado, una provincia de entrega adherida a SIRCIP según el Anexo C (por ejemplo Capital Federal) y un importe facturado (neto gravado, sin IVA) de $1000, cuando se solicita el cálculo de percepciones para dicho CUIT, entonces el sistema responde HTTP 200 y devuelve una Percepción por no inscripto de $20 (neto × 2%).
- AC-24 (RF-05, RNF-04): Dado el padrón importado con la línea `202603,30100100106,XXXX SA,901,34,C,1115111111111111111111110` (letra de alícuota C = 0.05%), un importe facturado (neto gravado, sin IVA) de $1000 y provincia de entrega Santa Fe (código Campo 7 = 5, jurisdicción no adherida a SIRCIP, sin alta), cuando se solicita el cálculo de percepciones para el CUIT 30100100106, entonces el sistema responde HTTP 200 y devuelve únicamente una Percepción IIBB SIRCIP de $0.50 (neto × 0.05%), sin percepción local adicional.

- AC-25 (RF-14): Dado un usuario con rol Administrador autenticado, cuando solicita importar un padrón indicando una ruta que, una vez resuelta, queda fuera del directorio de importación configurado, entonces el sistema responde HTTP 400, no lee el archivo indicado y no registra la importación en el historial.
- AC-26 (RF-08): Dado un usuario con rol Administrador autenticado, cuando solicita importar un padrón indicando una ruta dentro del directorio de importación configurado en la que no existe ningún archivo, entonces el sistema responde HTTP 422, registra la importación como fallida y esa constancia queda disponible para consulta.
- AC-27 (RNF-01): Dado un archivo de padrón válido de un millón de registros, cuando un Administrador lo importa, entonces el sistema responde HTTP 200 y la importación completa demora menos de 60 segundos.
- AC-28 (RNF-02): Dado un usuario dado de alta en la base de datos, cuando se inspecciona la contraseña almacenada para ese usuario, entonces el valor guardado es un hash bcrypt y no coincide con la contraseña en texto plano.
- AC-29 (RNF-03): Dado un usuario autenticado cuya última actividad ocurrió hace más de 24 horas, cuando solicita una operación que requiere autenticación, entonces el sistema responde HTTP 401.
- AC-30 (RNF-05): Dado un padrón importado de un millón de registros, cuando se solicitan al menos 1.000 cálculos individuales de percepciones, entonces el percentil 99 del tiempo de respuesta es menor a 2 segundos.
## Fuera de Alcance
- No hay una página de registración de usuarios. Los usuarios se dan de alta manualmente en una base de datos.
- No puede realizarse varias veces la importación de un padrón de un período, dicho de otra forma no hay importación parcial o modificación de un padrón importado. Para volver a importarlo primero hay que eliminarlo y luego volver a importarlo.
- No hay RBAC configurable: los permisos de los dos roles (Administrador y Usuario) son fijos, no hay pantalla ni funcionalidad para definir o modificar permisos.
- No hay aislamiento de datos entre usuarios: el padrón, las importaciones y los cálculos son recursos compartidos entre todos los usuarios autenticados; el sistema no persiste un historial de cálculos por usuario individual.
- No se automatiza la descarga del padrón desde el Portal Federal Tributario: esa descarga la realiza manualmente el Administrador; el sistema solo automatiza la importación y el almacenamiento a partir del archivo .txt ya descargado y dejado en el disco del servidor, dentro del directorio de importación configurado (ver RF-03).
- No se suben archivos de padrón desde el navegador: no hay pantalla de carga de archivos. El Administrador deja el .txt en el disco del servidor y el sistema lo lee de ahí indicando su ruta (ver RF-03).
- No se define comportamiento ante importaciones simultáneas del mismo período, ni ante solicitudes de cálculo mientras una importación de ese período está en curso.

## Riesgos y Dependencias
- Riesgo: La tabla de jurisdicciones adheridas a SIRCIP (Anexo C) no tiene versionado por período ni un mecanismo de actualización definido, a diferencia del padrón que se reimporta mensualmente (RF-03/RF-04). Si la adhesión de una jurisdicción cambia, no hay ningún RF que contemple actualizar esta tabla.
- Dependencia: SQL Server para almacenar los usuarios. Usuarios ingresados en dicha base.
- Dependencia: Documento de casos de prueba de cálculo de percepciones (entrada: CUIT, fecha, importe, provincia; salida esperada: provincias, alícuotas, tipo e importes), necesario para verificar el RNF-04. Resuelta: formalizado como AC-20, AC-21, AC-22, AC-23 y AC-24.
- Dependencia: tabla de alícuotas locales por jurisdicción no adherida a SIRCIP, necesaria para calcular la percepción local cuando el Campo 7 de la jurisdicción de entrega indica código 4. Resuelta: formalizada en el Anexo B, en base al archivo `SIRCIP - Padrón - Campo 7.xlsx`.
- Dependencia: reglas para determinar el código de Campo 7 por jurisdicción y calcular el importe de percepción a partir de él. Resuelta: la determinación del código está formalizada en la tabla de aplicación de códigos del Anexo A, y el cálculo del importe (alícuota del Campo 6 sobre el neto gravado, más la percepción adicional por sobretasa del 1% cuando corresponda) está formalizado en el Anexo B.
- Dependencia: formato del archivo de padrón (estructura de registro, separador, campos y su significado). Resuelta: formalizado en el Anexo A.
- Dependencia: tabla de jurisdicciones adheridas a SIRCIP y alícuota fija de la percepción por no inscripto, necesarias para RF-06 y RF-13. Resuelta: formalizada en el Anexo C y en el Anexo B (2%); a diferencia de las demás tablas del Anexo B, estos datos fueron provistos directamente por el usuario del proyecto, sin archivo fuente documental citado.

## Anexo A: Diseño de Registro del Padrón (Archivo de Importación)
Referencia para RF-03. El padrón se descarga en formato .txt desde el menú "Descargas" del sistema SIRCIP dentro del Portal Federal Tributario. Cada línea es un registro de contribuyente con campos separados por coma (CSV sin encabezado).

| Nro. | Campo | Formato | Ejemplo |
|---|---|---|---|
| 1 | Período del padrón | aaaamm | 202603 |
| 2 | Nº de CUIT del contribuyente | Numérico(11) | 30100100106 |
| 3 | Razón social del contribuyente | Alfanumérico(70) | Empresa de prueba |
| 4 | Jurisdicción sede del contribuyente | Numérico(3) | 904 |
| 5 | CRC del contribuyente del período (1) | Numérico(2) | 34 |
| 6 | Letra de alícuota única (2) | Alfanumérico(1) | B |
| 7 | Campo 7 — estado del contribuyente por jurisdicción (3) | Numérico(25) | 5225355222512555552512420 |

Ejemplo de registro:

```
202603,30100100106,Empresa de prueba,904,34,B,5225355222512555552512420
```

**(1) CRC (Código de Redundancia Cíclica):** valor numérico de 10 a 99 que varía por contribuyente mes a mes. Se debe conservar junto con el resultado del cálculo, ya que luego se utiliza al presentar la Declaración Jurada (Campo 2), donde el sistema valida que se haya aplicado el padrón correcto al practicar la percepción.

**(2) Set de alícuotas (Campo 6):**

| Letra | % | Letra | % | Letra | % | Letra | % |
|---|---|---|---|---|---|---|---|
| A | 0.00% | G | 0.40% | M | 1.20% | S | 2.50% |
| B | 0.01% | H | 0.50% | N | 1.40% | T | 3.00% |
| C | 0.05% | I | 0.60% | O | 1.50% | U | 3.50% |
| D | 0.10% | J | 0.70% | P | 1.60% | V | 4.00% |
| E | 0.20% | K | 0.80% | Q | 1.80% | W | 4.50% |
| F | 0.30% | L | 1.00% | R | 2.00% | X | 5.00% |

**(3) Campo 7 — estado por jurisdicción:** cadena numérica de 25 posiciones que se lee de derecha a izquierda. El primer carácter (el más a la derecha) es siempre 0 y debe descartarse; las 24 posiciones restantes (leídas de derecha a izquierda, de la 2ª a la 25ª) se corresponden en orden con la identificación de cada una de las 24 jurisdicciones del Convenio Multilateral.

Para el cálculo de percepciones se evalúa únicamente la posición correspondiente a la jurisdicción donde ocurre la operación (jurisdicción de entrega o de prestación). Valores posibles de cada posición:

| Valor | Significado |
|---|---|
| 1 | Inscripto |
| 2 | No inscripto con sobretasa |
| 3 | No inscripto sin sobretasa |
| 4 / 5 | Jurisdicción no adherida |

Ejemplos de generación e interpretación de este campo: [planilla de referencia](https://docs.google.com/spreadsheets/d/1MXUlg43Ng-xBIx7xO5epLf21qJCEX7oFWpCzZ2b8PIk/edit?usp=sharing).

**Tabla de aplicación de códigos (Campo 7):** el código que corresponde a cada jurisdicción se determina según si el contribuyente es CM o Local, si tiene alta en el padrón, si la jurisdicción de entrega está dada de alta para ese contribuyente y si dicha jurisdicción está adherida a SIRCIP:

| Caso | Contribuyente | En padrón | Jurisdicción de entrega dada de alta | Jurisdicción de entrega adherida | Aplica padrón SIRCIP | Aplica sobretasa | Aplica no inscripto | Código Campo 7 |
|---|---|---|---|---|---|---|---|---|
| 1 | CM | Sí | Sí | Sí | Sí | No | No | 1 |
| 2 | CM | Sí | Sí | No | Sí | No | No | 4 |
| 3 | CM | Sí | No | Sí | Sí | Sí | No | 2 (puede ser 3 si el contribuyente está excluido general, en cuyo caso no se aplica la sobretasa) |
| 4 | CM | Sí | No | No | Sí | No | No | 5 |
| 5 | — | No | — | Sí | No | No | Sí | — |
| 6 | — | No | — | No | No | No | No | — |
| 7 | Local | Sí | Sí | Sí | Sí | No | No | 1 |
| 8 | Local | Sí | No | Sí | Sí | Sí | No | 2 (puede ser 3 si el contribuyente está excluido general, en cuyo caso no se aplica la sobretasa) |
| 9 | Local | Sí | No | No | Sí | No | No | 5 |

Los casos 5 y 6 corresponden a contribuyentes que no tienen alta en ningún padrón (CM ni Local); no son un valor de Campo 7 propiamente dicho, sino la situación de un CUIT no encontrado en el padrón importado. El caso 6 (jurisdicción de entrega no adherida a SIRCIP, ver Anexo C) no genera ninguna percepción (RF-06). El caso 5 (jurisdicción de entrega adherida a SIRCIP, ver Anexo C) genera una percepción por no inscripto con alícuota fija del 2% (RF-13, ver fórmula en Anexo B).

Los ejemplos completos con el Campo 7 armado dígito a dígito para las 24 jurisdicciones (casos CM, CM con excluido general y Local) están en el archivo del proyecto `SIRCIP - Padrón - Campo 7.xlsx`, hojas "Ejemplo CM", "Ejemplo 2 CM", "Ejemplo Local" y "Excluido General".

## Anexo B: Cálculo del Importe de la Percepción
Referencia para RF-05. La percepción se calcula sobre el **neto gravado** (el importe facturado, sin considerar el IVA), aplicando la alícuota indicada en el Campo 6 del padrón:

```
Importe de la percepción = Neto gravado (importe facturado, sin IVA) × Alícuota (Campo 6)
```

Este cálculo se realiza para cada jurisdicción que, según el Campo 7 (ver "Tabla de aplicación de códigos" en el Anexo A), corresponda calcular percepción. Cuando el CUIT sí está en el padrón, esta percepción base corresponde a los códigos de Campo 7 1, 2, 3, 4 y 5 (ver Anexo A).

Cuando el Campo 7 de la jurisdicción indica código 2 (no inscripto con sobretasa), se genera además una **percepción adicional por sobretasa**, independiente de la anterior (no se suma a la alícuota del Campo 6), calculada sobre el mismo neto gravado con una alícuota fija del **1%**:

```
Importe de la percepción por sobretasa = Neto gravado (importe facturado, sin IVA) × 1%
```

Es decir, para esa jurisdicción el sistema debe devolver dos líneas de percepción: una por la alícuota del Campo 6 y otra por la sobretasa del 1%. Cuando el código es 3 (no inscripto sin sobretasa, contribuyente excluido general) no corresponde esta percepción adicional, solo la de la alícuota del Campo 6.

Cuando el Campo 7 de la jurisdicción de entrega indica código 4 (jurisdicción no adherida a SIRCIP, contribuyente con alta), también se genera una **percepción local adicional**, independiente de la percepción SIRCIP, calculada sobre el mismo neto gravado:

```
Importe de la percepción local = Neto gravado (importe facturado, sin IVA) × Alícuota local de la jurisdicción
```

A diferencia de la sobretasa del código 2, la alícuota de esta percepción local no es fija: depende de la normativa propia de cada jurisdicción no adherida y no forma parte del padrón SIRCIP. Cuando el código es 5 (también jurisdicción no adherida a SIRCIP, pero sin que la jurisdicción de entrega esté dada de alta para el contribuyente) no corresponde esta percepción local adicional, solo la percepción base del Campo 6.

Cuando el CUIT no está en el padrón del período (RF-06/RF-13) y la jurisdicción de entrega está adherida a SIRCIP (ver Anexo C), corresponde una **percepción por no inscripto**, calculada sobre el mismo neto gravado con una alícuota fija del **2%**, sin sobretasa adicional:

```
Importe de la percepción por no inscripto = Neto gravado (importe facturado, sin IVA) × 2%
```

Cuando el CUIT no está en el padrón y la jurisdicción de entrega no está adherida a SIRCIP, no corresponde ninguna percepción (RF-06).

**Tabla de alícuotas locales por jurisdicción:**

| Jurisdicción | Alícuota | Jurisdicción | Alícuota |
|---|---|---|---|
| 901 - Capital Federal | 1.5% | 913 - Mendoza | 1.5% |
| 902 - Buenos Aires | 2% | 914 - Misiones | 1% |
| 903 - Catamarca | 2.5% | 915 - Neuquén | 0.5% |
| 904 - Córdoba | 3% | 916 - Río Negro | 1% |
| 905 - Corrientes | 3.5% | 917 - Salta | 1.5% |
| 906 - Chaco | 4% | 918 - San Juan | 2% |
| 907 - Chubut | 4.5% | 919 - San Luis | 2.5% |
| 908 - Entre Ríos | 4% | 920 - Santa Cruz | 3% |
| 909 - Formosa | 3.5% | 921 - Santa Fe | 3.5% |
| 910 - Jujuy | 3% | 922 - Santiago del Estero | 4% |
| 911 - La Pampa | 2.5% | 923 - Tierra del Fuego | 4.5% |
| 912 - La Rioja | 2% | 924 - Tucumán | 4% |

Fuente: archivo del proyecto `SIRCIP - Padrón - Campo 7.xlsx`, hoja "Alicuota por jurisdicción".

## Anexo C: Jurisdicciones Adheridas a SIRCIP
Referencia para RF-06 y RF-13. Cuando el CUIT sí está en el padrón, el código de Campo 7 ya viene resuelto por CUIT y período (ver Anexo A), y es ese código el que RF-05 decodifica y aplica directamente sin necesidad de esta tabla. Pero cuando el CUIT **no** está en el padrón del período, no existe Campo 7 para ese CUIT, y el sistema debe consultar esta tabla para saber si la jurisdicción de entrega está adherida a SIRCIP: si lo está, corresponde la percepción por no inscripto del 2% (RF-13); si no lo está, no corresponde percepción (RF-06).

| Jurisdicción | Adherida a SIRCIP |
|---|---|
| 901 - Capital Federal | Sí |
| 902 - Buenos Aires | Sí |
| 903 - Catamarca | Sí |
| 904 - Córdoba | Sí |
| 905 - Corrientes | No |
| 906 - Chaco | Sí |
| 907 - Chubut | Sí |
| 908 - Entre Ríos | No |
| 909 - Formosa | No |
| 910 - Jujuy | Sí |
| 911 - La Pampa | Sí |
| 912 - La Rioja | Sí |
| 913 - Mendoza | Sí |
| 914 - Misiones | Sí |
| 915 - Neuquén | Sí |
| 916 - Río Negro | Sí |
| 917 - Salta | Sí |
| 918 - San Juan | Sí |
| 919 - San Luis | No |
| 920 - Santa Cruz | Sí |
| 921 - Santa Fe | No |
| 922 - Santiago del Estero | Sí |
| 923 - Tierra del Fuego | Sí |
| 924 - Tucumán | No |

Fuente: dato provisto por el usuario del proyecto.

***
