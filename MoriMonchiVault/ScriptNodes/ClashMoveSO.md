---
tags: [script, data, expedition, clash]
---

# ClashMoveSO.cs

**Ruta:** `Data/Expedition/ClashMoveSO.cs`

**Responsabilidad:** Define los parámetros de un movimiento de choque basado en la parte corporal elegida (Horn/Wings/Back). Un ClashMoveSO contiene timings de anticipación, bloqueo y golpe, alcance e impacto, y parámetros específicos de cada tipo de ataque (embestida con recoil, picada con ángulo, coletazo con radio barrido). Cada movimiento expone sus gestos de aviso y golpe para sincronía con MonchiGestureDriver. **S114:** fase Holding con duración independiente.

## Estructura

```csharp
public enum ClashSlot { Horn = 0, Wings = 1, Back = 2 }

public class ClashMoveSO : ScriptableObject
{
    public ClashSlot Slot;
    
    // Timings
    public float AnticipationSeconds;   // default 0.3
    public float HoldSeconds;           // S114: default 0.3
    public float StrikeSeconds;         // default 1.2
    
    // Alcance e impacto compartido
    public float Range;                 // default 5
    public float HitRadius;             // default 1.1
    public float Impulse;               // default 9
    public float UpBias;                // 0-1, default 0.25
    
    // Embestida (Horn)
    public float DashSpeed;             // default 14
    public float DashAcceleration;      // default 60
    public float SelfRecoil;            // default 0
    
    // Picada (Wings)
    public float LaunchAngle;           // 5-85 grados, default 45
    
    // Coletazo (Back)
    public float SweepRadius;           // default 2.2
    
    // Gestos
    public string TellGesture;          // p.ej. "Roar"
    public string StrikeGesture;        // p.ej. "" (vacío)
}
```

## Campos públicos

| Campo | Rango | Default | Descripción |
|-------|-------|---------|-------------|
| `Slot` | — | — | Parte corporal (Horn/Wings/Back) |
| `AnticipationSeconds` | [0, ∞) | 0.3 | Duración del tell previo al bloqueo |
| `HoldSeconds` | [0, ∞) | 0.3 | **S114:** Duración de la fase de bloqueo (embestida recta multi-golpe, lead acotado en picada) |
| `StrikeSeconds` | [0.1, ∞) | 1.2 | Duración del golpe efectivo (ventana de impacto) |
| `Range` | [0.5, ∞) | 5 | Distancia máxima a rivales viables (m) |
| `HitRadius` | [0.2, ∞) | 1.1 | Radio de impacto alrededor del monchi (m) |
| `Impulse` | [0, ∞) | 9 | Magnitud del impulso (m/s) |
| `UpBias` | [0, 1] | 0.25 | Factor de sesgo vertical en impulso |
| `DashSpeed` | [0, ∞) | 14 | Velocidad de embestida (Horn, m/s) |
| `DashAcceleration` | [0, ∞) | 60 | Aceleración de embestida (Horn, m/s²) |
| `SelfRecoil` | [0, ∞) | 0 | Retroceso del atacante (Horn) |
| `LaunchAngle` | [5, 85] | 45 | Ángulo de lanzamiento (Wings, grados) |
| `SweepRadius` | [0, ∞) | 2.2 | Radio de barrido (Back, m) |
| `TellGesture` | — | "Roar" | Gesto de aviso (Anticipating) |
| `StrikeGesture` | — | "" | Gesto del golpe (Striking) |

## Métodos públicos

- `Summary() → string` — devuelve resumen de debugging: "Horn: alcance 5 m, impulso 9"

## Ciclo de timing S114

1. **Anticipation:** `AnticipationSeconds`, tell visual/auditivo
2. **Hold:** `HoldSeconds`, bloqueo físico (embestida recta, lead acotado en picada)
3. **Strike:** `StrikeSeconds`, impacto y resolución

## Conexiones

**Entrada:**
- [[ClashTuningSO]] referencia cada ClashMoveSO por slot (Horn/Wings/Back)
- [[AgentClash.TryEngage()]] consulta el movimiento elegido
- [[AgentClash.ForceMove()]] recibe ClashMoveSO explícito

**Salida:**
- Se aplica en [[AgentClash]] en fases Anticipating, Holding (S114), Striking, Resolving
- Los gestos (TellGesture/StrikeGesture) los lee [[MonchiGestureDriver]]
- Telegraph con hold en [[CreatureCueDrawer]]

## Vinculado a

- [[Index/20 - MVP Combate]], S114
- [[ClashTuningSO]]
- [[AgentClash]]
- [[CreatureCueDrawer]]
