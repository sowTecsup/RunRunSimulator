---
tags: [memory-bank, persistence, identity, save, events]
---

# 07 — Persistence & Identity

## Responsabilidad Core (TL;DR)
Gestiona la persistencia local en disco (JSON aislado por cuenta), la identidad inmutable de las criaturas, y orquesta las mutaciones a través del bus de eventos global para mantener el desacoplamiento.

## Source of Truth & Centralización
- **Event Bus:** `GameEvents.cs` (namespace global, estático).
- **Orquestador de Guardado:** `GameManager.cs`. El único con derecho a decidir *cuándo* guardar/subir a la nube.
- **Lógica de I/O:** `SaveSystem.cs`. Lee y escribe JSON usando `Newtonsoft.Json`.
- **Cache en Memoria:** `CreatureRegistrySO.cs` (vista `[ReadOnly]` para el editor, poblado por el JSON).

## Flujo de Guardado (Reactividad)
1. **Mutación:** Un sistema (Breeding, Combat, Tienda) altera los datos.
2. **Notificación:** Ese sistema emite `GameEvents.OnRegistryChanged` u `OnFurnitureChanged`. NUNCA llama a guardar directo.
3. **Persistencia Local:** `GameManager` atrapa el evento y ordena a `SaveSystem` escribir al disco.
4. **Subida (Push):** Inmediatamente después, `GameManager` dispara `CloudSyncService.PushToCloud()` (fire-and-forget).
5. **Excepción (Needs):** Los stats en tiempo real (Health/Energy/Affect) mutan en RAM cada frame SIN disparar eventos, para no quemar el disco ni la API. Se guardan con `FlushToCloud()` al cerrar o pausar la app.

## Identidad de Criatura
- **Genetic String (`ToStringID()`):** Inmutable, representa las partes. Ej: `BS0-A3-E1-M2-FF00AA`.
- **UniqueID:** Clave real en la base de datos, incluye Ticks para diferenciación. Ej: `BS0-A3-E1-M2-FF00AA-{Ticks}`.
- **Restricción de Nomenclatura:** Los IDs autogenerados de las partes jamás deben llevar guión medio (`-`).

## Reglas de Oro (Invariantes)
- **Cero Acoplamiento:** Los publicadores de un evento no conocen a los oyentes. Los oyentes reciben lo que necesitan dentro del propio evento (Payload).
- **Scoping por Jugador:** Los archivos JSON se guardan con el formato `_<playerId>` para evitar que dos usuarios de Unity Player Accounts mezclen sus datos en el mismo PC.
- **Detección de Trampas:** `sync_meta.json` anota timestamps locales para evitar que el usuario edite el archivo JSON haciendo "rollbacks" de progreso.
