---
name: conventional-commit
description: Redacta el mensaje de un commit siguiendo Conventional Commits, en español, acorde al estilo ya usado en este repo. Se usa cuando el usuario pide armar, redactar o revisar un mensaje de commit, o antes de correr `git commit`.
---

# Conventional Commit

Generás el mensaje de commit, no el commit en sí (eso lo hace el flujo normal de
`git commit`). El objetivo es que el mensaje sea consistente con el historial
existente del repo y siga el formato Conventional Commits.

## Formato

```
<tipo>(<alcance opcional>): <descripción en infinitivo, minúscula, sin punto final>

<cuerpo opcional: el POR QUÉ, no el qué — el diff ya muestra el qué>

<footer opcional: BREAKING CHANGE: ..., Refs: #123>
```

## Tipos permitidos

- `feat`: agrega una funcionalidad nueva.
- `fix`: corrige un bug.
- `docs`: cambios solo de documentación (README, AGENTS.md, PRD.md, comentarios).
- `refactor`: cambio de código que no altera comportamiento ni agrega feature.
- `test`: agrega o corrige tests.
- `chore`: mantenimiento que no toca código de producción (deps, config, scripts).
- `style`: formato/espacios, sin cambio de lógica.
- `perf`: mejora de performance.
- `build` / `ci`: build system o pipelines.

## Reglas de estilo (según el historial de este repo)

- Mensaje en español, verbo en infinitivo: "agregar", "reemplazar", "corregir",
  no "agregado" ni "agrega".
- Descripción corta y concreta: qué cambia, no por qué (el por qué va en el
  cuerpo si hace falta y no es obvio desde el diff).
- Sin punto final en la primera línea.
- Si el cambio toca un solo archivo o tema puntual, no hace falta cuerpo —
  una sola línea alcanza (ver commits como "ajuste en el PRD.").
- Usá cuerpo solo cuando el motivo no es evidente por el diff (una decisión no
  obvia, un workaround, una corrección a algo reportado por el usuario).

## Paso a paso

1. Mirá `git status` y `git diff --staged` (o `git diff` si nada está staged)
   para entender el cambio real, no lo que el usuario dijo que iba a hacer.
2. Elegí el tipo según el efecto dominante del diff. Si mezcla tipos (ej. un
   feat que también actualiza docs), priorizá el tipo del cambio principal.
3. Escribí la primera línea siguiendo el formato de arriba.
4. Si el cambio no es autoexplicativo por el diff, agregá 1-2 líneas de cuerpo
   con el POR QUÉ.
5. Si el cambio rompe compatibilidad, agregá un footer `BREAKING CHANGE: <qué
   rompe y qué hay que hacer>`.
6. Mostrale el mensaje propuesto al usuario antes de commitear, salvo que ya
   haya pedido explícitamente que commitees directo.

## No hacer

- No inventar un scope si no aporta claridad.
- No mezclar varios cambios no relacionados en un solo commit: si el diff
  staged toca cosas independientes, avisale al usuario y sugerí separarlos.
- No agregar coautoría ni firmas salvo que el flujo de commit del proyecto lo
  pida explícitamente (ver reglas de git en las instrucciones generales).
