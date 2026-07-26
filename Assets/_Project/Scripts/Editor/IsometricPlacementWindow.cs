using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class IsometricPlacementWindow : EditorWindow
{
    private const string MenuPath = "Tools/Jam Template/Isometric Placement";
    private const float SceneViewPivotDistance = 10f;

    [SerializeField]
    private Camera placementCamera;

    [SerializeField]
    private float targetWorldY;

    [SerializeField]
    private int sortingOrder;

    [MenuItem(MenuPath)]
    private static void Open()
    {
        GetWindow<IsometricPlacementWindow>("Isometric Placement");
    }

    private void OnEnable()
    {
        ResolveMainCamera();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Normalizing moves selected objects onto a horizontal world plane " +
            "while preserving their Game View screen positions. Changes support Undo " +
            "and are not saved automatically.",
            MessageType.Info);

        placementCamera = (Camera)EditorGUILayout.ObjectField(
            "Placement Camera",
            placementCamera,
            typeof(Camera),
            true);

        if (GUILayout.Button("Use Main Camera"))
        {
            ResolveMainCamera();
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(placementCamera == null))
        {
            if (GUILayout.Button("Align Scene View To Placement Camera"))
            {
                AlignSceneView();
            }
        }

        EditorGUILayout.Space();
        targetWorldY = EditorGUILayout.FloatField("Target World Y", targetWorldY);

        using (new EditorGUI.DisabledScope(
                   placementCamera == null || Selection.transforms.Length == 0))
        {
            if (GUILayout.Button("Normalize Selected Y (Preserve Game View)"))
            {
                NormalizeSelectedTransforms();
            }
        }

        EditorGUILayout.Space();
        sortingOrder = EditorGUILayout.IntField("Sorting Order", sortingOrder);

        using (new EditorGUI.DisabledScope(Selection.transforms.Length == 0))
        {
            if (GUILayout.Button("Apply Order To Selected SpriteRenderers"))
            {
                ApplySortingOrder();
            }
        }
    }

    private void ResolveMainCamera()
    {
        placementCamera = Camera.main;
        if (placementCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>(true);
            if (cameras.Length == 1)
            {
                placementCamera = cameras[0];
            }
        }

        Repaint();
    }

    private void AlignSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || placementCamera == null)
        {
            Debug.LogWarning(
                "Isometric Placement requires an open Scene View and a placement camera.");
            return;
        }

        Transform cameraTransform = placementCamera.transform;
        sceneView.pivot =
            cameraTransform.position +
            cameraTransform.forward * SceneViewPivotDistance;
        sceneView.rotation = cameraTransform.rotation;
        sceneView.orthographic = placementCamera.orthographic;

        if (placementCamera.orthographic)
        {
            sceneView.size = placementCamera.orthographicSize;
        }

        sceneView.Repaint();
    }

    private void NormalizeSelectedTransforms()
    {
        if (placementCamera == null)
        {
            return;
        }

        Transform[] targets = GetTopLevelSelection();
        if (targets.Length == 0)
        {
            return;
        }

        Plane targetPlane = new Plane(
            Vector3.up,
            new Vector3(0f, targetWorldY, 0f));
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Normalize Isometric Visual Y");

        int movedCount = 0;
        int skippedCount = 0;
        var dirtyScenes = new HashSet<Scene>();

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            Vector3 screenPoint = placementCamera.WorldToScreenPoint(target.position);
            if (screenPoint.z <= 0f)
            {
                skippedCount++;
                continue;
            }

            Ray cameraRay = placementCamera.ScreenPointToRay(screenPoint);
            if (!targetPlane.Raycast(cameraRay, out float distance))
            {
                skippedCount++;
                continue;
            }

            Undo.RecordObject(target, "Normalize Isometric Visual Y");
            target.position = cameraRay.GetPoint(distance);
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            movedCount++;

            if (target.gameObject.scene.IsValid())
            {
                dirtyScenes.Add(target.gameObject.scene);
            }
        }

        foreach (Scene scene in dirtyScenes)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log(
            $"Isometric Placement normalized {movedCount} object(s) to Y={targetWorldY}. " +
            $"Skipped {skippedCount}. Scene changes were not saved automatically.");
    }

    private void ApplySortingOrder()
    {
        Transform[] selection = Selection.transforms;
        var renderers = new HashSet<SpriteRenderer>();

        for (int i = 0; i < selection.Length; i++)
        {
            SpriteRenderer[] children =
                selection[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < children.Length; j++)
            {
                renderers.Add(children[j]);
            }
        }

        if (renderers.Count == 0)
        {
            Debug.LogWarning(
                "Isometric Placement found no SpriteRenderer in the current selection.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set Sprite Sorting Order");
        var dirtyScenes = new HashSet<Scene>();

        foreach (SpriteRenderer renderer in renderers)
        {
            Undo.RecordObject(renderer, "Set Sprite Sorting Order");
            renderer.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

            if (renderer.gameObject.scene.IsValid())
            {
                dirtyScenes.Add(renderer.gameObject.scene);
            }
        }

        foreach (Scene scene in dirtyScenes)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log(
            $"Isometric Placement set sorting order {sortingOrder} on " +
            $"{renderers.Count} SpriteRenderer(s). Scene changes were not saved automatically.");
    }

    private static Transform[] GetTopLevelSelection()
    {
        Transform[] selection = Selection.transforms;
        var selectedSet = new HashSet<Transform>(selection);
        var targets = new List<Transform>(selection.Length);

        for (int i = 0; i < selection.Length; i++)
        {
            Transform candidate = selection[i];
            Transform parent = candidate.parent;
            bool hasSelectedAncestor = false;

            while (parent != null)
            {
                if (selectedSet.Contains(parent))
                {
                    hasSelectedAncestor = true;
                    break;
                }

                parent = parent.parent;
            }

            if (!hasSelectedAncestor)
            {
                targets.Add(candidate);
            }
        }

        return targets.ToArray();
    }
}
