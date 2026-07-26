using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class IsometricColliderPainterWindow : EditorWindow
{
    private enum PaintMode
    {
        None,
        Wall,
        Floor
    }

    private const string MenuPath = "Tools/Jam Template/Isometric Collider Painter";

    [SerializeField]
    private Camera placementCamera;

    [SerializeField]
    private Transform colliderParent;

    [SerializeField]
    private float groundY;

    [SerializeField]
    [Min(0.01f)]
    private float wallHeight = 2.5f;

    [SerializeField]
    [Min(0.01f)]
    private float wallThickness = 0.25f;

    [SerializeField]
    [Min(0.01f)]
    private float floorThickness = 0.25f;

    [SerializeField]
    private float floorYRotation = 45f;

    [SerializeField]
    private int colliderLayer;

    [SerializeField]
    [Min(0.01f)]
    private float spriteSurfaceDepth = 0.25f;

    [SerializeField]
    [Min(0.01f)]
    private float itemColliderDepth = 0.5f;

    [SerializeField]
    [Min(0f)]
    private float itemColliderPadding = 0.1f;

    [SerializeField]
    private Vector3 groundedItemColliderSize = new Vector3(1f, 1f, 1f);

    private PaintMode paintMode;
    private bool hasFirstPoint;
    private Vector3 firstPoint;

    [MenuItem(MenuPath)]
    private static void Open()
    {
        GetWindow<IsometricColliderPainterWindow>("Collider Painter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        ResolvePlacementCamera();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Draws 3D BoxColliders on a horizontal gameplay plane. " +
            "Wall mode uses two endpoints. Floor mode uses two opposite corners. " +
            "Press Esc to cancel. Scene changes support Undo and are not saved automatically.",
            MessageType.Info);

        placementCamera = (Camera)EditorGUILayout.ObjectField(
            "Placement Camera",
            placementCamera,
            typeof(Camera),
            true);
        if (GUILayout.Button("Use Main Camera"))
        {
            ResolvePlacementCamera();
        }

        colliderParent = (Transform)EditorGUILayout.ObjectField(
            "Collider Parent",
            colliderParent,
            typeof(Transform),
            true);
        groundY = EditorGUILayout.FloatField("Ground World Y", groundY);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);
        floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);
        floorYRotation = EditorGUILayout.FloatField(
            "Floor Y Rotation",
            floorYRotation);
        colliderLayer = EditorGUILayout.LayerField("Collider Layer", colliderLayer);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera Occlusion Surface", EditorStyles.boldLabel);
        spriteSurfaceDepth = EditorGUILayout.FloatField(
            "Surface Depth",
            spriteSurfaceDepth);

        using (new EditorGUI.DisabledScope(
                   Selection.activeGameObject == null ||
                   Selection.activeGameObject.GetComponent<SpriteRenderer>() == null))
        {
            if (GUILayout.Button("Match Collider To Selected Sprite"))
            {
                CreateSpriteSurfaceCollider();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Item Interaction Collider", EditorStyles.boldLabel);
        itemColliderDepth = EditorGUILayout.FloatField(
            "Item Depth",
            itemColliderDepth);
        itemColliderPadding = EditorGUILayout.FloatField(
            "Item Padding",
            itemColliderPadding);
        groundedItemColliderSize = EditorGUILayout.Vector3Field(
            "Grounded Item Size",
            groundedItemColliderSize);

        using (new EditorGUI.DisabledScope(
                   Selection.activeGameObject == null ||
                   Selection.activeGameObject.GetComponent<SpriteRenderer>() == null))
        {
            if (GUILayout.Button("Match Item Collider To Selected Sprite"))
            {
                CreateOrUpdateItemCollider();
            }
        }

        using (new EditorGUI.DisabledScope(
                   placementCamera == null ||
                   Selection.activeGameObject == null ||
                   Selection.activeGameObject.GetComponent<SpriteRenderer>() == null))
        {
            if (GUILayout.Button("Project Item Collider To Ground"))
            {
                CreateOrUpdateGroundedItemCollider();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gameplay Collision", EditorStyles.boldLabel);

        DrawModeButton(PaintMode.Wall, "Draw Wall (Two Endpoints)");
        DrawModeButton(PaintMode.Floor, "Draw Floor (Two Corners)");

        using (new EditorGUI.DisabledScope(paintMode == PaintMode.None))
        {
            if (GUILayout.Button("Cancel Drawing"))
            {
                CancelDrawing();
            }
        }

        string status = paintMode == PaintMode.None
            ? "Idle"
            : hasFirstPoint
                ? $"{paintMode}: click the second point"
                : $"{paintMode}: click the first point";
        EditorGUILayout.LabelField("Status", status);
    }

    private void DrawModeButton(PaintMode mode, string label)
    {
        Color previousColor = GUI.backgroundColor;
        if (paintMode == mode)
        {
            GUI.backgroundColor = new Color(0.45f, 0.9f, 0.55f);
        }

        if (GUILayout.Button(label))
        {
            paintMode = mode;
            hasFirstPoint = false;
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = previousColor;
    }

    private void ResolvePlacementCamera()
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

    private void OnSceneGUI(SceneView sceneView)
    {
        if (paintMode == PaintMode.None)
        {
            return;
        }

        Event current = Event.current;
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            CancelDrawing();
            current.Use();
            return;
        }

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        if (!groundPlane.Raycast(mouseRay, out float distance))
        {
            return;
        }

        Vector3 mousePoint = mouseRay.GetPoint(distance);
        Handles.color = paintMode == PaintMode.Wall
            ? new Color(1f, 0.55f, 0.1f)
            : new Color(0.1f, 0.85f, 1f);
        Handles.DrawWireDisc(mousePoint, Vector3.up, 0.12f);

        if (hasFirstPoint)
        {
            if (paintMode == PaintMode.Wall)
            {
                Handles.DrawAAPolyLine(4f, firstPoint, mousePoint);
            }
            else
            {
                DrawFloorPreview(firstPoint, mousePoint, floorYRotation);
            }
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (current.type != EventType.MouseDown ||
            current.button != 0 ||
            current.alt)
        {
            sceneView.Repaint();
            return;
        }

        if (!hasFirstPoint)
        {
            firstPoint = mousePoint;
            hasFirstPoint = true;
        }
        else
        {
            if (paintMode == PaintMode.Wall)
            {
                CreateWall(firstPoint, mousePoint);
            }
            else
            {
                CreateFloor(firstPoint, mousePoint);
            }

            hasFirstPoint = false;
        }

        current.Use();
        Repaint();
    }

    private void CreateWall(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        float length = direction.magnitude;
        if (length < 0.05f)
        {
            Debug.LogWarning("Collider Painter skipped a wall shorter than 0.05 units.");
            return;
        }

        GameObject wall = CreateColliderObject("WallCollider");
        Transform wallTransform = wall.transform;
        wallTransform.position =
            (start + end) * 0.5f + Vector3.up * (wallHeight * 0.5f);
        wallTransform.rotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);

        BoxCollider boxCollider = wall.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(wallThickness, wallHeight, length);
        FinishCreation(wall);
    }

    private void CreateFloor(Vector3 first, Vector3 second)
    {
        Quaternion rotation = Quaternion.Euler(0f, floorYRotation, 0f);
        Vector3 localDiagonal = Quaternion.Inverse(rotation) * (second - first);
        float width = Mathf.Abs(localDiagonal.x);
        float depth = Mathf.Abs(localDiagonal.z);
        if (width < 0.05f || depth < 0.05f)
        {
            Debug.LogWarning(
                "Collider Painter skipped a floor narrower than 0.05 units.");
            return;
        }

        GameObject floor = CreateColliderObject("FloorCollider");
        Transform floorTransform = floor.transform;
        floorTransform.position = new Vector3(
            (first.x + second.x) * 0.5f,
            groundY - floorThickness * 0.5f,
            (first.z + second.z) * 0.5f);
        floorTransform.rotation = rotation;

        BoxCollider boxCollider = floor.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(width, floorThickness, depth);
        FinishCreation(floor);
    }

    private GameObject CreateColliderObject(string baseName)
    {
        GameObject colliderObject = new GameObject(
            GameObjectUtility.GetUniqueNameForSibling(
                colliderParent,
                baseName));
        Undo.RegisterCreatedObjectUndo(colliderObject, $"Create {baseName}");
        colliderObject.layer = colliderLayer;

        if (colliderParent != null)
        {
            Undo.SetTransformParent(
                colliderObject.transform,
                colliderParent,
                $"Parent {baseName}");
        }

        return colliderObject;
    }

    private void CreateSpriteSurfaceCollider()
    {
        SpriteRenderer spriteRenderer =
            Selection.activeGameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning(
                "Collider Painter requires a selected SpriteRenderer with a sprite.");
            return;
        }

        Transform spriteTransform = spriteRenderer.transform;
        GameObject surface = new GameObject(
            GameObjectUtility.GetUniqueNameForSibling(
                spriteTransform,
                "OcclusionSurface"));
        Undo.RegisterCreatedObjectUndo(
            surface,
            "Create Sprite Occlusion Surface");
        surface.layer = colliderLayer;
        Undo.SetTransformParent(
            surface.transform,
            spriteTransform,
            "Parent Sprite Occlusion Surface");

        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = Vector3.one;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        BoxCollider boxCollider = surface.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(
            spriteBounds.center.x,
            spriteBounds.center.y,
            0f);
        boxCollider.size = new Vector3(
            spriteBounds.size.x,
            spriteBounds.size.y,
            spriteSurfaceDepth);
        boxCollider.isTrigger = true;

        FinishCreation(surface);
    }

    private void CreateOrUpdateItemCollider()
    {
        SpriteRenderer spriteRenderer =
            Selection.activeGameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning(
                "Collider Painter requires a selected SpriteRenderer with a sprite.");
            return;
        }

        Transform spriteTransform = spriteRenderer.transform;
        Transform existing = spriteTransform.Find("ItemInteractionCollider");
        GameObject colliderObject;
        BoxCollider boxCollider;

        if (existing == null)
        {
            colliderObject = new GameObject("ItemInteractionCollider");
            Undo.RegisterCreatedObjectUndo(
                colliderObject,
                "Create Item Interaction Collider");
            Undo.SetTransformParent(
                colliderObject.transform,
                spriteTransform,
                "Parent Item Interaction Collider");
            boxCollider = colliderObject.AddComponent<BoxCollider>();
        }
        else
        {
            colliderObject = existing.gameObject;
            boxCollider = colliderObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider>(colliderObject);
            }

            Undo.RecordObject(
                colliderObject.transform,
                "Update Item Interaction Collider");
            Undo.RecordObject(
                boxCollider,
                "Update Item Interaction Collider");
        }

        colliderObject.layer = colliderLayer;
        Transform colliderTransform = colliderObject.transform;
        colliderTransform.localPosition = Vector3.zero;
        colliderTransform.localRotation = Quaternion.identity;
        colliderTransform.localScale = Vector3.one;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        Vector2 visualSize = spriteRenderer.drawMode == SpriteDrawMode.Simple
            ? new Vector2(spriteBounds.size.x, spriteBounds.size.y)
            : spriteRenderer.size;
        boxCollider.center = new Vector3(
            spriteBounds.center.x,
            spriteBounds.center.y,
            0f);
        boxCollider.size = new Vector3(
            Mathf.Max(0.01f, visualSize.x + itemColliderPadding * 2f),
            Mathf.Max(0.01f, visualSize.y + itemColliderPadding * 2f),
            itemColliderDepth);
        boxCollider.isTrigger = true;

        WorldStoryInteractable owner =
            spriteRenderer.GetComponentInParent<WorldStoryInteractable>();
        if (owner != null)
        {
            RegisterItemCollider(owner, boxCollider);
        }
        else
        {
            Debug.LogWarning(
                $"'{spriteRenderer.name}' has no WorldStoryInteractable in its parents. " +
                "The collider was created, but it was not registered as an item collider.",
                spriteRenderer);
        }

        FinishCreation(colliderObject);
    }

    private static void RegisterItemCollider(
        WorldStoryInteractable owner,
        BoxCollider itemCollider)
    {
        var serializedOwner = new SerializedObject(owner);
        SerializedProperty colliders =
            serializedOwner.FindProperty("interactionColliders");
        if (colliders == null)
        {
            Debug.LogWarning(
                $"Could not find Interaction Colliders on '{owner.name}'.",
                owner);
            return;
        }

        for (int i = 0; i < colliders.arraySize; i++)
        {
            if (colliders.GetArrayElementAtIndex(i).objectReferenceValue ==
                itemCollider)
            {
                return;
            }
        }

        Undo.RecordObject(owner, "Register Item Interaction Collider");
        int newIndex = colliders.arraySize;
        colliders.InsertArrayElementAtIndex(newIndex);
        colliders.GetArrayElementAtIndex(newIndex).objectReferenceValue =
            itemCollider;
        serializedOwner.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
    }

    private void CreateOrUpdateGroundedItemCollider()
    {
        SpriteRenderer spriteRenderer =
            Selection.activeGameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null ||
            spriteRenderer.sprite == null ||
            placementCamera == null)
        {
            Debug.LogWarning(
                "Collider Painter requires a selected SpriteRenderer and Placement Camera.");
            return;
        }

        WorldStoryInteractable owner =
            spriteRenderer.GetComponentInParent<WorldStoryInteractable>();
        if (owner == null)
        {
            Debug.LogWarning(
                $"'{spriteRenderer.name}' has no WorldStoryInteractable in its parents.",
                spriteRenderer);
            return;
        }

        Vector3 spriteCenter = spriteRenderer.transform.TransformPoint(
            spriteRenderer.sprite.bounds.center);
        Vector3 screenPoint = placementCamera.WorldToScreenPoint(spriteCenter);
        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, groundY, 0f));
        Ray cameraRay = placementCamera.ScreenPointToRay(screenPoint);
        if (screenPoint.z <= 0f ||
            !groundPlane.Raycast(cameraRay, out float distance))
        {
            Debug.LogWarning(
                $"Could not project '{spriteRenderer.name}' onto Ground World Y {groundY}.",
                spriteRenderer);
            return;
        }

        Vector3 groundPoint = cameraRay.GetPoint(distance);
        Transform existing = owner.transform.Find("ItemGroundInteractionCollider");
        GameObject colliderObject;
        BoxCollider boxCollider;

        if (existing == null)
        {
            colliderObject = new GameObject("ItemGroundInteractionCollider");
            Undo.RegisterCreatedObjectUndo(
                colliderObject,
                "Create Grounded Item Interaction Collider");
            Undo.SetTransformParent(
                colliderObject.transform,
                owner.transform,
                "Parent Grounded Item Interaction Collider");
            boxCollider = colliderObject.AddComponent<BoxCollider>();
        }
        else
        {
            colliderObject = existing.gameObject;
            boxCollider = colliderObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider>(colliderObject);
            }

            Undo.RecordObject(
                colliderObject.transform,
                "Update Grounded Item Interaction Collider");
            Undo.RecordObject(
                boxCollider,
                "Update Grounded Item Interaction Collider");
        }

        Vector3 safeSize = new Vector3(
            Mathf.Max(0.05f, groundedItemColliderSize.x),
            Mathf.Max(0.05f, groundedItemColliderSize.y),
            Mathf.Max(0.05f, groundedItemColliderSize.z));
        colliderObject.layer = colliderLayer;
        colliderObject.transform.position =
            groundPoint + Vector3.up * (safeSize.y * 0.5f);
        colliderObject.transform.rotation = Quaternion.identity;
        SetWorldScaleOne(colliderObject.transform);

        boxCollider.center = Vector3.zero;
        boxCollider.size = safeSize;
        boxCollider.isTrigger = true;

        Transform spritePlaneCollider =
            spriteRenderer.transform.Find("ItemInteractionCollider");
        if (spritePlaneCollider != null)
        {
            BoxCollider oldCollider =
                spritePlaneCollider.GetComponent<BoxCollider>();
            if (oldCollider != null && oldCollider.enabled)
            {
                Undo.RecordObject(
                    oldCollider,
                    "Disable Sprite Plane Item Collider");
                oldCollider.enabled = false;
                EditorUtility.SetDirty(oldCollider);
            }
        }

        RegisterItemCollider(owner, boxCollider);
        RegisterInteractionPoint(owner, colliderObject.transform);
        FinishCreation(colliderObject);
    }

    private static void RegisterInteractionPoint(
        WorldStoryInteractable owner,
        Transform interactionPoint)
    {
        var serializedOwner = new SerializedObject(owner);
        SerializedProperty point =
            serializedOwner.FindProperty("interactionPoint");
        if (point == null)
        {
            Debug.LogWarning(
                $"Could not find Interaction Point on '{owner.name}'.",
                owner);
            return;
        }

        Undo.RecordObject(owner, "Set Item Interaction Point");
        point.objectReferenceValue = interactionPoint;
        serializedOwner.ApplyModifiedProperties();
        EditorUtility.SetDirty(owner);
    }

    private static void SetWorldScaleOne(Transform target)
    {
        Vector3 parentScale = target.parent != null
            ? target.parent.lossyScale
            : Vector3.one;
        target.localScale = new Vector3(
            SafeReciprocal(parentScale.x),
            SafeReciprocal(parentScale.y),
            SafeReciprocal(parentScale.z));
    }

    private static float SafeReciprocal(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }

    private static void FinishCreation(GameObject colliderObject)
    {
        Selection.activeGameObject = colliderObject;
        EditorSceneManager.MarkSceneDirty(colliderObject.scene);
        Debug.Log(
            $"Collider Painter created '{colliderObject.name}'. " +
            "The scene was not saved automatically.",
            colliderObject);
    }

    private static void DrawFloorPreview(
        Vector3 first,
        Vector3 second,
        float yRotation)
    {
        Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f);
        Vector3 center = (first + second) * 0.5f;
        Vector3 halfDiagonal =
            Quaternion.Inverse(rotation) * (second - first) * 0.5f;
        float halfWidth = Mathf.Abs(halfDiagonal.x);
        float halfDepth = Mathf.Abs(halfDiagonal.z);

        Vector3 a = center + rotation * new Vector3(-halfWidth, 0f, -halfDepth);
        Vector3 b = center + rotation * new Vector3(halfWidth, 0f, -halfDepth);
        Vector3 c = center + rotation * new Vector3(halfWidth, 0f, halfDepth);
        Vector3 d = center + rotation * new Vector3(-halfWidth, 0f, halfDepth);
        Handles.DrawAAPolyLine(4f, a, b, c, d, a);
    }

    private void CancelDrawing()
    {
        paintMode = PaintMode.None;
        hasFirstPoint = false;
        SceneView.RepaintAll();
        Repaint();
    }
}
