---
tags: [script, debug, dev-tools]
---

# GeneticsLabPreview.cs

**Ruta:** `Core/GeneticsLabPreview.cs`

**Responsabilidad:** Panel de debug Odin para previsualizador de genética (generador random de DNAs, loader por ID string, breakdown de rareza). **S61:** Botón "Generate Random Creature" llama `CreatureGenerator.GenerateRandom(gameManager.Database)` sin `FurTypeDatabaseSO` ni `RarityOddsTableSO` (ambos null → fallback uniform).

## Métodos Privados (Buttons)

| Método | Descripción |
|--------|-------------|
| `GenerateRandomCreature()` | **S61** Genera DNA vía `CreatureGenerator.GenerateRandom(gameManager.Database)` uniforme (sin odds, sin fur type ponderado), muestra en inspector |
| `LoadFromID()` | Parsea DNA string format (BODYSHAPEID-ARMID-EYEID-MOUTHID-RRGGBB), carga, valida partes en database |
| `RefreshRarityBreakdown()` | Actualiza breakdown de rareza de cada parte contra database |
| `ValidateDNA()` | Logea si cada parte existe en database |

## Cambios S61

**GenerateRandomCreature():**
```csharp
private void GenerateRandomCreature()
{
    if (gameManager == null) { Debug.LogWarning("[GeneticsLabPreview] No GameManager assigned."); return; }
    currentDNA       = CreatureGenerator.GenerateRandom(gameManager.Database);  // CAMBIO: sin furDb, sin odds
    currentDNAString = currentDNA.ToStringID();
    RefreshRarityBreakdown();
    Debug.Log($"[GeneticsLabPreview] Generated (preview): {currentDNAString}");
}
```

**Cambios:**
- Antes: `GenerateRandom(gameManager.Database, gameManager.RarityOddsTable, gameManager.FurTypeDatabase)`
- Ahora: `GenerateRandom(gameManager.Database)` (defaults: `furDb = null`)
- Resultado: DNA uniforme en todas las partes (sin rarity filter, sin FurType ponderado)

**Impacto:**
- Preview es 100% uniform (cada parte spawn con igual probabilidad)
- FurType también uniform (fallback en generador cuando furDb=null)
- Útil para testing general de genética; no refleja odds reales de mint

## Campos Serializados (Setup)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `gameManager` | `GameManager` | Referencia a GameManager para acceder Database |

## Campos Mostrables (Current Creature)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `currentDNA` | `CreatureDNA` | DNA actual generado/cargado |
| `currentDNAString` | `string` | Representación string del DNA |
| `rarityBodyShape` | `Rarity` | Breakdown: rareza de BodyShape contra database |
| `rarityArms` | `Rarity` | Breakdown: rareza de Arms |
| `rarityEyes` | `Rarity` | Breakdown: rareza de Eyes |
| `rarityMouth` | `Rarity` | Breakdown: rareza de Mouth |
| `rarityScore` | `string` | Promedio de rareza (enum + float) |

## Flujo UI

1. Botón "Generate Random Creature" (verde) → GenerateRandomCreature() → DNA uniforme → display
2. Botón "Load from ID" (azul) → LoadFromID() → parsea string → DNA cargado → display + validation
3. Breakdown de rareza actualiza contra database en ambos casos

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Dev Tools]]
- [[CreatureGenerator]]
- [[CreatureDatabaseSO]]

## Conexiones

**Entrada:**
- GameManager (Database ref)
- UI buttons: GenerateRandomCreature, LoadFromID

**Salida:**
- Debug logs (generated DNA strings, validation warnings)
- Inspector display (currentDNA, breakdown)

## Notas S61

- GenerateRandom sin parámetros (furDb=null) = uniforme puro (debug/testing)
- No refleja odds reales de mint (que usa GameManager.FurTypeDatabase ponderado)
- Preview es herramienta de testing, no de validación de odds
