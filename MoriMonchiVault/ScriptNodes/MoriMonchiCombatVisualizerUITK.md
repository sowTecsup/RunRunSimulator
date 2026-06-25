---
tags: [script, ui]
---

# MoriMonchiCombatVisualizerUITK.cs

**Ruta:** `UI/MoriMonchiCombatVisualizerUITK.cs`

**Responsabilidad:** Barra de HP world-space de UN combatiente del visualizer. Componente HIJO del prefab del peleador (NO la raíz, para que el billboard no gire el modelo), con un `UIDocument` que apunta a `CombatHpBar.uxml` (elementos `name` y `fill`).

**Driven por el Service (sin `side`):** ya no se filtra por lado. El `CombatVisualizerService` la maneja por referencia directa:
- `Bind(string displayName)`: fija el nombre y resetea el HP a 100%.
- `SetHp(float pct)`: fija el % objetivo; `Update` interpola el `fill` con `fillLerpSeconds`.

**Binding resiliente + fix de árbol huérfano:** `EnsureRefs()` detecta cuando el `UIDocument` reconstruye su árbol (al reactivar el GameObject tras una muerte → `Back`) comparando `docRoot != root`; re-resuelve los elementos y re-marca el nombre para reescribirlo. El nombre/HP se guardan como datos y se reaplican en `Update` hasta que las refs existan (a prueba de timing del re-spawn). Sin esto, al retroceder la barra quedaba apuntando al árbol viejo y "desaparecía".

**Billboard:** en `LateUpdate` orienta el panel hacia `Camera.main` (`Quaternion.LookRotation(toCam)`, `uprightOnly`), igual que [[NameTag]]. Así es independiente de la rotación del ancla: se puede rotar el slot del oponente para que mire a la pantalla sin que la barra rote ni el texto se invierta.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualizerService]], [[CreatureDNA]]
