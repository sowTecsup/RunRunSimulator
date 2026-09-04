---
tags: [script, data, expedition, clash, singleton]
---

# ClashTuningSO.cs

**Ruta:** `Data/Expedition/ClashTuningSO.cs`

**Responsabilidad:** Singleton central que centraliza TODOS los parámetros de combate físico de la arena: movimientos por slot (Horn/Wings/Back), enganche de rivales, cooldowns, post-golpe (resolución, dazed), counter-attack, retreat y gracia de víctima. Llenado al inicio de escena en [[ArenaSandbox]] para que [[AgentClash.TryEngage()]] siempre tenga valores vigentes.

## Estructura

```csharp
public class ClashTuningSO : ScriptableObject
{
    public static ClashTuningSO Current { get; private set; }
    
    private void OnEnable() { Current = this; }
    
    // Movimientos por slot
    public ClashMoveSO Horn;
    public ClashMoveSO Wings;
    public ClashMoveSO Back;
    
    // Enganche
    public float EngageRange;           // default 5
    public float MinBoldness;           // 0-1, default 0.45
    public float Cooldown;              // default 8
    public float DiveMinDistance;       // default 4
    public int SweepMinRivals;          // default 2
    public float SweepRange;            // default 2.5
    
    // Después del golpe
    public float ResolveSeconds;        // default 0.4
    public float DazedSeconds;          // default 0.7
    public float ReengageBoldness;      // 0-1, default 0.7
    public float RetreatDistance;       // default 6
    public float VictimGraceSeconds;    // default 6
    public float ChainImmunitySeconds;  // default 0.8
}
```

## Campos públicos

**Movimientos:**
- **Horn/Wings/Back:** referencias a [[ClashMoveSO]] para cada tipo de ataque

**Enganche (TryEngage):**
- **EngageRange:** distancia máxima para iniciar choque (m)
- **MinBoldness:** coraje mínimo requerido (0-1) para intentar choque
- **Cooldown:** segundos entre intentos de choque (una vez que termina uno)
- **DiveMinDistance:** distancia mínima para elegir picada (Wings)
- **SweepMinRivals:** cantidad mínima de rivales en rango para elegir coletazo (Back)
- **SweepRange:** radio en el que contar rivales para coletazo

**Post-golpe:**
- **ResolveSeconds:** cuánto espera el atacante antes de poder roamear/decidir
- **DazedSeconds:** cuánto tiempo el golpeado está en estado Dazed (mareado), manejando solo la rotación
- **ReengageBoldness:** coraje mínimo para contra-atacar al atacante en Decide() (default 0.7)
- **RetreatDistance:** distancia a la que retrocede si no contra-ataca (m)
- **VictimGraceSeconds:** tiempo en el que la víctima NO puede ser golpeada de nuevo
- **ChainImmunitySeconds:** tiempo en el que el atacante es inmune a knockes en cadena del rival

## Métodos públicos

- `MoveFor(ClashSlot slot) → ClashMoveSO` — devuelve el movimiento correspondiente al slot

## Invariantes S100

- `Current` es un singleton accedido vía `OnEnable()` cuando la escena lo carga. Garantiza que [[AgentClash.TryEngage()]] siempre tenga tuning vigente.
- Si es `null` en algún punto, los métodos de AgentClash devuelven defaults (cooldown 8, DazedSeconds 0.7, ChainImmunitySeconds 0.8).
- El dominó de knockes se detiene si el atacante está inmune (línea 62 en [[AgentPhysics.HandleCollisionEnter()]]).

## Conexiones

**Entrada:**
- Instanciado en la escena como asset (.asset)
- Referencia serializada en [[ArenaSandbox]] (línea 14)

**Salida:**
- Consultado por [[AgentClash.TryEngage()]] → valida Boldness >= MinBoldness, EngageRange, etc.
- Consultado por [[AgentClash.Decide()]] → ReengageBoldness para counter-attack
- Consultado por [[AgentClash.ReceiveHit()]] → ChainImmunitySeconds
- Consultado por [[AgentClash.OnRecovered()]] → VictimGraceSeconds, DazedSeconds
- Consultado por [[AgentClash.Finish()]] → Cooldown

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ClashMoveSO]]
- [[AgentClash]]
- [[ArenaSandbox]]
