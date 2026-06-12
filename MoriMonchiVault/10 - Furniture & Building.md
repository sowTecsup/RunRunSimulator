---
tags: [memory-bank, furniture, building, shop, stage-3]
---

# 10 — Furniture & Building

## Responsabilidad Core (TL;DR)
Gestiona el sistema de construcción de la tienda en grilla 3D (Building Mode), el inventario del jugador (Hotbar y Storage), y la economía/comercio de muebles y accesorios.

## Source of Truth & Centralización
- **Data (Muebles Estáticos):** `FurnitureDefinitionSO` (Id = `F#`, prefijo F). Ocupan celdas.
- **Data (Props Sueltos):** `ItemDefinitionSO` (Id = `I#`, prefijo I). Objetos físicos agarrables.
- **Estado Mundo:** `FurnitureRegistrySO` (Guarda qué muebles están colocados, rotados y en qué celda).
- **Inventario Jugador:** `PlayerInventorySO` (Desbloqueos de muebles, props almacenados y memoria del Hotbar).
- **Flujos Core:** `FurnitureService.cs` (API para ubicar/quitar) y `StoreManager.cs` (API comercial).

## Flujo de Construcción (Build Mode)
1. **Activación:** Se entra al modo con la tecla *B*. Esto activa el Action Map `Building` **sobre** el mapa `Player` (aditivo, permite seguir caminando).
2. **Máquina de Estados (`BuildModeController`):**
   - *Browsing:* Selecciona del hotbar (1-4) o usa raycast para editar/borrar un mueble existente.
   - *Placing:* Mueve el ghost verde/rojo que hace snap a la `PlacementGrid`. Rota con *R*.
   - *Editing/Deleting:* Mueve un mueble levantado o confirma borrado.
3. **Colocación (Event-Driven):** `FurnitureService.TryPlace` valida celdas libres y colisiones con el nivel (`obstacleMask`). Si es válido, lo graba en el registro y dispara `OnFurnitureChanged`.
4. **Mallas:** `FurnitureSpawner` escucha el evento y regenera las mallas, pegándolas al suelo real vía raycast vertical (`TrySampleFloor`).

## Flujo Comercial y Económico
- **Tienda (`ShopCatalogSO`):** Desacopla el ítem de su precio. Envuelve un ítem en `StoreShopData`, configurando reglas temporales de descuentos (por días/meses) y stock.
- **Muebles (`BuyFurniture`):** Va directo al inventario de desbloqueos permanentes (un `F#` se compra una vez).
- **Props Físicos (`BuyWorldProp`):** Genera una `DeliveryBox` en la tienda. Romperla tira el ítem al suelo.
- **Storage Container:** Mueble físico en el mundo. Traga props arrojados en él (lo saca del mundo, lo mete a `PlayerInventorySO`) y tiene UI para retirarlos a voluntad.

## Contenedores de Criaturas (`MoriMochiContainer` y subclases)

Familia de muebles que capturan MoriMonchis lanzados dentro de su trigger volume y los confinan al área NavMesh `BreedingRoom`.

| Clase | Propósito |
|-------|-----------|
| `MoriMochiContainer` | Base: captura, confinamiento, censo. `Occupants` público. |
| `StoreContainer` | Vitrina de exhibición. Restaura needs a `restoreRate/s` (overrides el decay del agente). Futuro hook de compra por NPCs. |
| `BreedingContainer` | Corral de cría. Cada `rollInterval` segundos tira un dado usando `BreedingAffinityTableSO` (matriz 6×6 de personalidades). Si hay pareja válida y el dado pasa, dispara el breed (async o local según `useAsyncBreed`). |

**Setup en Unity para ambos subtipos:** trigger `BoxCollider`, piso pintado con el Area `BreedingRoom` + rebake, componente derivado (no el base), `StoreContainer` solo necesita ajustar `restoreRate`; `BreedingContainer` necesita `BreedingAffinityTableSO` asset (botón *Seed Defaults*) y referencia a `BreedingController` (ver pendientes).

## Reglas de Oro (Invariantes)
- **Arquitectura MVC calcada:** Grid = Ocupación Matemática; Service = Modificador/Validador; Spawner = Visualizador que reacciona a eventos de mutación.
- **Pivote Central en Prefabs:** El pivote raíz del mueble debe estar en el **centro inferior** de su Footprint XZ. De lo contrario, rotará fuera de la cuadrícula. Para alinear fácil, usar el componente de editor `FurniturePivotAligner`.
- **Y Flotante:** La posición vertical (Y) no se persiste en el JSON. Al cargar la escena, el raycast lee el suelo y ajusta el mueble, permitiendo que toleren terrenos irregulares o cambios en el escenario.
