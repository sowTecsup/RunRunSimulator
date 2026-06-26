---
tags: [script, ui]
---

# CreatureGridView.cs

**Ruta:** `UI/CreatureGridView.cs`

**Responsabilidad:** Grid read-only de criaturas (Odin TableList). Impulsado por eventos `GameEvents.OnRegistryChanged/OnRegistryReloaded`. Muestra tabla de rows con stats base (6 campos: CON/ATK/SPD/DEF/LCK/EVA), genética, estado, combates, crianzas.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[CreatureRegistrySO]], [[GameEvents]], [[CreatureDNA]], [[CreatureGridUITK]]

**CreatureRow struct (inner class):**
```csharp
[Serializable]
private class CreatureRow
{
    public string Name;          // CustomName o ToStringID()
    public Color Color;          // BaseColor (swatch)
    public CreatureGender Gender;
    public float CON;            // BaseConstitution
    public float ATK;            // BaseAttack
    public float SPD;            // BaseSpeed
    public float DEF;            // BaseDefense
    public float LCK;            // BaseLuck
    public float EVA;            // BaseEvasion
    public string Fights;        // "X (Y)" = FightCount (WinCount)
    public int Breeds;           // BreedCount
    public string Mother;        // CustomName del MotherID o "—" / "???"
    public string Father;        // CustomName del FatherID o "—" / "???"
    public string State;         // "SOLD" / "DEAD" / "Breeding" / "In Queue" / "Free"
    public string Born;          // "dd/MM/yyyy HH:mm" o "—"
}
```

**Static From():** construye CreatureRow desde CreatureDNA (resuelve nombres de padres vía registry)

**Método Rebuild():** ordena por BirthDate descendente (más recientes primero), crea TableList vía Odin
