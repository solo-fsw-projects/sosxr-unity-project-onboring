using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace OverfortGames.SmartEditorSelection 
{
    public class SmartEditorSelectionSettingsWindow : OverlayPopupWindow
    {
        private const string k_SmartSelectionSettingsWindowUxmlPath = "Assets/Overfort Games/Smart Editor Selection/UI/SmartSelectionSettings.uxml";
        private ColorField selectionColorField;
        private ColorField multiSelectionColorField;
        private Toggle forceDisableGizmosField;
        private Toggle showCurrentSelectionLabelField;
        private Slider selectionColorFillAmountField;
        private Slider multiSelectionColorFillAmountField;
        private DropdownField multiSelectionKeyField;
        private Toggle invertMultiSelectionField;
        private Toggle showMultiSelectionLabelField;

        private const int LABEL_WIDTH = 175;

        protected override void OnEnable()
        {
            base.OnEnable();

            VisualTreeAsset mainTemplate = null;

            string[] guids = AssetDatabase.FindAssets("SmartSelectionSettings t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                mainTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
            else
            {
                Debug.LogError("Asset SmartSelectionSettings not found");
                return;
            }

            mainTemplate.CloneTree(rootVisualElement);

            rootVisualElement.Q<TextElement>("Test").text = L10n.Tr("Smart Selection");

            selectionColorField = new ColorField();
            selectionColorField.label = "Hover Selection Color";
            selectionColorField.labelElement.style.width = LABEL_WIDTH;
            selectionColorField.value = ToggleSmartEditorSelection.OutlineSelectionColor.Value;
            selectionColorField.RegisterValueChangedCallback<Color>(
                (x) =>
                {
                    ToggleSmartEditorSelection.OutlineSelectionColor.Value = x.newValue;
                }
            );
            rootVisualElement.Add(selectionColorField);

            selectionColorFillAmountField = new Slider(0, 1);
            selectionColorFillAmountField.label = "Hover Fill Amount";
            selectionColorFillAmountField.labelElement.style.width = LABEL_WIDTH;
            selectionColorFillAmountField.value = ToggleSmartEditorSelection.OutlineSelectionColorFillAmount.Value;
            selectionColorFillAmountField.RegisterValueChangedCallback<float>(
                (x) =>
                {
                    ToggleSmartEditorSelection.OutlineSelectionColorFillAmount.Value = x.newValue;
                }
            );
            rootVisualElement.Add(selectionColorFillAmountField);

            showCurrentSelectionLabelField = new Toggle();
            showCurrentSelectionLabelField.labelElement.style.width = LABEL_WIDTH;
            showCurrentSelectionLabelField.label = "Hover Object Name Label";
            showCurrentSelectionLabelField.value = ToggleSmartEditorSelection.ShowSelectionLabel.Value;

            showCurrentSelectionLabelField.RegisterValueChangedCallback<bool>(
                (x) =>
                {
                    ToggleSmartEditorSelection.ShowSelectionLabel.Value = x.newValue;
                }
            );
            rootVisualElement.Add(showCurrentSelectionLabelField);

            multiSelectionColorField = new ColorField();
            multiSelectionColorField.label = "Selection Color";
            multiSelectionColorField.labelElement.style.width = LABEL_WIDTH;
            multiSelectionColorField.value = ToggleSmartEditorSelection.OutlineMultiSelectionColor.Value;
            multiSelectionColorField.RegisterValueChangedCallback<Color>(
                (x) =>
                {
                    ToggleSmartEditorSelection.OutlineMultiSelectionColor.Value = x.newValue;
                }
            );
            rootVisualElement.Add(multiSelectionColorField);

            multiSelectionColorFillAmountField = new Slider(0, 1);
            multiSelectionColorFillAmountField.label = "Selection Fill Amount";
            multiSelectionColorFillAmountField.labelElement.style.width = LABEL_WIDTH;
            multiSelectionColorFillAmountField.value = ToggleSmartEditorSelection.OutlineMultiSelectionColorFillAmount.Value;
            multiSelectionColorFillAmountField.RegisterValueChangedCallback<float>(
                (x) =>
                {
                    ToggleSmartEditorSelection.OutlineMultiSelectionColorFillAmount.Value = x.newValue;
                }
            );
            rootVisualElement.Add(multiSelectionColorFillAmountField);

            multiSelectionKeyField = new DropdownField();
            multiSelectionKeyField.choices = Enum.GetValues(typeof(ToggleSmartEditorSelection.MultiSelectionKeyType))
                .Cast<ToggleSmartEditorSelection.MultiSelectionKeyType>()
                .Select(x=>x.ToString())
                .ToList();
            multiSelectionKeyField.label = "Multi Selection Key";
            multiSelectionKeyField.labelElement.style.width = LABEL_WIDTH;
            multiSelectionKeyField.value = multiSelectionKeyField.choices[(int)ToggleSmartEditorSelection.MultiSelectionKey.Value];
            multiSelectionKeyField.RegisterValueChangedCallback<string>(
                (x) =>
                {
                    ToggleSmartEditorSelection.MultiSelectionKey.Value = Enum.Parse<ToggleSmartEditorSelection.MultiSelectionKeyType>(x.newValue);
                }
            );
            rootVisualElement.Add(multiSelectionKeyField);

            invertMultiSelectionField = new Toggle();
            invertMultiSelectionField.label = "Multi Selection Auto";
            invertMultiSelectionField.labelElement.style.width = LABEL_WIDTH;
            invertMultiSelectionField.value = ToggleSmartEditorSelection.InvertMultiSelection.Value;
            invertMultiSelectionField.RegisterValueChangedCallback<bool>(
                (x) =>
                {
                    ToggleSmartEditorSelection.InvertMultiSelection.Value = x.newValue;
                }
            );
            rootVisualElement.Add(invertMultiSelectionField);

            showMultiSelectionLabelField = new Toggle();
            showMultiSelectionLabelField.label = "Multi Selection Names Label";
            showMultiSelectionLabelField.labelElement.style.width = LABEL_WIDTH;
            showMultiSelectionLabelField.value = ToggleSmartEditorSelection.ShowMultiSelectionLabel.Value;

            showMultiSelectionLabelField.RegisterValueChangedCallback<bool>(
                (x) =>
                {
                    ToggleSmartEditorSelection.ShowMultiSelectionLabel.Value = x.newValue;
                }
            );
            rootVisualElement.Add(showMultiSelectionLabelField);

            forceDisableGizmosField = new Toggle();
            forceDisableGizmosField.label = "Disable Gizmos";
            forceDisableGizmosField.labelElement.style.width = LABEL_WIDTH;
            forceDisableGizmosField.value = ToggleSmartEditorSelection.ForceDisableGizmos.Value;
            forceDisableGizmosField.RegisterValueChangedCallback<bool>(
                (x) =>
                {
                    ToggleSmartEditorSelection.ForceDisableGizmos.Value = x.newValue;
                }
            );
            rootVisualElement.Add(forceDisableGizmosField);

        }
    }
}

