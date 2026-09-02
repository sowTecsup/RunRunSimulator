---
tags: [index, furniture]
---

# 10 - Furniture & Building

**Responsabilidad:** Construccion en grilla 3D (Building Mode), inventario (Hotbar/Storage), economia/tienda de muebles.

**Building Mode:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[BuildingInputs]] | `Player/BuildingInputs.cs` | Dueno action map Building (eventos estaticos) |
| [[BuildModeController]] | `Systems/Furniture/BuildModeController.cs` | Maquina 4 estados (Browsing/Placing/Editing/Deleting) |
| [[FurnitureService]] | `Systems/Furniture/FurnitureService.cs` | CRUD muebles (place, remove, rotate) |
| [[FurnitureSpawner]] | `Systems/Furniture/FurnitureSpawner.cs` | Instancia/remueve muebles en escena |
| [[FurniturePivotAligner]] | `Systems/Furniture/FurniturePivotAligner.cs` | Editor: alineacion pivotes |
| [[PlacedFurnitureMarker]] | `Systems/Furniture/PlacedFurnitureMarker.cs` | Marker en muebles colocados |
| [[PlacementGrid]] | `Systems/Furniture/PlacementGrid.cs` | Grid posicionamiento (ocupacion, snap) |

**Store / Economia:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[StoreManager]] | `Systems/Store/StoreManager.cs` | Validacion y ejecucion de compras |
| [[StoreShopData]] | `Systems/Store/StoreShopData.cs` | Pricing y stock por listing |
| [[ShopCatalogSO]] | `Systems/Store/ShopCatalogSO.cs` | Catalogo con descuentos y restock |
| [[DeliveryBox]] | `Systems/Store/DeliveryBox.cs` | Paquete fisico delivery (IInteractable) |
| [[StorageContainer]] | `Systems/Store/StorageContainer.cs` | Caja almacenamiento fisico |

**Data Definitions:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[FurnitureDefinitionSO]] | `Data/FurnitureDefinitionSO.cs` | Data de mueble individual |
| [[FurnitureDatabaseSO]] | `Data/FurnitureDatabaseSO.cs` | Database de muebles |
| [[ItemDefinitionSO]] | `Data/ItemDefinitionSO.cs` | Data de item no-mueble |
| [[ItemDatabaseSO]] | `Data/ItemDatabaseSO.cs` | Database de items |
| [[PlacedFurniture]] | `Data/PlacedFurniture.cs` | Record serializable mueble colocado |

**Contenedores Criaturas:** MoriMochiContainer (base captura), StoreContainer (restaura needs), BreedingContainer (auto-pair breeding).

**Ring de combate (S95, demo Dragon RPS E3):** `ScriptableObjects/FurnitureSystem/Furniture3x3_Ring.asset` (`F9`, 3x3, `Functional`, precio 100; listado en `ShopCatalogs/ShopCatalog.asset` con stock 1) → prefab `Resources/Prefabs/Furnitures/Containers/Ring.prefab` (raiz con `FurniturePivotAligner` 3x3 + `BoxCollider` trigger 3x1x3 como el BreedingRoom + `PanelTrigger(panel = Combat)`; hijos: `Podium` (instancia anidada de `Prefabs/Arena/Podium.prefab`, escala 1,7) y `Stage/SpotPlayer` (-0,75, 0,3, 0) / `Stage/SpotRival` (0,75, 0,3, 0) mirandose, reservados para E4). Todo creado por `eval_file` (`mk_ring.cs`), sin tocar la escena: el spawner lo instancia desde el registro. El jugador lo abre con E (raycast `QueryTriggerInteraction.Collide` + `GetComponentInParent<IInteractable>`). Detalle y auditoria en [[Index/21 - Combate v3 - Dragon RPS]] §9.12.

**Reglas de Oro:**
- MVC: Grid (matematico) Service (validador) Spawner (visualizador reactivo)
- Pivote raiz de mueble en centro inferior de footprint XZ
- Y flotante: no se persiste en JSON (raycast al cargar escena)
