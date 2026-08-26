using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatBoardBuilder : MonoBehaviour
    {
        [SerializeField] private BoardLayoutSO layout;
        [SerializeField] private Color lightColor = new Color(0.78f, 0.78f, 0.72f);
        [SerializeField] private Color darkColor = new Color(0.52f, 0.55f, 0.5f);
        [SerializeField] private float baseThickness = 0.25f;
        [SerializeField] private float levelHeight = 1f;
        [SerializeField] private Vector3 wiggleAmplitude = new Vector3(0.04f, 0.07f, 0.04f);
        [SerializeField] private float wiggleFrequency = 0.05f;

        private CombatBoard board;
        private readonly Dictionary<Vector2Int, Transform> blocks = new Dictionary<Vector2Int, Transform>();

        public CombatBoard Board
        {
            get
            {
                if (board == null && layout != null) board = new CombatBoard(layout, levelHeight);
                return board;
            }
        }

        private void Awake()
        {
            for (int x = 0; x < Board.Width; x++)
            {
                for (int z = 0; z < Board.Depth; z++)
                {
                    BuildCell(x, z);
                }
            }
        }

        private void BuildCell(int x, int z)
        {
            Vector2Int cell = new Vector2Int(x, z);
            if (!Board.InBounds(cell))
                return;

            int elevation = Board.GetElevation(cell);
            float height = baseThickness + elevation * Board.LevelHeight;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Cell_{x}_{z}";
            cube.transform.SetParent(transform);
            cube.transform.localPosition = new Vector3(
                x * CombatBoard.CellSize + CombatBoard.CellSize * 0.5f,
                elevation * Board.LevelHeight - height * 0.5f,
                z * CombatBoard.CellSize + CombatBoard.CellSize * 0.5f);
            cube.transform.localScale = new Vector3(CombatBoard.CellSize, height, CombatBoard.CellSize);

            cube.GetComponent<Renderer>().material.color = (x + z) % 2 == 0 ? lightColor : darkColor;

            MMWiggle wiggle = cube.AddComponent<MMWiggle>();
            wiggle.PositionActive = true;
            wiggle.PositionWiggleProperties = new WiggleProperties
            {
                WigglePermitted = false,
                WiggleType = WiggleTypes.Random,
                AmplitudeMin = -wiggleAmplitude,
                AmplitudeMax = wiggleAmplitude,
                FrequencyMin = wiggleFrequency,
                FrequencyMax = wiggleFrequency
            };

            blocks[cell] = cube.transform;
        }

        public Transform GetBlock(Vector2Int cell)
        {
            return blocks.TryGetValue(cell, out Transform block) ? block : null;
        }
    }
}
