---
tags: [script, data, expedition, rule, polymorphic]
---

# ExpeditionRuleBase.cs

**Ruta:** `Data/Expedition/ExpeditionRuleBase.cs`

**Responsabilidad:** Clase abstracta `[Serializable]` para reglas de evaluación de expedición. Cada subclase implementa `Matches(Percept, self, rules, out score)` para puntuar un percepto como objetivo viable. Propósito: desacoplar lógica de evaluación de scoring → todo vive en SO, nada en C#. Contiene enum `ExpeditionGoal` (actualmente solo `SeekMaterial = 0`, extensible a: llevar a salida, confrontar, huir, reagruparse, obedecer). Subclase concreta: `SeekMaterialRule` (score = distancia inversa × sesgo de osadía).

## Enum ExpeditionGoal

```csharp
public enum ExpeditionGoal
{
    SeekMaterial = 0,
    // future: Seek Exit, Confront, Flee, Regroup, Obey
}
```

## Clase Abstract ExpeditionRuleBase

**Métodos abstractos:**
- `Goal → ExpeditionGoal` — propiedad de solo lectura que identifica el tipo de meta (para logging/filtering).
- `Matches(in Percept p, MoriMochiAgent self, ExpeditionRulesSO rules, out float score) → bool` — evaluador principal. Devuelve true si el percepto pasa el filtro; si true, `score` contiene su puntuación. Si false, `score` es ignorado. Puede acceder a `rules.ArriveDistance`, `rules.GiveUpSeconds`, etc. para lógica compartida.
- `Summary() → string` — descripción legible de la regla (para Inspector/logging).

**Parámetro `rules` (ExpeditionRulesSO):** necesario para leer tuning global (`ArriveDistance`, `RepathInterval`, `GiveUpSeconds`); permite que múltiples reglas compartan configuración.

## Clase Concreta SeekMaterialRule

**Responsabilidad:** "busca material visible a cierta distancia, modulado por osadía".

**Campos:**
- `MaxDistance` (float, min 0, default 0) — distancia máxima de búsqueda (0 = sin límite).
- `BoldnessBias` (-1 a +1, default 0) — sesgo genético: valores positivos → agentes audaces van más lejos; negativos → tímidos evitan lejanos.

**Evaluación (Matches):**
1. Chequea `p.Kind == PerceivableKind.Material` → false si no es material.
2. Chequea `p.Source != null && p.Source.gameObject.activeInHierarchy` → false si el recolectable está inactivo.
3. Si `MaxDistance > 0`, chequea `dist <= MaxDistance` → false si está muy lejos.
4. Calcula score: `(1 / (1 + dist)) * (1 + bias * (boldness - 0.5) * 2)`.
   - Componente distancia: inversa (más cerca = más score).
   - Componente osadía: si `BoldnessBias=0`, factor es 1 (neutral); si `BoldnessBias=+1`, criaturas audaces (boldness→1) multiplican score por 2; tímidas (boldness→0) por 0. Esto permite diseñadores favorecer ciertos tipos de criatura.
5. Devuelve true + score.

**Summary:** "Busca material a distancia `<= MaxDistance` (sin limite si 0), sesgo osadia `BoldnessBias`".

## Patrón de Extensión

Futuros rules heredarían:

```csharp
public class SeekExitRule : ExpeditionRuleBase {
  public override ExpeditionGoal Goal => ExpeditionGoal.SeekExit;
  [SerializeField] public float ExitRadius = 5f;
  public override bool Matches(in Percept p, MoriMochiAgent self, ExpeditionRulesSO rules, out float score) {
    score = 0f;
    if (p.Kind != PerceivableKind.Exit) return false;  // nuevo Kind
    // ... scoring logic
  }
  public override string Summary() => $"Busca salida (radius {ExitRadius})";
}
```

## Invariantes S97

- **Serializable:** `[Serializable]` permite que `List<ExpeditionRuleBase>` en `ExpeditionRulesSO` se persista (lista polimórfica via Odin).
- **Funcional puro:** `Matches()` no muta estado; es safe llamar múltiples veces.
- **Percept read-only:** se pasa `in Percept` (by-ref) para evitar copia; es read-only.
- **Score sin restricciones:** puede ser negativo, cero, infinito; `TryEngage()` en `AgentExpedition` solo se importa de máximo.
- **MaxDistance=0:** convención de "sin límite" (más eficiente que `float.MaxValue`).
- **Sesgo multiplicativo:** `bias * (value - 0.5) * 2` asegura rango [-bias, +bias] alrededor del factor 1.
- **Sin percept.Source.GetComponent():** la responsabilidad de validar componentes (ej: MaterialPickup) está en `AgentExpedition.TryEngage()` post-filtering.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ExpeditionRulesSO]] (contenedor de lista polimórfica)
- [[AgentExpedition]] (evaluador, itera reglas en TryEngage)
- [[Percept]] (entrada de Matches)
- [[MoriMochiAgent]] (segundo parámetro de Matches, acceso a DNA.Boldness)
- [[PerceivableKind]] (Material = 4, extensible a Exit/Danger)
