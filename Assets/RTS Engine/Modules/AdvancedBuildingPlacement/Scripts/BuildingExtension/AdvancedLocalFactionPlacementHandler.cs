using RTSEngine.Audio;
using RTSEngine.Controls;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSEngine.BuildingExtension
{
    public class AdvancedLocalFactionPlacementHandler : AdvancedBuildingPlacementHandlerBase
    {
        #region Attributes
        //[Header("General")]
        [SerializeField, Tooltip("Audio clip to play when the player places a building.")]
        private AudioClipFetcher placeBuildingAudio = new AudioClipFetcher();

        //[Header("Rotation")]
        [SerializeField, Tooltip("Enable to allow the player to rotate buildings while placing them.")]
        private bool canRotate = true;
        [SerializeField, Tooltip("Key used to increment the building's euler rotation on the y axis.")]
        private ControlType positiveRotationKey = null;
        [SerializeField, Tooltip("Key used to decrement the building's euler rotation on the y axis.")]
        private ControlType negativeRotationKey = null;
        [SerializeField, Tooltip("How fast would the building rotate?")]
        private float rotationSpeed = 1f;

        //[Header("Hold And Spawn")]
        [SerializeField, Tooltip("Enable to allow the player to hold a key to keep placing the same building type multiple times.")]
        private bool holdAndSpawnEnabled = false;
        private bool holdAndSpawnActive = false;
        private IBuildingPlacementTask holdAndSpawnTask;
        private BuildingPlacementOptions holdAndSpawnOptions;

        [SerializeField, Tooltip("Key used to keep placing the same building type multiple times when the option to do so is enabled")]
        private ControlType holdAndSpawnKey = null;
        [SerializeField, Tooltip("Preserve last building placement rotation when holding and spawning buildings?")]
        private bool preserveBuildingRotation = true;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            globalEvent.GameStateUpdatedGlobal += HandleGameStateUpdatedGlobal;
        }

        protected override void OnDisabled()
        {
            globalEvent.GameStateUpdatedGlobal -= HandleGameStateUpdatedGlobal;
        }

        private void HandleGameStateUpdatedGlobal(IGameManager gameMgr, EventArgs args)
        {
            if (gameMgr.State == GameStateType.pause && SegmentationActive)
                Stop();
        }
        #endregion

        #region Update
        protected override void OnInactiveUpdate()
        {
            if (holdAndSpawnActive)
            {
                Add(holdAndSpawnTask, holdAndSpawnOptions);

                holdAndSpawnActive = false;
            }
        }

        protected override void OnActiveUpdate()
        {
            // Right mouse button stops building placement
            if (Input.GetMouseButtonUp(1))
            {
                Stop();
                return;
            }

            MoveBuilding();

            RotateBuilding();

            // Activate drag and drop for current building placement?
            if (SegmentationEnabled && !SegmentationActive
                && current.firstInstance.PlacerComponent.SegmentData.enabled
                && Input.GetMouseButtonDown(0))
            {
                EnableSegmentation();
            }

            // Left mouse button allows to place the buildings
            if (Input.GetMouseButtonUp(0)
                && (!EventSystem.current.IsPointerOverGameObject() || SegmentationActive))
                AttemptComplete();
        }
        #endregion

        #region Handling Movement
        private void MoveBuilding(bool forceMovementUpdate = false)
        {
            // Using a raycheck, we will make the current building follow the mouse position and stay on top of the terrain.
            if (!Physics.Raycast(
                mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition),
                out RaycastHit hit,
                Mathf.Infinity,
                placerMgr.PlacableLayerMask))
                return;

            // Depending on the height of the terrain, we will place the building on it
            Vector3 nextBuildingPos = hit.point;
            // Make sure that the building position on the y axis stays inside the min and max height interval
            nextBuildingPos.y += placerMgr.BuildingPositionYOffset;

            if (forceMovementUpdate
                || current.lastInstance.transform.position != nextBuildingPos)
            {
                // Segmentation mode inactive? Simple single building placement handling
                if (!SegmentationActive)
                {
                    if (IsGridPlacementActive)
                    {
                        placerMgr.GridHandler.TryGetCellPosition(nextBuildingPos, out nextBuildingPos);
                    }

                    current.firstInstance.PlacerComponent.OnPlacementUpdate(nextBuildingPos);
                    return;
                }

                // Segmentation mode active
                OnSegmentationUpdate(finalPosition: nextBuildingPos);
            }
        }
        #endregion

        #region Handling Rotation
        private void RotateBuilding()
        {
            // Can not rotate buildings when segmentation mode is active 
            if (!canRotate
                || SegmentationActive
                || current.mainOptions.disableRotation)
                return;

            for (int i = 0; i < current.SegmentCount; i++)
            {
                IBuilding instance = current.instances[i];

                float rotationMultiplier;
                // For grid placement, only allow for 4 direction rotations
                if (IsGridPlacementActive)
                {
                    rotationMultiplier = (controls.GetDown(positiveRotationKey) ? 1.0f : (controls.GetDown(negativeRotationKey) ? -1.0f : 0.0f));
                    instance.transform.Rotate(new Vector3(0.0f, rotationMultiplier * 90.0f, 0.0f));
                }
                // Non grid placement allows for free rotation
                else
                {
                    Vector3 nextEulerAngles = instance.transform.rotation.eulerAngles;
                    // Only rotate if one of the keys is pressed down (check for direction) and rotate on the y axis only.
                    rotationMultiplier = (controls.Get(positiveRotationKey) ? 1.0f : (controls.Get(negativeRotationKey) ? -1.0f : 0.0f));
                    nextEulerAngles.y += rotationSpeed * rotationMultiplier;

                    instance.transform.rotation = Quaternion.Euler(nextEulerAngles);
                }

                if (rotationMultiplier != 0.0f)
                    instance.PlacerComponent.OnPlacementUpdate();
            }
        }
        #endregion

        #region Adding
        protected override void OnAdded(ErrorMessage errorMsg, IBuilding placementInstance, IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            if (errorMsg != ErrorMessage.none)
                return;

            placementInstance.gameObject.SetActive(false);
        }
        #endregion

        #region Starting
        protected override void OnStart(IBuilding placementInstance, BuildingPlacementOptions options = default)
        {
            placementInstance.gameObject.SetActive(true);
            Vector3 nextBuildingPos = placementInstance.transform.position;

            // Only if this is a new instance/segment, will we set its position on start.
            if (!options.isReplacingExisting)
            {
                // Set the position of the new building (and make sure it's on the terrain)
                if (Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, placerMgr.PlacableLayerMask))
                {
                    nextBuildingPos = hit.point;
                    nextBuildingPos.y += placerMgr.BuildingPositionYOffset;
                    if (IsGridPlacementActive)
                    {
                        placerMgr.GridHandler.TryGetCellPosition(nextBuildingPos, out nextBuildingPos);
                    }
                }
            }

            placementInstance.PlacerComponent.OnPlacementUpdate(nextBuildingPos);
        }
        #endregion

        #region Stopping
        protected override void OnStop(AdvancedPendingPlacementData removedPlacement)
        {
            holdAndSpawnActive = false;
        }
        #endregion

        #region Complete
        protected override void OnComplete(ErrorMessage errorMsg, CompletedPlacementData completedPlacement)
        {
            if (errorMsg != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = current.firstInstance
                });

                return;
            }

            audioMgr.PlaySFX(placeBuildingAudio.Fetch(), completedPlacement.task.TargetObject, loop:false);

            if (!IsActive && holdAndSpawnEnabled && controls.Get(holdAndSpawnKey))
            {
                holdAndSpawnActive = true;
                holdAndSpawnTask = completedPlacement.task;
                holdAndSpawnOptions = new BuildingPlacementOptions
                {
                    setInitialRotation = preserveBuildingRotation,
                    initialRotation = completedPlacement.rotation
                };
            }
        }
        #endregion
    }
}
