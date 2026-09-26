# Examen Parcial — Plataforma de Incidencias (bicicletas compartidas)

Operaciones registra averías por estación. Pantalla `/Operaciones/Incidencias` con búsqueda (Algolia),
listado rápido (Redis) y actualización en tiempo real (PieHost). Tres desarrollos simultáneos
fusionados sin perder funciones, desplegado en Render.

- **URL Render:** `https://examen-parcial-emkh.onrender.com`
- **Commit desplegado:** `c7201fce4721b93298974ea6902c11b43d6b7d1f` (merge PR #4)
- **Stack:** .NET 10, ASP.NET Core MVC + Identity, EF Core + SQLite, Algolia.Search 7.x,
  Redis (StackExchange), PieSocket JS + publish HTTP, Docker.

## Usuario de prueba (seed)

| Email | Clave |
|-------|-------|
| `supervisor@empresa.pe` | `Supervisor123*` |

Seed: Estación Central (freno B-014, prioridad 3), Parque Norte (anclaje, 2), Malecón Sur (llanta, 1) abiertas + 1 cerrada.

## Local

```bash
dotnet tool install --global dotnet-ef
dotnet run --project IncidenciasApp   # migra + seed automáticos
```

## Variables de entorno

| Variable | Efecto si falta |
|----------|-----------------|
| `Algolia__AppId`, `Algolia__ApiKey` (búsqueda), `Algolia__IndexName` | Respaldo: LIKE en base, badge "Local" |
| `Redis__ConnectionString` | Caché en memoria local |
| `PieHost__ClusterId`, `PieHost__Key`, `PieHost__Secret`, `PieHost__Room` | Se registra el salto, cierre sigue funcionando |

Claves solo en Render, nunca en el repo. La clave de admin de Algolia no se usa en ningún lado.

## Pruebas

- **Algolia:** buscar `Freno` trae B-014; buscar texto de una cerrada no la muestra; vacía = lista habitual.
- **Redis:** primera carga badge "Base de datos", segunda "Redis"; al cerrar vuelve a "Base de datos". Logs dicen REDIS/BASE DE DATOS.
- **PieHost:** dos sesiones; al cerrar en una, la otra quita la fila y avisa sin recargar; `RealtimeConfig` sin login no aplica (requiere auth); `POST /hubs` n/a (se usa PieSocket).
- **Fusiones:** `git log --graph --oneline --all` muestra ancestro común `c34f1c2`, merge A, resolución en B, merge B, resolución en C, merge C.

## Ramas y PRs

| Rama | PR | Título final aportado |
|------|----|----------------------|
| `feature/busqueda-algolia` | #1 | Incidencias abiertas encontradas |
| `feature/cache-redis` | #2 | Incidencias abiertas con consulta rápida |
| `feature/websocket-piehost` | #3 | Incidencias abiertas en tiempo real (final) |
| `deploy/render` | #4 | Dockerfile + render.yaml + README |

Resolución 1 (B): se quedó el título de B y se unió el formulario de búsqueda con el caché.
Resolución 2 (C): título final de C; en `Cerrar` se guarda, se invalida caché y se publica el evento, en ese orden.
