using RTSEngine.BuildingExtension;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.BuildingExtension
{
    [CustomEditor(typeof(AdvancedBuildingPlacer))]
    public class AdvancedBuildingPlacerEditor : TabsEditorBase<AdvancedBuildingPlacer>
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string[] { "General", "Segmentation", "Grid Placement" },
        };

        public override void OnInspectorGUI()
        {
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
                case "Segmentation":
                    OnSegmentationInspectorGUI();
                    break;
                case "Grid Placement":
                    OnGridPlacementInspectorGUI();
                    break;
            }
        }

        private void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("placableTerrainAreas"));
            EditorGUILayout.PropertyField(SO
                .FindProperty("canPlaceOutsideBorder"));
            EditorGUILayout.PropertyField(SO
                .FindProperty("canPlaceInEnemyBorder"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("ignoreLayerMask"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("debug"));
        }

        private void OnSegmentationInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentData"), true);
            
            if (!SO.FindProperty("segmentData.enabled").boolValue)
                return;
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO
                .FindProperty("minSegmentCountForReplacement"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("firstSegmentReplacement"), true);
            EditorGUILayout.PropertyField(SO
                .FindProperty("lastSegmentReplacement"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentPeriodReplacement"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("defaultSegmentReplacement"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("segmentSnapping"), true);
        }

        protected virtual void OnGridPlacementInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO
                .FindProperty("gridOptions"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO
                .FindProperty("displayGridArea"), true);
            EditorGUILayout.PropertyField(SO
                .FindProperty("gridAreaColor"), true);
        }
    }
}
