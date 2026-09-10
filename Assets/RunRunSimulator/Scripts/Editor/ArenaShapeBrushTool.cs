using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;
namespace MoriMonchiSimulator
{

[EditorTool("Pincel de sala", typeof(ArenaShapeBrush))]
public class ArenaShapeBrushTool : EditorTool
{
    private Mesh previewMesh;
    private Material previewMaterial;
    private int cachedVersion = -1;
    private int cachedMaskLength = -1;
    private readonly List<Vector3> strokePoints = new();

    public override GUIContent toolbarIcon
    {
        get
        {
            var content = new GUIContent("Pincel", "Pintar la sala (Shift borra, [ ] radio)");
            var icon = EditorGUIUtility.IconContent("d_editicon.sml");
            if (icon != null && icon.image != null)
                content.image = icon.image;
            return content;
        }
    }

    public override void OnActivated()
    {
        base.OnActivated();
        cachedVersion = -1;
        cachedMaskLength = -1;
        strokePoints.Clear();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        var brush = target as ArenaShapeBrush;
        if (brush == null) return;

        var shape = brush.GetComponent<ArenaShape>();
        if (shape == null) return;

        var e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(id);

        var plane = new Plane(Vector3.up, new Vector3(0f, shape.Center.y, 0f));
        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!plane.Raycast(ray, out float distance)) return;
        var hit = ray.GetPoint(distance);

        DrawBrushCursor(brush, hit, e.shift);
        DrawStrokePreview(brush, e.shift);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && !e.alt)
                {
                    Undo.RecordObject(brush, "Pintar sala");
                    strokePoints.Clear();
                    AppendStrokePoint(brush, hit);
                    brush.PaintAt(hit, e.shift);
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    AppendStrokePoint(brush, hit);
                    brush.PaintAt(hit, e.shift);
                    e.Use();
                }
                window.Repaint();
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    brush.Apply();
                    strokePoints.Clear();
                    EditorUtility.SetDirty(brush);
                    e.Use();
                }
                break;

            case EventType.KeyDown:
                if (e.keyCode == KeyCode.RightBracket)
                {
                    brush.BrushRadius += 0.5f;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.LeftBracket)
                {
                    brush.BrushRadius = Mathf.Max(0.5f, brush.BrushRadius - 0.5f);
                    e.Use();
                }
                break;

            case EventType.MouseMove:
                window.Repaint();
                break;

            case EventType.Repaint:
                RebuildPreviewMeshIfNeeded(brush, shape);
                if (previewMesh != null && previewMesh.vertexCount > 0)
                {
                    GetPreviewMaterial().SetPass(0);
                    Graphics.DrawMeshNow(previewMesh, Matrix4x4.identity);
                }
                break;
        }

        DrawHelpBox(brush);
    }

    private void AppendStrokePoint(ArenaShapeBrush brush, Vector3 point)
    {
        if (strokePoints.Count >= 200) return;
        if (strokePoints.Count > 0 && Vector3.Distance(strokePoints[strokePoints.Count - 1], point) < brush.BrushRadius * 0.35f)
            return;
        strokePoints.Add(point);
    }

    private static void DrawBrushCursor(ArenaShapeBrush brush, Vector3 hit, bool erase)
    {
        var color = erase ? new Color(1f, 0.35f, 0.35f) : new Color(0.35f, 0.8f, 1f);
        Handles.color = color;
        Handles.DrawWireDisc(hit, Vector3.up, brush.BrushRadius);
        Handles.color = new Color(color.r, color.g, color.b, 0.12f);
        Handles.DrawSolidDisc(hit, Vector3.up, brush.BrushRadius);
    }

    private void DrawStrokePreview(ArenaShapeBrush brush, bool erase)
    {
        if (strokePoints.Count == 0) return;

        Handles.color = erase ? new Color(1f, 0.35f, 0.35f, 0.15f) : new Color(0.35f, 0.8f, 1f, 0.15f);
        for (int i = 0; i < strokePoints.Count; i++)
            Handles.DrawSolidDisc(strokePoints[i], Vector3.up, brush.BrushRadius);
    }

    private static void DrawHelpBox(ArenaShapeBrush brush)
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10f, 10f, 360f, 24f));
        GUILayout.Box($"Pincel de sala · arrastrar pinta · Shift borra · [ ] radio ({brush.BrushRadius:0.0} m) · soltar aplica");
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void RebuildPreviewMeshIfNeeded(ArenaShapeBrush brush, ArenaShape shape)
    {
        var mask = brush.Mask;
        if (mask == null) return;

        if (previewMesh != null && cachedVersion == brush.Version && cachedMaskLength == mask.Length)
            return;

        cachedVersion = brush.Version;
        cachedMaskLength = mask.Length;

        BuildPreviewMesh(brush, shape, mask);
    }

    private void BuildPreviewMesh(ArenaShapeBrush brush, ArenaShape shape, byte[] mask)
    {
        if (previewMesh == null)
            previewMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        else
            previewMesh.Clear();

        int size = brush.Size;
        float cell = brush.Cell;
        var origin = brush.Origin;
        float y = shape.Center.y + 0.06f;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        for (int j = 0; j < size; j++)
        {
            for (int i = 0; i < size; i++)
            {
                if (mask[j * size + i] == 0) continue;

                float x0 = origin.x + i * cell;
                float x1 = x0 + cell;
                float z0 = origin.y + j * cell;
                float z1 = z0 + cell;

                int baseIndex = vertices.Count;
                vertices.Add(new Vector3(x0, y, z0));
                vertices.Add(new Vector3(x0, y, z1));
                vertices.Add(new Vector3(x1, y, z1));
                vertices.Add(new Vector3(x1, y, z0));

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
        }

        previewMesh.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        previewMesh.SetVertices(vertices);
        previewMesh.SetTriangles(triangles, 0);
    }

    private Material GetPreviewMaterial()
    {
        if (previewMaterial != null) return previewMaterial;

        var shader = Shader.Find("Hidden/Internal-Colored");
        previewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        previewMaterial.color = new Color(0.3f, 0.8f, 1f, 0.18f);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        return previewMaterial;
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (previewMesh != null)
        {
            DestroyImmediate(previewMesh);
            previewMesh = null;
        }

        if (previewMaterial != null)
        {
            DestroyImmediate(previewMaterial);
            previewMaterial = null;
        }

        cachedVersion = -1;
        cachedMaskLength = -1;
    }
}
}
