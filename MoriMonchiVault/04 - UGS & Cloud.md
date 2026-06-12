---
tags: [memory-bank, ugs, cloud, auth, persistence]
---

# 04 — UGS & Cloud

## Responsabilidad Core (TL;DR)
Coordina la autenticación del jugador mediante Unity Player Accounts, la persistencia en la nube (Cloud Save) y la interacción con backend serverless (Unity Cloud Code) para mecánicas asíncronas.

## Source of Truth & Centralización
- **Manager Principal:** `CloudSyncService.cs`. Gestiona auth, push/pull de Cloud Save.
- **Cloud Tooling (DEV):** `CloudCodeTester.cs`. Bypasea timers y ejecuta scripts forzadamente para pruebas en editor.
- **Mecanismo Auth:** Autenticación anónima silenciosa que retiene sesión (`SessionTokenExists`). El primer login abre navegador.

## Flujo de Sincronización (Cloud Save)
1. **Auto-Pull (Login):** Al autenticar (`OnSignedInComplete`), se aísla el guardado al `playerID` (`SaveSystem.SetUserScope`), y se fuerza un `PullAsync()` para bajar la última data de la nube.
2. **Auto-Push (Mutaciones):** `GameManager` escucha `OnRegistryChanged` / `OnFurnitureChanged` y delega en `CloudSyncService.PushToCloud()` (fire-and-forget).
3. **Seguridad:** Usa `sync_meta.json` local para marcar timestamps y detectar si el usuario hizo manipulación local de savefiles.

## Arquitectura Backend (Cloud Code & Scheduler)
- **Archivos:** Todos los scripts JS están en la carpeta `CloudCode/`.
- **Scheduler Indirerecto (CRÍTICO):** El UGS Scheduler NO llama al código directo. Emite un evento cron (`.sched`) -> el servicio Trigger (`.tr`) lo atrapa -> ejecuta el script JS (ej. `process-matchmaking.js`). 
- **Deploy:** Solo vía CLI (`ugs deploy <file>`). Requiere Service Account con rol "Cloud Code Publisher" y "Environments Admin".

## Reglas de Oro (Quirks de Custom Data)
- **Top-Level Arrays prohibidos:** La API de UGS rechaza arrays en JSON. Las pools/colas SIEMPRE se serializan como `{ entries: [...] }`.
- **Escritura (`setCustomItem`):** Usar exclusivamente la firma de 3 argumentos: `(projectId, customId, body)` donde `body = {key, value}`.
- **Lectura Múltiple inestable:** El método `getCustomItems([key1, key2])` falla silenciosamente. SIEMPRE leer keys una por una en un loop y validar.
- **Service Account vs Project Secrets:** *Service Account* es para deploy CLI; *Project Secrets* son env-vars para que los scripts consuman APIs.
