---
tags: [script, world, npc, data]
---

# NpcNameBank.cs

**Ruta:** `World/Npc/NpcNameBank.cs`

**Responsabilidad:** Clase estática pura que genera nombres aleatorios (Nombre Apellido) en español para NPCs, espejo de `CreatureNameBank`. Sin estado mutable.

**Datos públicos:**
- `firstNames[]` (40 nombres ES): Carmen, Beto, Lupita, Chucho, Rosa, Pancho, Tere, Nacho, Marisol, Toño, Chela, Memo, Paty, Lalo, Cuca, Beni, Mago, Pepe, Yolanda, Goyo, Lucha, Tacho, Mari, Chayo, Fito, Nena, Quique, Dora, Chano, Vicky, Ramiro, Chabe, Polo, Maru, Gera, Licha, Tavo, Coco, Mela.
- `lastNames[]` (40 apellidos ES): Pérez, Gómez, Ramírez, Soto, Vargas, Mendoza, Cruz, Reyes, Flores, Castro, Ortega, Núñez, Rincón, Bravo, Salas, Quiroz, Lozano, Mejía, Cano, Pacheco, Tovar, Zúñiga, Aguilar, Barrios, Gallardo, Peña, Villa, Cordero, Madrigal, Carrillo, Solís, Nava, Espinoza, Trejo, Olvera, Cisneros, Garza, Rendón, Bustos, Maldonado.

**Método público:**
- `GetRandomName()` → string. Devuelve `"{firstNames[Random]} {lastNames[Random]}"` (combinación aleatoria de nombre + apellido).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcAgent]], [[NpcThoughtTag]], [[CreatureNameBank]]
