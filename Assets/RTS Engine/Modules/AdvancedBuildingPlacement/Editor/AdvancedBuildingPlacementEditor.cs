using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace RTSEngine.EditorOnly
{
    [InitializeOnLoad]
    public static class AdvancedBuildingPlacementEditor
    {
        static AdvancedBuildingPlacementEditor()
        {
            RTSEditorHelper.OnRTSPrefabsAndAssetsReload += HandleRTSPrefabsAndAssetsReload;
        }

        private static void HandleRTSPrefabsAndAssetsReload()
        {
            RTSEditorPropertiesHandler.EnableProperty(RTSEditorProperties.advancedBuildingPlacement);
        }

        private static SerializedObject buildingPlacement_SO;
        private static SerializedObject localPlacementHandler_adv_SO;

        [MenuItem("RTS Engine/Modules/Advanced Placement/Configure Map Scene", false, 201)]
        private static void ConfigureMapScene()
        {
            BuildingPlacement buildingPlacement = GameObject.FindObjectOfType<BuildingPlacement>();
            if (!buildingPlacement.IsValid())
            {
                // ERROR
                return;
            }

            buildingPlacement_SO = new SerializedObject(buildingPlacement);

            AdvancedLocalFactionPlacementHandler localPlacementHandler_adv = buildingPlacement.gameObject.GetComponent<AdvancedLocalFactionPlacementHandler>();
            if (!localPlacementHandler_adv.IsValid())
                localPlacementHandler_adv = buildingPlacement.gameObject.AddComponent<AdvancedLocalFactionPlacementHandler>();
            localPlacementHandler_adv_SO = new SerializedObject(localPlacementHandler_adv);

            localPlacementHandler_adv_SO.Update();

            // General Fields
            localPlacementHandler_adv_SO.FindProperty("reservePlacementResources").boolValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("reservePlacementResources").boolValue;

            // Rotation Fields
            localPlacementHandler_adv_SO.FindProperty("canRotate").boolValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("canRotate").boolValue;

            localPlacementHandler_adv_SO.FindProperty("positiveRotationKey").objectReferenceValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("positiveRotationKey").objectReferenceValue;
            localPlacementHandler_adv_SO.FindProperty("negativeRotationKey").objectReferenceValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("negativeRotationKey").objectReferenceValue;

            localPlacementHandler_adv_SO.FindProperty("rotationSpeed").floatValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("rotationSpeed").floatValue;

            // Hold And Spawn Fields
            localPlacementHandler_adv_SO.FindProperty("holdAndSpawnEnabled").boolValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("holdAndSpawnEnabled").boolValue;

            localPlacementHandler_adv_SO.FindProperty("holdAndSpawnKey").objectReferenceValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("holdAndSpawnKey").objectReferenceValue;

            localPlacementHandler_adv_SO.FindProperty("preserveBuildingRotation").boolValue =
                buildingPlacement_SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("preserveBuildingRotation").boolValue;

            localPlacementHandler_adv_SO.ApplyModifiedProperties();
        }

        [MenuItem("RTS Engine/Modules/Advanced Placement/Configure Single Building Prefab", false, 251)]
        private static void ConfigureBuildingPrefab()
        {
            if(!RTSEditorHelper.CurrentPrefab.IsValid())
            {
                // ERROR
                return;
            }

            ConfigureBuildingPrefabInternal(RTSEditorHelper.CurrentPrefab.GetComponent<IBuilding>());
        }

        [MenuItem("RTS Engine/Modules/Advanced Placement/Configure All Building Prefabs", false, 252)]
        private static void ConfigureAllBuildingPrefabs()
        {
            foreach(IEntity entity in RTSEditorHelper.GetEntities().Values)
            {
                if(entity.IsBuilding())
                    ConfigureBuildingPrefabInternal(entity.gameObject.GetComponent<IBuilding>());
            }
        }

        private static void ConfigureBuildingPrefabInternal(IBuilding building)
        {
            BuildingPlacer buildingPlacer = RTSEditorHelper.CurrentPrefab.GetComponentInChildren<BuildingPlacer>();
            if(!buildingPlacer.IsValid())
            {
                // ERROR
                RTSEditorHelper.LogError($"[RTS Engine Editor] Unable to find a '{typeof(BuildingPlacer).Name}' component attached to the prefab!");
                return;
            }

            SerializedObject buildingPlacer_SO = new SerializedObject(buildingPlacer);

            AdvancedBuildingPlacer buildingPlacer_adv = buildingPlacer.gameObject.gameObject.GetComponent<AdvancedBuildingPlacer>();
            if (!buildingPlacer_adv.IsValid())
                buildingPlacer_adv = buildingPlacer.gameObject.AddComponent<AdvancedBuildingPlacer>();
            SerializedObject buildingPlacer_adv_SO = new SerializedObject(buildingPlacer_adv);

            buildingPlacer_adv_SO.Update();

            for(int i = 0; i < buildingPlacer_SO.FindProperty("placableTerrainAreas").arraySize; i++)
            {
                buildingPlacer_adv_SO.FindProperty("placableTerrainAreas").InsertArrayElementAtIndex(i);

                buildingPlacer_adv_SO.FindProperty("placableTerrainAreas").GetArrayElementAtIndex(i).objectReferenceValue =
                    buildingPlacer_SO.FindProperty("placableTerrainAreas").GetArrayElementAtIndex(i).objectReferenceValue;
            }

            buildingPlacer_adv_SO.FindProperty("canPlaceOutsideBorder").boolValue =
                buildingPlacer_SO.FindProperty("canPlaceOutsideBorder").boolValue;

            buildingPlacer_adv_SO.FindProperty("debug").boolValue =
                buildingPlacer_SO.FindProperty("debug").boolValue;

            buildingPlacer_adv_SO.ApplyModifiedProperties();

            EditorUtility.SetDirty(RTSEditorHelper.CurrentPrefab);

            GameObject.DestroyImmediate(buildingPlacer);
        }
    }
}
