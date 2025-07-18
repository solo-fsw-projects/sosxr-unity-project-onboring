using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;

namespace OverfortGames.SmartEditorSelection 
{
    [Overlay(typeof(SceneView), "", "", true, defaultDockZone = DockZone.TopToolbar)]
    public class SmartEditorSelectionOverlay : ToolbarOverlay
    {
        SmartEditorSelectionOverlay() : base(
            ToggleSmartEditorSelection.id)
        { }
    }
}




