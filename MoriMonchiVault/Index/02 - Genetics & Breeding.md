---
tags: [index, genetics]
---

# 02 - Genetics & Breeding

**Responsabilidad:** Identidad, progresion y aspecto visual de criaturas. Emparejamiento genetico, herencia, ensamblaje 3D.

**Data Model:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[CreatureDNA]] | `Data/CreatureDNA.cs` | String genetico + metadata (UniqueID, linaje, stats, combat history, needs, busy state) |
| [[CreatureStats]] | `Data/CreatureStats.cs` | Calculo stats compuestos (HP/Attack/Speed) |
| [[NeedsState]] | `Data/NeedsState.cs` | Health/Energy/Affect runtime (NO dispara RegistryChanged) |
| [[CreatureDatabaseSO]] | `Data/CreatureDatabaseSO.cs` | Database maestra criaturas |
| [[CreatureNameBank]] | `Data/CreatureNameBank.cs` | Nombres procedurales |
| [[PersonalityProfileSO]] | `Data/PersonalityProfileSO.cs` | 6 arquetipos personalidad |
| [[RarityOddsTableSO]] | `Data/RarityOddsTableSO.cs` | Pesos rareza |
| [[CreatureGenerator]] | `Core/CreatureGenerator.cs` | Generador aleatorio de criaturas |

**Partes:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[BodyPart]] | `Data/Parts/BodyPart.cs` | Clase base abstracta (Icon, ID, Rarity, Tier, Set, HP/Attack/Speed) |
| [[ArmPart]] | `Data/Parts/ArmPart.cs` | Brazo concreto |
| [[EyePart]] | `Data/Parts/EyePart.cs` | Ojo concreto |
| [[MouthPart]] | `Data/Parts/MouthPart.cs` | Boca concreto |
| [[BodyShapePart]] | `Data/Parts/BodyShapePart.cs` | Cuerpo concreto |
| [[PartDatabaseSO]] | `Data/Databases/PartDatabaseSO.cs` | DB abstracta Dictionary<string,T> + SyncAllIDs + RollAllNames |
| [[ArmDatabaseSO]] | `Data/Databases/ArmDatabaseSO.cs` | DB brazos (prefijo A) |
| [[EyeDatabaseSO]] | `Data/Databases/EyeDatabaseSO.cs` | DB ojos (prefijo E) |
| [[MouthDatabaseSO]] | `Data/Databases/MouthDatabaseSO.cs` | DB bocas (prefijo M) |
| [[BodyShapeDatabaseSO]] | `Data/Databases/BodyShapeDatabaseSO.cs` | DB cuerpos (prefijo BS) |

**Visual Assembler:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[PartNameBank]] | `Data/PartNameBank.cs` | Palabras para nombres de partes |
| [[PartVisualBankSO]] | `Data/PartVisualBankSO.cs` | Mapa part ID a prefab BodyPartJoint |
| [[MoriMonchiVisualizer]] | `World/MoriMonchiVisualizer.cs` | Ensamblaje 3D en 6 sockets |
| [[BodyPartJoint]] | `World/BodyPartJoint.cs` | Punto conexion + isMirror |
| [[MoriMonchiController]] | `World/MoriMonchiController.cs` | Facade Agent + Visualizer |

**Breeding:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[BreedingService]] | `Systems/Breeding/BreedingService.cs` | Logica local de cruce + herencia |
| [[BreedingController]] | `Systems/Breeding/BreedingController.cs` | Controlador UI breeding |
| [[AsyncBreedingService]] | `Systems/Breeding/AsyncBreedingService.cs` | Breeding async server-side |
| [[BreedingAffinityTableSO]] | `Data/BreedingAffinityTableSO.cs` | Matriz 6x6 afinidad personalidad |
| [[InheritanceOddsTableSO]] | `Data/InheritanceOddsTableSO.cs` | Pesos herencia genetica |
| [[BreedingContainer]] | `World/BreedingContainer.cs` | Corral cria con auto-pair timer |

**Reglas de Oro:**
- Genetic String inmutable (jamas cambia tras nacer)
- Server autoritativo para timestamps de breed
- IDs de partes sin guion medio (-)
- Gender y Personality NO forman parte del genetic string
- NeedsState NO dispara RegistryChanged (solo flush en quit/pause)
- **Potencial por parte (decisión de Juan, S95):** cuerno, espalda y ala tienen un potencial entero **1-10** (`CreatureDNA.HornPotential/BackPotential/WingPotential`). Ninguna parte es intrínsecamente mejor que otra (lo que varía son los quirks); `Tier`/`Rarity` de `BodyPart` quedan fuera del combate y son deuda de diseño ([[Index/11 - Technical Debt]]). Al nacer por compra sale **1-3** (`CreatureGenerator.RandomMintPotential`); por cría es **promedio de los padres ±1** (`BreedingService.InheritPotential`, desempate aleatorio del .5, clamp 1-10). Es la potencia del combate v3 ([[Index/21 - Combate v3 - Dragon RPS]] §9.10). No forma parte del genetic string.
