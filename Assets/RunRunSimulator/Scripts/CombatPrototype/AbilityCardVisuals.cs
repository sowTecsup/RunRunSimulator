using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class AbilityCardVisuals
    {
        public static VisualElement BuildAbilityMiniGrid(CombatAbilitySO ability)
        {
            Color empty = Hex("#FFFFFF14");
            int minX = -2, maxX = 2, minY = -2, maxY = 2;
            if (ability.Targeting == TargetingMode.DirectionalTemplate && ability.TemplateOffsets != null)
            {
                for (int i = 0; i < ability.TemplateOffsets.Length; i++)
                {
                    Vector2Int off = ability.TemplateOffsets[i];
                    minX = Mathf.Min(minX, off.x);
                    maxX = Mathf.Max(maxX, off.x);
                    minY = Mathf.Min(minY, off.y);
                    maxY = Mathf.Max(maxY, off.y);
                }
                minX = Mathf.Max(minX, -4);
                maxX = Mathf.Min(maxX, 4);
                minY = Mathf.Max(minY, -4);
                maxY = Mathf.Min(maxY, 4);
            }

            int cols = maxX - minX + 1;
            int rows = maxY - minY + 1;
            int ax = -minX;
            int ay = -minY;

            Color[,] fill = new Color[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    fill[x, y] = empty;

            bool reinforceAnchor = false;

            if (ability.Type == AbilityType.Movement)
            {
                fill[ax, ay] = Hex("#59C96A");
            }
            else if (ability.Targeting == TargetingMode.DirectionalTemplate)
            {
                if (ability.TemplateOffsets != null)
                {
                    for (int i = 0; i < ability.TemplateOffsets.Length; i++)
                    {
                        Vector2Int off = ability.TemplateOffsets[i];
                        int gx = ax + off.x;
                        int gy = ay + off.y;
                        if (gx >= 0 && gx < cols && gy >= 0 && gy < rows) fill[gx, gy] = Hex("#FFD34D");
                    }
                }

                if (ability.Landing == LandingKind.BehindAnchor && ax - 1 >= 0) fill[ax - 1, ay] = Hex("#59C96A");
                else if (ability.Landing == LandingKind.AtAnchor) fill[ax, ay] = Hex("#59C96A");
                else if (ability.Landing == LandingKind.Stay) reinforceAnchor = true;
            }
            else if (ability.Targeting == TargetingMode.AirborneEnemy)
            {
                fill[ax, ay] = Hex("#FF6B5E");
                Vector2Int[] cardinals = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                for (int d = 0; d < cardinals.Length; d++)
                    for (int dist = 1; dist <= 2; dist++)
                    {
                        int gx = ax + cardinals[d].x * dist;
                        int gy = ay + cardinals[d].y * dist;
                        if (gx >= 0 && gx < cols && gy >= 0 && gy < rows) fill[gx, gy] = Hex("#FF6B5E40");
                    }
            }

            VisualElement grid = new VisualElement();
            grid.pickingMode = PickingMode.Ignore;
            grid.style.marginLeft = 8;
            grid.style.marginRight = 6;
            grid.style.flexShrink = 0;

            int cellSize = cols > 6 ? 5 : 7;
            for (int y = 0; y < rows; y++)
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.pickingMode = PickingMode.Ignore;
                for (int x = 0; x < cols; x++)
                {
                    VisualElement cell = new VisualElement();
                    cell.style.width = cellSize;
                    cell.style.height = cellSize;
                    cell.style.marginLeft = 1;
                    cell.style.marginRight = 1;
                    cell.style.marginTop = 1;
                    cell.style.marginBottom = 1;
                    cell.style.backgroundColor = fill[x, y];
                    cell.pickingMode = PickingMode.Ignore;
                    if (reinforceAnchor && x == ax && y == ay)
                    {
                        SetBorderWidth(cell, 1);
                        SetBorderColor(cell, Hex("#59C96A"));
                    }

                    row.Add(cell);
                }

                grid.Add(row);
            }

            return grid;
        }

        public static Label BuildAbilityTag(CombatAbilitySO ability)
        {
            string text;
            Color color;
            if (ability.Type == AbilityType.Movement)
            {
                text = "→mov";
                color = Hex("#7EC8FF");
            }
            else
            {
                text = "⚔1";
                if (ability.Targeting == TargetingMode.AirborneEnemy) text += " aéreo";
                color = Hex("#FF9E8F");
            }

            Label tag = new Label(text);
            tag.style.fontSize = 11;
            tag.style.color = color;
            tag.style.marginLeft = 4;
            tag.style.flexShrink = 0;
            tag.pickingMode = PickingMode.Ignore;
            return tag;
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        private static void SetBorderColor(VisualElement e, Color c)
        {
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        }

        private static void SetBorderWidth(VisualElement e, float w)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
        }
    }
}
