using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[EditorTool("Place Objects Tool")]
public class PlaceObjectsTool : EditorTool
{
    Texture2D _toolIcon;

    GUIContent _iconContent;

    VisualElement _toolRootElement;
    Toggle _useCurrentSelection;
    ObjectField _prefabObjectField;

    bool _receivedClickDownEvent;
    bool _receivedClickUpEvent;

    public override GUIContent toolbarIcon => _iconContent;

    public override void OnActivated()
    {
        _iconContent = new GUIContent
        {
            image = _toolIcon,
            text = "Place Objects Tool",
            tooltip = "Place Objects Tool"
        };

        SceneView.beforeSceneGui += BeforeSceneGUI;

        //Create the UI
        _toolRootElement = new VisualElement();
        _toolRootElement.style.width = 200;
        var backgroundColor = EditorGUIUtility.isProSkin
            ? new Color(0.21f, 0.21f, 0.21f, 0.8f)
            : new Color(0.8f, 0.8f, 0.8f, 0.8f);
        _toolRootElement.style.backgroundColor = backgroundColor;
        _toolRootElement.style.marginLeft = 10f;
        _toolRootElement.style.marginBottom = 10f;
        _toolRootElement.style.paddingTop = 5f;
        _toolRootElement.style.paddingRight = 5f;
        _toolRootElement.style.paddingLeft = 5f;
        _toolRootElement.style.paddingBottom = 5f;
        var titleLabel = new Label("Place Objects Tool");
        titleLabel.style.unityTextAlign = TextAnchor.UpperCenter;

        _prefabObjectField = new ObjectField {allowSceneObjects = true, objectType = typeof(GameObject)};
        _useCurrentSelection = new Toggle {label = "Use Current Selection"};

        _toolRootElement.Add(titleLabel);
        _toolRootElement.Add(_useCurrentSelection);
        _toolRootElement.Add(_prefabObjectField);

        var sv = SceneView.lastActiveSceneView;
        sv.rootVisualElement.Add(_toolRootElement);
        sv.rootVisualElement.style.flexDirection = FlexDirection.ColumnReverse;
    }

    public override void OnWillBeDeactivated()
    {
        SceneView.beforeSceneGui -= BeforeSceneGUI;
        _toolRootElement?.RemoveFromHierarchy();
    }

    void BeforeSceneGUI(SceneView sceneView)
    {
        if (ToolManager.IsActiveTool(this))
        {
            if (_useCurrentSelection.value && Selection.activeGameObject == null)
            {
                _receivedClickDownEvent = false;
                _receivedClickUpEvent = false;
                return;
            }

            if (!_useCurrentSelection.value && _prefabObjectField?.value == null)
            {
                _receivedClickDownEvent = false;
                _receivedClickUpEvent = false;
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _receivedClickDownEvent = true;
                Event.current.Use();
            }

            if (_receivedClickDownEvent && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                _receivedClickDownEvent = false;
                _receivedClickUpEvent = true;
                Event.current.Use();
            }
        }
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView))
            return;


        if (ToolManager.IsActiveTool(this))
        {
            if (_useCurrentSelection.value && Selection.activeGameObject == null)
                return;

            if (_useCurrentSelection.value)
                _prefabObjectField.value = Selection.activeGameObject;

            if (!_useCurrentSelection.value && _prefabObjectField?.value == null)
                return;

            Handles.DrawWireDisc(GetCurrentMousePositionInScene(), Vector3.up, 1f);
            if (_receivedClickUpEvent)
            {
                var newObject = _useCurrentSelection.value ? Selection.activeGameObject : _prefabObjectField.value;

                PrefabUtility.IsPartOfPrefabInstance(newObject);
                var newPrefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(newObject);
                if (newPrefabInstance == null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(newObject))
                    {
                        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(newObject);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        newPrefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
                    }
                    else
                    {
                        newPrefabInstance = Instantiate((GameObject) newObject);
                    }
                }

                newPrefabInstance.transform.position = GetCurrentMousePositionInScene();

                Event.current.Use();
                Undo.RegisterCreatedObjectUndo(newPrefabInstance, "Place new object");
                _receivedClickUpEvent = false;
            }

            window.Repaint();
        }
    }

    Vector3 GetCurrentMousePositionInScene()
    {
        Vector3 mousePosition = Event.current.mousePosition;
        var placeObject = HandleUtility.PlaceObject(mousePosition, out var newPosition, out var normal);
        return placeObject ? newPosition : HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
    }
}