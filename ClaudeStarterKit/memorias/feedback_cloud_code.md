---
name: feedback-cloud-code-sdk
description: Usar fetch REST API directo en Cloud Code JS scripts — NO el SDK @unity-services/cloud-save-1.x
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 269b3ad1-967b-4010-8852-9ece3d016405
---

En scripts de Cloud Code (UGS), usar `fetch` nativo contra la REST API de Cloud Save en lugar de importar `@unity-services/cloud-save-1.x`.

**Why:** El SDK tiene firmas de API y versiones disponibles inconsistentes. `DataApi`, `getCustomItem`, `setCustomItem` generaron errores en líneas múltiples (57, 78-81, 89, 102, 109-117, etc.) en dos intentos distintos con `1.2` y `1.5`. `fetch` nativo (Node.js 18+, disponible en Cloud Code) funciona sin dependencias de versión y el comportamiento de la REST API es estable y documentado.

**How to apply:** Al escribir cualquier Cloud Code script que acceda a Cloud Save:
- Usar `fetch` directamente con las URLs REST oficiales
- Custom Data (pool global compartido): `GET/PUT https://cloud-save.services.api.unity.com/v1/data/projects/{projectId}/environments/{environmentId}/custom/{key}` con `Authorization: Bearer {context.serviceToken}`
- Player Data (por jugador): `GET https://...players/{playerId}/items?keys=key` y `PATCH https://...players/{playerId}/items` con body `{ data: [{ key, value }] }`, usando `context.serviceToken` para escribir en otros jugadores
- Body de Custom Data PUT: `{ value: JSON.stringify(data) }`
- Evitar `require("@unity-services/cloud-save-1.x")` a menos que se verifique la versión exacta disponible en el runtime del proyecto
