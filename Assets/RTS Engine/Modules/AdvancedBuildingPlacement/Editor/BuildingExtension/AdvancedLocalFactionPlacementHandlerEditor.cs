using RTSEngine.BuildingExtension;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.BuildingExtension
{
    [CustomEditor(typeof(AdvancedLocalFactionPlacementHandler))]
    public class AdvancedLocalFactionPlacementHandlerEditor : TabsEditorBase<AdvancedLocalFactionPlacementHandler>
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string[] { "General", "Rotation" },
            new string[] { "Hold And Spawn", "Segmentation"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Advanced Local Player Faction Placement", RTSEditorHelper.EditorTitleStyle);

            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch(tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Rotation":
                    OnRotationInspectorGUI();
                    break;
                case "Hold And Spawn":
                    OnHoldAndSpawnInspectorGUI();
                    break;
                case "Segmentation":
                    OnSegmentationInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("reservePlacementResources"));
        }

        protected virtual void OnRotationInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("canRotate"));

            if (!SO.FindProperty("canRotate").boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("positiveRotationKey"));
            EditorGUILayout.PropertyField(SO
                .FindProperty("negativeRotationKey"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("rotationSpeed"));
        }

        private void OnHoldAndSpawnInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("holdAndSpawnEnabled"));
            
            if (!SO.FindProperty("holdAndSpawnEnabled").boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("holdAndSpawnKey"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("preserveBuildingRotation"));
        }

        private void OnSegmentationInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentationEnabled"));
            
            if (!SO.FindProperty("segmentationEnabled").boolValue)
                return;
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO
                .FindProperty("disablePlacementOnAnyInvalidSegment"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentSnapAlignAlways"));
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentSnapKey"));
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentUnsnapRange"));
        }
    }
}
