using UnityEngine;
namespace MoriMonchiSimulator
{

public static class NpcNameBank
{
    private static readonly string[] firstNames =
    {
        "Carmen",  "Beto",    "Lupita",  "Chucho",  "Rosa",
        "Pancho",  "Tere",    "Nacho",   "Marisol", "Toño",
        "Chela",   "Memo",    "Paty",    "Lalo",    "Cuca",
        "Beni",    "Mago",    "Pepe",    "Yolanda", "Goyo",
        "Lucha",   "Tacho",   "Mari",    "Chayo",   "Fito",
        "Nena",    "Quique",  "Dora",    "Chano",   "Vicky",
        "Ramiro",  "Chabe",   "Polo",    "Maru",    "Gera",
        "Licha",   "Tavo",    "Coco",    "Mela",    "Beni",
    };

    private static readonly string[] lastNames =
    {
        "Pérez",     "Gómez",     "Ramírez",   "Soto",      "Vargas",
        "Mendoza",   "Cruz",      "Reyes",     "Flores",    "Castro",
        "Ortega",    "Núñez",     "Rincón",    "Bravo",     "Salas",
        "Quiroz",    "Lozano",    "Mejía",     "Cano",      "Pacheco",
        "Tovar",     "Zúñiga",    "Aguilar",   "Barrios",   "Gallardo",
        "Peña",      "Villa",     "Cordero",   "Madrigal",  "Carrillo",
        "Solís",     "Nava",      "Espinoza",  "Trejo",     "Olvera",
        "Cisneros",  "Garza",     "Rendón",    "Bustos",    "Maldonado",
    };

    public static string GetRandomName() =>
        $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
}
}
