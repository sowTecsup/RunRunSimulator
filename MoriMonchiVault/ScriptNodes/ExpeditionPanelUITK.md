---
tags: [script, ui, expedition, uitk]
---

# ExpeditionPanelUITK.cs

**Ruta:** `UI/ExpeditionPanelUITK.cs`

**Responsabilidad (S120):** Panel UITK para elegir elenco antes de bajar a arena. Lista criaturas vivas del registro filtradas por energía (≥30), elegibles si no están ocupadas. Máximo 3 selecciones. Emite `ExpeditionBridge.RequestDeparture(ids)` con los `UniqueID` de las elegidas. Implementa `IUINavigable` para keyboard/gamepad.

**Campos:**
- `[SerializeField] UIDocument document` — referencia al documento UITK
- `[SerializeField] UIPanelType panel = UIPanelType.Expedition` — tipo de panel (8)
- `[SerializeField, Min(1)] int maxPick = 3` — máximo de criaturas elegibles
- `[SerializeField, Range(0, 100)] float minEnergy = 30f` — energía mínima para elegir
- `List<VisualElement> cards` — tarjetas de criaturas
- `List<CreatureDNA> dnas` — referencias a DNAs mostrados
- `List<bool> eligible` — estado de elegibilidad por índice
- `List<bool> picked` — estado de selección por índice
- `int focused` — índice con foco actual

**Métodos públicos:**
- `void OnUINavigate(Vector2 dir)` — navega izquierda/derecha entre tarjetas
- `void OnUISubmit()` — toggle selección de tarjeta con foco
- `bool OnUICancel()` — retorna false (no usar cancel aquí)

**Métodos privados clave:**
- `void Rebuild()` — reconstruye lista desde registry: filtra vivas/no-vendidas, ordena elegibles primero + energía descendente
- `int CompareEntries(CreatureDNA a, CreatureDNA b)` — comparador: elegibles > no-elegibles, energía descendente
- `VisualElement BuildCard(CreatureDNA dna, bool ok)` — crea tarjeta con retrato (MonchiPortraitUI), nombre, estado (Ocupada/Energía/Cansada), barra de energía con color (verde ≥60, amarillo ≥30, rojo <30)
- `void ToggleAt(int index)` — selecciona/deselecciona si elegible y bajo maxPick
- `void UpdateGoButton()` — activa botón "Ir" solo si hay ≥1 seleccionada
- `void Depart()` — llamado al hacer clic "Ir": cierra panel → `ExpeditionBridge.RequestDeparture(ids)` con UniqueID

**Flujo:**
1. `OnEnable`: suscribe a `UIManager.OnPanelSet/Toggle`
2. `Start`: cableado de botones, `Rebuild()`, registro como navegable
3. Panel se muestra: `Rebuild()` filtra del registry
4. Navegación por teclado/gamepad o clic directo
5. `Depart()`: emite IDs → ExpeditionBridge

**S120-S122:** Implementación nueva. Introduce `UIPanelType.Expedition = 8` y flujo de selección de equipo previo a la bajada. Textos localizados en tabla `Strings` en/es.

**Invariantes:**
- Máximo `maxPick` elegidas a la vez
- Solo elegibles (libres + energía ≥ `minEnergy`) pueden ser seleccionadas
- Suscripción/desuscripción simétrica `OnEnable`/`OnDisable`/`OnDestroy`
- El botón "Ir" habilitado solo con ≥1 elegida

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]] (§6c), [[GameScene]], `UI Toolkit/ExpeditionPanel.uxml`

**Conexiones:** [[ExpeditionBridge]], [[GameManager]], [[CreatureRegistrySO]], [[CreatureDNA]], [[UIManager]], [[MonchiPortraitUI]], [[Loc]]
