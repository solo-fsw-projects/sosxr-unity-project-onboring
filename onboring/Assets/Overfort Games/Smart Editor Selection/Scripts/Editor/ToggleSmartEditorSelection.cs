using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace OverfortGames.SmartEditorSelection
{
    [EditorToolbarElement(id, typeof(SceneView))]
    public class ToggleSmartEditorSelection : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public enum MultiSelectionKeyType
        {
            Shift,
            Ctrl
        }


        private readonly Texture2D logoDeactivated;
        private readonly Texture2D logoActivated;

        private List<GameObject> currentSelectedObjects = new();

        private readonly List<Renderer> renderOutlineObjects = new();

        private readonly Dictionary<Camera, float> cameraFarPlaneDictionary = new();

        private readonly Dictionary<GameObject, bool> terrainsPickableDictionary = new();

        private bool lastDrawGizmos = false;

        private Tool previousTool = Tool.Move;
        private GameObject[] filter;
        private bool wantsToSelect;
        private Event currentEvent;


        private ToggleSmartEditorSelection()
        {
            _instance = this;

            {
                var guids = AssetDatabase.FindAssets("smarteditorselection_icon t:Texture2D");

                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    logoDeactivated = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                else
                {
                    Debug.LogError("Asset 'icon' not found");
                }
            }

            {
                var guids = AssetDatabase.FindAssets("smarteditorselection_icon_activated t:Texture2D");

                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    logoActivated = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                else
                {
                    Debug.LogError("Asset 'icon_activated' not found");
                }
            }

            text = "";
            tooltip = "";
            icon = logoDeactivated;

            this.RegisterValueChangedCallback(ToggleChanged);
            SceneView.duringSceneGui += DrawSelectionBoundingBox;
            EditorApplication.update += Update;
            dropdownClicked += DropdownClicked;

            value = Active.Value;

            if (value)
            {
                DisableTransformToolsGizmos();
                DisableCamerasFarPlane();
                DisableTerrainSelection();
            }

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.postprocessModifications += UndoPostProcessModifications;
            UpdateFilter();
            UpdateCameraFarPlainDictionary();
            Selection.selectionChanged += SelectionChanged;
        }


        public static EditorPrefsVariable<float> OutlineSelectionColorFillAmount { get; set; } = new("key_outlineSelectionColorFillAmount", .5f);
        public static EditorPrefsVariable<float> OutlineMultiSelectionColorFillAmount { get; set; } = new("key_outlineMultiSelectionColorFillAmount", .5f);
        public static EditorPrefsVariable<bool> ForceDisableGizmos { get; set; } = new("key_forceDisableGizmos", true);
        public static EditorPrefsVariable<bool> ShowSelectionLabel { get; set; } = new("key_showSelectionLabel", true);
        public static EditorPrefsVariable<bool> ShowMultiSelectionLabel { get; set; } = new("key_showMultiSelectionLabel", true);
        public static EditorPrefsVariable<bool> Active { get; set; } = new("key_active", false);
        public static EditorPrefsVariable<MultiSelectionKeyType> MultiSelectionKey { get; set; } = new("key_multiSelectionKey", MultiSelectionKeyType.Shift);
        public static EditorPrefsVariable<bool> InvertMultiSelection { get; set; } = new("key_invertMultiSelection", false);

        public const string id = "smart-selection";

        private static Renderer hoverRenderer;

        public static EditorPrefsVariable<Color> OutlineSelectionColor = new("key_outlineSelectionColor", new Color(1, 0.8352941f, 0, 1));
        public static EditorPrefsVariable<Color> OutlineMultiSelectionColor = new("key_outlineMultiSelectionColor", new Color(1, 0.5843138f, 0, 1));

        private static ToggleSmartEditorSelection _instance;

        private static bool keyToggleValue;

        // This property is specified by IAccessContainerWindow and is used to access the Overlay's EditorWindow.
        public EditorWindow containerWindow { get; set; }


        ~ToggleSmartEditorSelection()
        {
            EditorApplication.update -= Update;
            dropdownClicked -= DropdownClicked;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Undo.postprocessModifications -= UndoPostProcessModifications;
            Selection.selectionChanged -= SelectionChanged;
        }


        private void Update()
        {
            if (Active.Value)
            {
                if (icon != logoActivated)
                {
                    icon = logoActivated;
                }
            }
            else
            {
                if (icon != logoDeactivated)
                {
                    icon = logoDeactivated;
                }
            }

            if (wantsToSelect)
            {
                Selection.objects = currentSelectedObjects.Select(x => x.gameObject).ToArray();
                wantsToSelect = false;
            }
        }


        private void DropdownClicked()
        {
            if (!(containerWindow is SceneView view))
            {
                return;
            }

            var w = PopupWindowBase.Show<SmartEditorSelectionSettingsWindow>(this, new Vector2(300, 205));
        }


        private void ToggleChanged(ChangeEvent<bool> evt)
        {
            Active.Value = evt.newValue;

            if (_instance.value)
            {
                _instance.DisableTransformToolsGizmos();
                _instance.DisableGizmos();
                _instance.DisableCamerasFarPlane();
                _instance.DisableTerrainSelection();

                UpdateRenderOutlineObjects();
                SyncCurrentSelectedObjectsWithSelectionObjects();
            }
            else
            {
                _instance.EnableTransformToolsGizmos();
                _instance.EnableCamerasFarPlane();
                _instance.EnableTerrainSelection();
                _instance.EnableGizmos();

                currentSelectedObjects.Clear();
                renderOutlineObjects.Clear();
            }
        }


        private void EnableGizmos()
        {
            SceneView.lastActiveSceneView.drawGizmos = lastDrawGizmos;
        }


        private void DisableGizmos()
        {
            lastDrawGizmos = SceneView.lastActiveSceneView.drawGizmos;

            if (ForceDisableGizmos.Value)
            {
                SceneView.lastActiveSceneView.drawGizmos = false;
            }
        }


        [ClutchShortcut("OverfortGames Smart Selection/Toggle Smart Selection", typeof(SceneView), KeyCode.C)]
        public static void ToggleSmartSelectionShortcutClutch()
        {
            keyToggleValue = !keyToggleValue;

            _instance.value = keyToggleValue;
        }


        [Shortcut("OverfortGames Smart Selection/Toggle Smart Selection Perma Toggle", typeof(SceneView), KeyCode.G)]
        public static void ToggleSmartSelectionShortcut()
        {
            _instance.value = !_instance.value;
        }


        private void DrawSelectionBoundingBox(SceneView view)
        {
            currentEvent = Event.current;

            if (!Active.Value)
            {
                hoverRenderer = null;

                return;
            }

            if (Event.current.type == EventType.Used)
            {
                wantsToSelect = true;
            }

            if (currentEvent.type == EventType.MouseMove)
            {
                hoverRenderer = null;
                GameObject[] ignore = null; // GameObject.FindObjectOfType<Canvas>().GetComponentsInChildren<RectTransform>().Select(x => x.gameObject).ToArray();

                var hoveredObject = HandleUtility.PickGameObject(currentEvent.mousePosition, false, ignore, filter);

                if (hoveredObject != null)
                {
                    hoverRenderer = hoveredObject.GetComponentInChildren<Renderer>();

                    if (hoverRenderer.GetComponent<LODGroup>())
                    {
                        hoverRenderer = hoverRenderer.GetComponentsInChildren<Renderer>().Where(x => x.isVisible).FirstOrDefault();
                    }
                    else if (hoverRenderer.transform.parent != null)
                    {
                        if (hoverRenderer.transform.parent.GetComponent<LODGroup>() != null)
                        {
                            hoverRenderer = hoverRenderer.transform.parent.GetComponentsInChildren<Renderer>().Where(x => x.isVisible).FirstOrDefault();
                        }
                    }
                }

                HandleUtility.Repaint();
            }

            if (hoverRenderer != null)
            {
                if (GUIUtility.hotControl == 0)
                {
                    HandlesInternal.DrawOutline(new[] {hoverRenderer}, OutlineSelectionColor.Value, OutlineSelectionColor.Value, OutlineSelectionColorFillAmount.Value);
                }

                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0) // Left mouse button down
                {
                    var objectToAdd = hoverRenderer.gameObject;

                    if (objectToAdd.GetComponent<LODGroup>() == null)
                    {
                        if (objectToAdd.transform.parent != null)
                        {
                            if (objectToAdd.transform.parent.GetComponent<LODGroup>() != null)
                            {
                                objectToAdd = objectToAdd.transform.parent.gameObject;
                            }
                        }
                    }

                    var doMultiSelection = false;

                    switch (MultiSelectionKey.Value)
                    {
                        case MultiSelectionKeyType.Shift:
                            if (currentEvent.shift)
                            {
                                doMultiSelection = true;
                            }

                            break;
                        case MultiSelectionKeyType.Ctrl:
                            if (currentEvent.command || currentEvent.control)
                            {
                                doMultiSelection = true;
                            }

                            break;
                    }

                    if (InvertMultiSelection.Value)
                    {
                        doMultiSelection = !doMultiSelection;
                    }

                    if (doMultiSelection)
                    {
                        if (currentSelectedObjects.Contains(objectToAdd) == false)
                        {
                            currentSelectedObjects.Add(objectToAdd);
                        }
                        else
                        {
                            currentSelectedObjects.Remove(objectToAdd);
                        }
                    }
                    else
                    {
                        currentSelectedObjects.Clear();
                        currentSelectedObjects.Add(objectToAdd);
                    }

                    Selection.objects = currentSelectedObjects.ToArray();
                }
            }

            DrawLabels();

            HandlesInternal.DrawOutline(renderOutlineObjects.ToArray(), OutlineMultiSelectionColor.Value, OutlineMultiSelectionColor.Value, OutlineMultiSelectionColorFillAmount.Value);
        }


        private void DrawLabels()
        {
            var hoverSelectedLabelRect = new Rect(50, 10, 0, 0);

            var labelStyle = new GUIStyle
            {
                normal = new GUIStyleState {textColor = Color.white, background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f))},
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            if (ShowMultiSelectionLabel.Value)
            {
                Handles.BeginGUI();

                for (var i = 0; i < currentSelectedObjects.Count; i++)
                {
                    var selectedObject = currentSelectedObjects[i];

                    var size = labelStyle.CalcSize(new GUIContent(selectedObject.name));

                    float padding = 10;
                    var labelRect = new Rect(50, 10 + i * (size.y + 20), size.x + padding, size.y + padding);

                    if (i == 0)
                    {
                        hoverSelectedLabelRect = labelRect;
                    }

                    GUI.Label(labelRect, selectedObject.name, labelStyle);
                }

                Handles.EndGUI();
            }

            if (ShowSelectionLabel.Value)
            {
                Handles.BeginGUI();

                if (hoverRenderer != null)
                {
                    var hoverSelectedLabelSize = labelStyle.CalcSize(new GUIContent(hoverRenderer.name));

                    float hoverSelectedLabelPadding = 10;
                    hoverSelectedLabelRect = new Rect(hoverSelectedLabelRect.xMax + 10, hoverSelectedLabelRect.yMin, hoverSelectedLabelSize.x + hoverSelectedLabelPadding, hoverSelectedLabelSize.y + hoverSelectedLabelPadding);

                    GUI.Label(hoverSelectedLabelRect, hoverRenderer.name, labelStyle);
                }


                Handles.EndGUI();
            }
        }


        private void DisableTransformToolsGizmos()
        {
            if (Tools.current == Tool.None)
            {
                return;
            }

            previousTool = Tools.current;

            Tools.current = Tool.None;
        }


        private void EnableTransformToolsGizmos()
        {
            Tools.current = previousTool;
        }


        private void DisableCamerasFarPlane()
        {
            foreach (var camera in cameraFarPlaneDictionary.Keys)
            {
                camera.farClipPlane = 0;
            }
        }


        private void EnableCamerasFarPlane()
        {
            foreach (var camera in cameraFarPlaneDictionary.Keys)
            {
                camera.farClipPlane = cameraFarPlaneDictionary[camera];
            }
        }


        private void DisableTerrainSelection()
        {
            foreach (var terrainsPickablePair in terrainsPickableDictionary)
            {
                SceneVisibilityManager.instance.DisablePicking(terrainsPickablePair.Key, true);
            }
        }


        private void EnableTerrainSelection()
        {
            foreach (var terrainsPickablePair in terrainsPickableDictionary)
            {
                // WIP Enable picking only on terrains that were already pickable
                SceneVisibilityManager.instance.EnablePicking(terrainsPickablePair.Key, true);
            }
        }


        private void OnHierarchyChanged()
        {
            UpdateFilter();
            UpdateCameraFarPlainDictionary();
        }


        private void SelectionChanged()
        {
            UpdateRenderOutlineObjects();
        }


        private void SyncCurrentSelectedObjectsWithSelectionObjects()
        {
            currentSelectedObjects.Clear();

            currentSelectedObjects = Selection.gameObjects.ToList();
        }


        private void UpdateRenderOutlineObjects()
        {
            renderOutlineObjects.Clear();

            foreach (var selectedObject in Selection.gameObjects)
            {
                var selectedObjectRenderer = selectedObject.GetComponent<Renderer>();

                if (selectedObjectRenderer != null)
                {
                    renderOutlineObjects.Add(selectedObjectRenderer);
                }

                if (selectedObject.GetComponent<LODGroup>())
                {
                    foreach (var renderer in selectedObject.GetComponentsInChildren<Renderer>())
                    {
                        if (renderOutlineObjects.Contains(renderer) == false)
                        {
                            renderOutlineObjects.Add(renderer);
                        }
                    }
                }
                else
                {
                    if (selectedObject.transform.parent != null)
                    {
                        if (selectedObject.transform.parent.GetComponent<LODGroup>() != null)
                        {
                            foreach (var renderer in selectedObject.transform.parent.GetComponentsInChildren<Renderer>())
                            {
                                if (renderOutlineObjects.Contains(renderer) == false)
                                {
                                    renderOutlineObjects.Add(renderer);
                                }
                            }
                        }
                    }
                }
            }
        }


        private void UpdateCameraFarPlainDictionary()
        {
            var newCameraFarPlaneDictionary = new Dictionary<Camera, float>();

            foreach (var camera in GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                newCameraFarPlaneDictionary.Add(camera, camera.farClipPlane);
            }

            foreach (var newCameraFarPlanePair in newCameraFarPlaneDictionary)
            {
                if (cameraFarPlaneDictionary.ContainsKey(newCameraFarPlanePair.Key))
                {
                    if (newCameraFarPlanePair.Value != 0)
                    {
                        cameraFarPlaneDictionary[newCameraFarPlanePair.Key] = newCameraFarPlanePair.Value;
                    }
                }
                else
                {
                    cameraFarPlaneDictionary.Add(newCameraFarPlanePair.Key, newCameraFarPlanePair.Value);
                }
            }

            cameraFarPlaneDictionary.RemoveNullKeys();
        }


        private void UpdateFilter()
        {
            filter = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(x => x.GetComponent<Renderer>() != null).Select(x => x.gameObject).ToArray();

            terrainsPickableDictionary.Clear();

            foreach (var terrain in GameObject.FindObjectsByType<Terrain>(sortMode: FindObjectsSortMode.None, findObjectsInactive: FindObjectsInactive.Exclude))
            {
                var isPickingEnabled = !SceneVisibilityManager.instance.IsPickingDisabled(terrain.gameObject);

                terrainsPickableDictionary.Add(terrain.gameObject, isPickingEnabled);
            }
        }


        private UndoPropertyModification[] UndoPostProcessModifications(UndoPropertyModification[] modifications)
        {
            UpdateCameraFarPlainDictionary();

            return modifications;
        }


        private Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];

            for (var i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }


    public class HandlesInternal
    {
        [Flags]
        public enum OutlineDrawMode
        {
            SelectionOutline = 1 << 0,
            SelectionWire = 1 << 1
        }


        public static void DrawOutline(Renderer[] renderers, Color parentNodeColor, Color childNodeColor, float fillOpacity = 0)
        {
            int[] parentRenderers, childRenderers;
            FilterRendererIDs(renderers, out parentRenderers, out childRenderers);
            DrawOutline(parentRenderers, null, parentNodeColor, childNodeColor, fillOpacity);
        }


        public static void DrawOutline(int[] parentRenderers, int[] childRenderers, Color parentNodeColor, Color childNodeColor, float fillOpacity = 0)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            CallInternalDrawOutline(parentNodeColor, childNodeColor, 0, parentRenderers, childRenderers, OutlineDrawMode.SelectionOutline, fillOpacity, fillOpacity);
            CallInternalFinishDrawingCamera(Camera.current, false);
        }


        private static void CallInternalDrawOutline(Color parentNodeColor, Color childNodeColor, int submeshOutlineMaterialId, int[] parentRenderers, int[] childRenderers, OutlineDrawMode outlineMode, float parentOutlineAlpha = 0, float childOutlineAlpha = 0)
        {
            var handlesType = typeof(Handles);
            var internalDrawOutlineMethod = handlesType.GetMethod("Internal_DrawOutline", BindingFlags.NonPublic | BindingFlags.Static);

            if (internalDrawOutlineMethod != null)
            {
                internalDrawOutlineMethod.Invoke(null, new object[]
                {
                    parentNodeColor, childNodeColor, submeshOutlineMaterialId,
                    parentRenderers, childRenderers, (int) outlineMode,
                    parentOutlineAlpha, childOutlineAlpha
                });
            }
            else
            {
                Debug.LogError("Internal_DrawOutline method not found");
            }
        }


        private static void CallInternalFinishDrawingCamera(Camera camera, bool someBoolValue)
        {
            var handlesType = typeof(Handles);

            var internalFinishDrawingCameraMethod = handlesType.GetMethod("Internal_FinishDrawingCamera",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] {typeof(Camera), typeof(bool)},
                null);

            if (internalFinishDrawingCameraMethod != null)
            {
                internalFinishDrawingCameraMethod.Invoke(null, new object[] {camera, someBoolValue});
            }
            else
            {
                Debug.LogError("Internal_FinishDrawingCamera method not found");
            }
        }


        private static void FilterRendererIDs(Renderer[] renderers, out int[] parentRendererIDs, out int[] childRendererIDs)
        {
            if (renderers == null)
            {
                Debug.LogWarning("The Renderer array is null. Handles.DrawOutline will not be rendered.");
                parentRendererIDs = new int[0];
                childRendererIDs = new int[0];

                return;
            }

            var parentIndex = 0;
            parentRendererIDs = new int[renderers.Length];

            foreach (var renderer in renderers)
            {
                parentRendererIDs[parentIndex++] = renderer.GetInstanceID();
            }

            var tempChildRendererIDs = new HashSet<int>();

            foreach (var renderer in renderers)
            {
                var children = renderer.GetComponentsInChildren<Renderer>();

                for (var i = 1; i < children.Length; i++)
                {
                    var id = children[i].GetInstanceID();

                    if (!HasMatchingInstanceID(parentRendererIDs, id, parentIndex))
                    {
                        tempChildRendererIDs.Add(id);
                    }
                }
            }

            childRendererIDs = tempChildRendererIDs.ToArray();
        }


        private static bool HasMatchingInstanceID(int[] ids, int id, int cutoff)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }

                if (i > cutoff)
                {
                    return false;
                }
            }

            return false;
        }
    }


    public static class DictionaryExtensions
    {
        public static bool HasNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : Object
        {
            foreach (var key in dictionary.Keys)
            {
                if (key == null)
                {
                    return true;
                }
            }

            return false;
        }


        public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : Object
        {
            if (!dictionary.HasNullKeys())
            {
                return;
            }

            foreach (var key in dictionary.Keys.ToArray())
            {
                if (key == null)
                {
                    dictionary.Remove(key);
                }
            }
        }
    }
}