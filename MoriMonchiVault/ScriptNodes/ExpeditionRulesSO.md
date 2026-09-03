---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** **Singleton por escena** (`Current` static) que centraliza tuning de expedición. Contiene lista polimórfica Odin de reglas `ExpeditionRuleBase`, y knobs de navegación compartidos (`ArriveDistance`, `RepathInterval`, `GiveUpSeconds`). En tienda, `Current == null` → expedición desactiva. En Arena, `Current` apunta a asset `ExpeditionRules.asset`. Botón `PopulateDefaults()` precarga una `SeekMaterialRule`.

## Propiedades Estáticas

- `Current → ExpeditionRulesSO` — singleton por escena; set por `OnEnable()` del asset activo. Null si no hay asset activo en la tienda.

## Campos Públicos

**Lista de reglas (Odin):**
- `rules` (List<ExpeditionRuleBase>, IReadOnlyList pública) — lista polimórfica de reglas de evaluación. Dibujable con `[ListDrawerSettings(ShowFoldout=false, DefaultExpandedState=true)]` para comodidad de edición.

**Tuning de navegación (compartido entre reglas):**
- `ArriveDistance` (float, min 0.1, default 0.9) — distancia planar al recolectable para considerarse "llegado" (m).
- `RepathInterval` (float, min 0.05, default 0.5) — cada cuántos segundos se recalcula el destino en NavMesh (throttle).
- `GiveUpSeconds` (float, min 1, default 12) — timeout: si no se llega al recolectable en este tiempo, abandona (s).

## Métodos Públicos

- `PopulateDefaults()` — **Botón Odin**: inicializa `rules` si está null y agrega una `SeekMaterialRule()` por defecto. Marca dirty.

## Ciclo de Vida

```csharp
OnEnable():
  Current = this
  → Al cargar la escena Arena, este asset se vuelve Current.
  → Si la escena es la tienda (sin ExpeditionRules asset), Current sigue siendo anterior (o null si es primera escena).

OnDisable():
  (no implementado, pero si se desa se podría limpiar Current)
```

## Invariantes S97

- **Singleton por escena:** `Current` refleja el asset activo. En tienda `Current=null` (Arena nunca se mezcla con tienda en mismo juego); en Arena sandbox `Current=asset ExpeditionRules`.
- **Null-safe:** `AgentExpedition.TryEngage()` chequea `rules == null` antes de iterar; si null, devuelve false sin crash.
- **Lista polimórfica (Odin):** `[OdinSerialize]` + `SerializedScriptableObject` permite almacenar lista de subclases concretas de `ExpeditionRuleBase` sin necesidad de wrapper.
- **Shared tuning:** `ArriveDistance`, `RepathInterval`, `GiveUpSeconds` son consultados por `AgentExpedition.TickExpedition()`; evita duplicación en cada regla.
- **Extensibilidad:** nuevas reglas (`SeekExitRule`, `ConfrontRule`, etc.) se agregan a la lista en Inspector; cero cambios en C#.

## Estructura Interna

```csharp
public class ExpeditionRulesSO : SerializedScriptableObject
{
  private List<ExpeditionRuleBase> rules = new();  // polimórfica
  public static ExpeditionRulesSO Current { get; private set; }

  private void OnEnable() { Current = this; }  // singleton setup

  public IReadOnlyList<ExpeditionRuleBase> Rules => rules;  // interfaz read-only

  public float ArriveDistance = 0.9f;   // shared tuning
  public float RepathInterval = 0.5f;
  public float GiveUpSeconds = 12f;
}
```

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/06 - Player & World]]

## Conexiones

- [[ExpeditionRuleBase]] / [[SeekMaterialRule]] (lista de reglas concretas)
- [[AgentExpedition]] (lector: `ExpeditionRulesSO.Current`, itera `rules`, consulta tuning)
- [[ArenaSandbox]] (configura asset referenciado en Inspector)
