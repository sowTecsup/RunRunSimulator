---
tags: [script, data, expedition, clash]
---

# ClashMoveSO.cs

**Ruta:** `Data/Expedition/ClashMoveSO.cs`

**Responsabilidad:** Define los parámetros de un movimiento de choque basado en la parte corporal elegida (Horn/Wings/Back). Un ClashMoveSO contiene timings de anticipación y golpe, alcance e impacto, y parámetros específicos de cada tipo de ataque (embestida con recoil, picada con ángulo, coletazo con radio barrido). Cada movimiento expone sus gestos de aviso y golpe para sincronía con MonchiGestureDriver.

## Estructura

```csharp
public enum ClashSlot { Horn = 0, Wings = 1, Back = 2 }

public class ClashMoveSO : ScriptableObject
{
    public ClashSlot Slot;
    
    // Timings
    public float AnticipationSeconds;   // default 0.3
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

- **Slot:** determina qué parte del cuerpo se usa (Horn/Wings/Back)
- **AnticipationSeconds:** cuánto tiempo el monchi muestra su intención antes de golpear
- **StrikeSeconds:** duración del golpe efectivo (tiempo disponible para impactar)
- **Range:** distancia máxima a rivales viables (m)
- **HitRadius:** radio de impacto alrededor del monchi (m)
- **Impulse:** magnitud del impulso aplicado al rival (m/s)
- **UpBias:** factor de sesgo vertical en el impulso (0=horizontal, 1=vertical)
- **DashSpeed/DashAcceleration:** parámetros de NavMeshAgent para embestida (Horn)
- **LaunchAngle:** ángulo de lanzamiento para picada (Wings, 5-85 grados)
- **SweepRadius:** radio de barrido del coletazo (Back)
- **TellGesture:** gesto de aviso que MonchiGestureDriver dispara en Anticipating
- **StrikeGesture:** gesto del golpe en Striking (a menudo vacío)

## Métodos públicos

- `Summary() → string` — devuelve resumen de debugging: "Horn: alcance 5 m, impulso 9"

## Conexiones

**Entrada:**
- [[ClashTuningSO]] referencia cada ClashMoveSO por slot (Horn/Wings/Back)
- [[AgentClash.TryEngage()]] consulta el movimiento elegido
- [[AgentClash.ForceMove()]] recibe ClashMoveSO explícito

**Salida:**
- Se aplica en [[AgentClash]] en fases Anticipating, Striking, Resolving
- Los gestos (TellGesture/StrikeGesture) los lee [[MonchiGestureDriver]]

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ClashTuningSO]]
- [[AgentClash]]
