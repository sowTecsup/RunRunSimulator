using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class AbilityCardVisuals
    {
        public static VisualElement BuildAbilityMiniGrid(CombatAbilitySO ability)
        {
            Color empty = Hex("#FFFFFF14");
            Color[,] fill = new Color[5, 5];
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    fill[x, y] = empty;

            bool reinforceAnchor = false;

            if (ability.Type == AbilityType.Movement)
            {
                fill[2, 2] = Hex("#59C96A");
            }
            else if (ability.Targeting == TargetingMode.DirectionalTemplate)
            {
                if (ability.TemplateOffsets != null)
                {
                    for (int i = 0; i < ability.TemplateOffsets.Length; i++)
                    {
                        Vector2Int off = ability.TemplateOffsets[i];
                        int gx = 2 + off.x;
                        int gy = 2 + off.y;
                        if (gx >= 0 && gx < 5 && gy >= 0 && gy < 5) fill[gx, gy] = Hex("#FFD34D");
                    }
                }

                if (ability.Landing == LandingKind.BehindAnchor) fill[1, 2] = Hex("#59C96A");
                else if (ability.Landing == LandingKind.Stay) reinforceAnchor = true;
            }
            else if (ability.Targeting == TargetingMode.AirborneEnemy)
            {
                fill[2, 2] = Hex("#FF6B5E");
                Vector2Int[] cardinals = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                for (int d = 0; d < cardinals.Length; d++)
                    for (int dist = 1; dist <= 2; dist++)
                    {
                        int gx = 2 + cardinals[d].x * dist;
                        int gy = 2 + cardinals[d].y * dist;
                        if (gx >= 0 && gx < 5 && gy >= 0 && gy < 5) fill[gx, gy] = Hex("#FF6B5E40");
                    }
            }

            VisualElement grid = new VisualElement();
            grid.pickingMode = PickingMode.Ignore;
            grid.style.marginLeft = 8;
            grid.style.marginRight = 6;

            for (int y = 0; y < 5; y++)
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.pickingMode = PickingMode.Ignore;
                for (int x = 0; x < 5; x++)
                {
                    VisualElement cell = new VisualElement();
                    cell.style.width = 7;
                    cell.style.height = 7;
                    cell.style.marginLeft = 1;
                    cell.style.marginRight = 1;
                    cell.style.marginTop = 1;
                    cell.style.marginBottom = 1;
                    cell.style.backgroundColor = fill[x, y];
                    cell.pickingMode = PickingMode.Ignore;
                    if (reinforceAnchor && x == 2 && y == 2)
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
