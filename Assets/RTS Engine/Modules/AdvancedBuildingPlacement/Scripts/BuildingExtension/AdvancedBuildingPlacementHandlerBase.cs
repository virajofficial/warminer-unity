using RTSEngine.Audio;
using RTSEngine.Cameras;
using RTSEngine.Controls;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    #region Data Types
    public struct AdvancedPendingPlacementData : IPendingPlacementData
    {
        // Main task that initiated the building placement process
        public IBuildingPlacementTask mainTask;
        public BuildingPlacementOptions mainOptions;

        // Active instances that are being placed within this placement process
        public List<IBuilding> instances;

        // Individual placement tasks for each instance
        public List<IBuildingPlacementTask> instanceTasks;
        // Includes indexes of instances list that are not placement instances but already-placed buildings snapped to this placement segmentation process
        public List<int> snappedIndexes;

        public int SegmentCount => instances.Count;
        public bool IsActive => SegmentCount > 0;

        // First element in the instances list
        public IBuilding firstInstance => SegmentCount > 0 ? instances[0] : null;
        // Last element in the instances list
        public IBuilding lastInstance => SegmentCount > 0 ? instances[SegmentCount - 1] : null;

        // Segmentation option for the placement process
        public BuildingPlacementSegmentData segmentData;
        public bool IsValidSegmentIndex(int index) => index.IsValidIndex(instances);

        public AdvancedPendingPlacementData(IBuildingPlacementTask mainTask, BuildingPlacementOptions mainOptions)
        {
            this.mainTask = mainTask;
            this.mainOptions = mainOptions;

            instances = new List<IBuilding>();
            instanceTasks = new List<IBuildingPlacementTask>();
            snappedIndexes = new List<int>();

            segmentData = mainTask.TargetObject.GetComponentInChildren<IBuildingPlacer>().SegmentData;
        }

        public void AddInstance(int newSegmentIndex, IBuilding instance, IBuildingPlacementTask task)
        {
            instances[newSegmentIndex] = instance;
            instanceTasks[newSegmentIndex] = task;
        }

        public void RemoveAt(int index)
        {
            instances.RemoveAt(index);
            instanceTasks.RemoveAt(index);

            snappedIndexes.Clear();
            for (int i = 0; i < SegmentCount; i++)
            {
                if (!instances[i].IsPlacementInstance)
                    snappedIndexes.Add(i);
            }
        }

        public int GetNewFreeIndex()
        {
            if (SegmentCount >= 2)
            {
                int snappedIndex;
                if ((snappedIndex = snappedIndexes.IndexOf(SegmentCount - 1)) != -1)
                {
                    snappedIndexes[snappedIndex] += 1;
                }

                instances.Add(instances[SegmentCount - 1]);
                instanceTasks.Add(instanceTasks[instanceTasks.Count - 1]);

                instances[SegmentCount - 2] = null;
                instanceTasks[instanceTasks.Count - 2] = null;

                return SegmentCount - 2;
            }
            else
            {
                instances.Add(null);
                instanceTasks.Add(null);

                return SegmentCount - 1;
            }
        }

    }
    #endregion

    public abstract class AdvancedBuildingPlacementHandlerBase : MonoBehaviour, IBuildingPlacementHandler
    {
        #region Attributes
        [HideInInspector]
        public Int2D tabID;

        protected AdvancedPendingPlacementData current
        {
            get 
            {
                if (queue.Count == 0)
                {
                    logger.LogError($"[{GetType().Name}] Attempting to get current pending placement data from an empty queue. Please check for IsActive first!");
                    return default;
                }
                return queue[0];
            }
        }
        public bool CanRotateCurrent => Count > 0 && !current.mainOptions.disableRotation;
        private List<AdvancedPendingPlacementData> queue;
        public IReadOnlyList<AdvancedPendingPlacementData> Queue => queue;
        public int Count => queue.Count;

        public bool IsActive => Count > 0 && current.IsActive;

        [SerializeField, Tooltip("Reserve resources that will be used to place the placement buildings so that the player faction does not consume them before the placement is completed.")]
        private bool reservePlacementResources = true;
        //[SerializeField, Tooltip("Disable placement of the multiple instances of the building type if at least one of them does not have the placement requirements fulfilled?")]
        //private bool disablePlacementOnAnyInvalid = true;

        //[Header("Segmentation")]
        [SerializeField, Tooltip("Enable placing multiple instances of the building type by dragging and dropping the mouse in a row that starts from the position the mouse was dragged from until the position where the mouse is released at. Only works with buildings that have this option enabled at the level of their BuildingPlacer component.")]
        private bool segmentationEnabled = true;
        public bool SegmentationEnabled => segmentationEnabled;
        public bool SegmentationActive { private set; get; }
        protected Vector3 SegmentationInitialPosition { private set; get; }
        // firstArea and firstSegmentLength hold the area and length of the first segment instance prior to segmentation placement start
        // the first instance can be replaced later with a different segment but these fields will still hold the original segment values
        // the reason behind this is that whenever the first segment is replaced with one with a different length of area ...
        // ... we can adjust the positioning of the other segments to keep the same starting position.
        // this is only valid when it comes to grid placement
        private Int2D firstSegmentArea;
        private float firstSegmentLength;

        [SerializeField, Tooltip("Enable this field to disable placement of all segments if only one does not meet the placement requirements.")]
        private bool disablePlacementOnAnyInvalidSegment = true;

        [SerializeField, Tooltip("When a segment is snapped into an already placed building, enable this option to always align the new segments orthogonally to the placed snap target.")]
        private bool segmentSnapAlignAlways = true;
        [SerializeField, Tooltip("When the above snap alignment option is disabled, you can assign a control type to this field to enable it when the player holds down the key associated with the control type.")]
        private ControlType segmentSnapKey = null;
        [SerializeField, Tooltip("When a segment moves away from its snap target by the distance assigned in this field, it would be unsnapped."), Min(0.0f)]
        private float segmentUnsnapRange = 0.2f;
        private Vector3 segmentDirection;

        // Grid placement
        public bool IsGridPlacementActive { private set; get; }

        public IFactionSlot FactionSlot { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; }
        protected ISelector selector { private set; get; } 
        protected IMovementManager mvtMgr { private set; get; } 
        protected IMainCameraController mainCamCtrl { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementAdded;
        private void RaisePlacementAdded(AdvancedPendingPlacementData addedPlacement)
        {
            var handler = PlacementAdded;
            handler?.Invoke(this, addedPlacement);
        }

        public event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementStopped;
        private void RaisePlacementStopped(AdvancedPendingPlacementData stoppedPlacement)
        {
            var handler = PlacementStopped;
            handler?.Invoke(this, stoppedPlacement);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IFactionSlot factionSlot)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.placerMgr = gameMgr.GetService<IBuildingPlacement>();
            this.selector = gameMgr.GetService<ISelector>(); 
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.mainCamCtrl = gameMgr.GetService<IMainCameraController>();

            this.FactionSlot = factionSlot;

            queue = new List<AdvancedPendingPlacementData>();

            ResetProperties();

            IsGridPlacementActive = placerMgr.GridHandler.IsValid() && placerMgr.GridHandler.IsEnabled;

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Update
        public void OnUpdate()
        {
            if (!IsActive)
            {
                OnInactiveUpdate();
                return;
            }

            OnActiveUpdate();
        }

        protected virtual void OnActiveUpdate()
        {
        }

        protected virtual void OnInactiveUpdate()
        {

        }
        #endregion

        #region Adding
        public virtual ErrorMessage CanAdd(IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            ErrorMessage errorMsg;
            if ((errorMsg = task.CanStart()) != ErrorMessage.none)
                return errorMsg;
            else if (!task.FactionID.IsSameFaction(FactionSlot.ID))
                return ErrorMessage.factionMismatch;

            return ErrorMessage.none;
        }

        public ErrorMessage Add(IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            if (options.isSnappingSegment)
            {
                return AddExistingToSnap(options.targetIndex, options.snapTarget);
            }

            ErrorMessage errorMsg;
            if ((errorMsg = CanAdd(task, options)) != ErrorMessage.none)
            {
                if (FactionSlot.IsLocalPlayerFaction())
                {
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = task.TargetObject
                    });
                }

                OnAdded(errorMsg, null, task, options);
                return errorMsg;
            }

            if(FactionSlot.IsLocalPlayerFaction())
                selector.Lock(buildingMgr);

            Vector3 nextPlacementPosition = Vector3.zero;
            Quaternion nextPlacementRotation = task.TargetObject.transform.rotation;

            int nextSegmentIndex;
            AdvancedPendingPlacementData nextPlacementData;

            if (options.isReplacingExisting)
            {
                if (Count == 0)
                {
                    logger.LogError($"[{GetType().Name}] Attempting to replace placement segment instance while there is no pending placement in the queue!");
                    return ErrorMessage.invalid;
                }
                else if (!current.IsValidSegmentIndex(options.targetIndex))
                {
                    logger.LogError($"[{GetType().Name}] The specified segment index '{options.targetIndex}' of the placement instance to replace is invalid. Current placement instances count is '{current.SegmentCount}'.");
                    return ErrorMessage.invalid;
                }
                else if (!current.instances[options.targetIndex].IsPlacementInstance)
                {
                    logger.LogError($"[{GetType().Name}] The specified segment index '{options.targetIndex}' refers to an already placed building that can not be replaced!");
                    return ErrorMessage.invalid;
                }

                nextPlacementPosition = current.instances[options.targetIndex].transform.position;
                nextPlacementRotation = current.instances[options.targetIndex].transform.rotation;

                DeleteCurrentSegment(options.targetIndex);

                nextPlacementData = current;
                nextSegmentIndex = options.targetIndex;
            }
            else
            {
                if (options.isAddingSegment)
                {
                    if (Count == 0)
                    {
                        logger.LogError($"[{GetType().Name}] Attempting to add placement instance as a new segment while there is no pending placement in the queue!");
                        return ErrorMessage.invalid;
                    }
                    nextPlacementData = current;
                }
                // If this is the first instance to add to the current placement queue and this is not replacing an existing instance
                // Then it is the one with the main creation task that is used to generate other segments in the case of segmentation placement mode 
                else
                {
                    nextPlacementData = new AdvancedPendingPlacementData(task, options);
                    queue.Add(nextPlacementData);
                }

                nextSegmentIndex = current.GetNewFreeIndex();

                if (options.setInitialRotation)
                    nextPlacementRotation = options.initialRotation;
            }

            return AddNewInstance(task, options, nextPlacementPosition, nextPlacementRotation, nextPlacementData, nextSegmentIndex);
        }

        private ErrorMessage AddNewInstance(IBuildingPlacementTask task, BuildingPlacementOptions options, Vector3 position, Quaternion rotation, AdvancedPendingPlacementData placementData, int segmentIndex)
        {
            task.OnStart();

            IBuilding placementInstance = buildingMgr.CreatePlacementBuilding(
                task.TargetObject,
                rotation,
                new InitBuildingParameters
                {
                    factionID = FactionSlot.ID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = options.initialCenter,
                });
            placementInstance.transform.position = position;

            if (reservePlacementResources)
                gameMgr.GetService<IResourceManager>().UpdateReserveResources(
                    task.RequiredResources,
                    FactionSlot.ID
                );

            placementData.AddInstance(segmentIndex, placementInstance, task);

            OnAdded(ErrorMessage.none, placementInstance, task, options);
            RaisePlacementAdded(placementData);

            for (int index = 0; index < current.SegmentCount; index++)
            {
                // Callbacks only for placement instances
                if (!current.instances[index].IsPlacementInstance)
                    continue;

                current.instances[index].PlacerComponent.OnPlacementStart(new BuildingPlacementUpdateData
                {
                    targetSegmentIndex = segmentIndex,
                    sourceSegmentIndex = index,
                    segmentCount = current.SegmentCount,
                });
            }
            if (IsGridPlacementActive)
            {
                placerMgr.GridHandler.TryGetCellPosition(placementInstance.transform.position, out Vector3 gridPosition);
                placementInstance.transform.position = gridPosition;
            }
            globalEvent.RaiseBuildingPlacementStartGlobal(placementInstance);

            if (Count == 1)
                OnStart(placementInstance, options);

            return ErrorMessage.none;
        }

        private ErrorMessage AddExistingToSnap(int snapIndex, IBuilding snapTarget)
        {
            // Target segment index is invalid or has been already snapped to an existing/placed building
            if (!IsActive
                || !SegmentationActive
                || !current.IsValidSegmentIndex(snapIndex)
                || current.snappedIndexes.Contains(snapIndex))
                return ErrorMessage.invalid;

            if (current.SegmentCount < 1)
            {
                logger.LogError($"[{GetType().Name}] Unable to snap segment segment when there is no more than one segment in the current placement!");
                return ErrorMessage.invalid;
            }
            else if (!snapTarget.IsValid() || !snapTarget.PlacerComponent.Placed)
            {
                logger.LogError($"[{GetType().Name}] Unable to snap segment segment when the snap target is invalid or is not already placed!");
                return ErrorMessage.invalid;
            }

            DeleteCurrentSegment(snapIndex);

            current.instances[snapIndex] = snapTarget;
            current.instanceTasks[snapIndex] = null;
            current.snappedIndexes.Add(snapIndex);

            return ErrorMessage.none;
        }

        private ErrorMessage RevertSnappedInstance(int snappedIndex, Vector3 position, Quaternion rotation)
        {
            if (!IsActive
                || !SegmentationActive
                || !current.snappedIndexes.Contains(snappedIndex))
                return ErrorMessage.invalid;

            current.snappedIndexes.Remove(snappedIndex);
            return AddNewInstance(current.mainTask, default, position, rotation, current, snappedIndex);
        }

        protected virtual void OnAdded(ErrorMessage errorMsg, IBuilding placementInstance, IBuildingPlacementTask task, BuildingPlacementOptions options) { }
        #endregion

        #region Starting
        protected virtual void OnStart(IBuilding placementInstance, BuildingPlacementOptions options = default) { }
        #endregion

        #region Stopping
        // Remove all segments in queueIndex 0
        public bool Stop() => Stop(queueIndex: 0);

        public bool StopAll()
        {
            while (queue.Count > 0)
                Stop(queueIndex: 0);

            return true;
        }

        public void StopOnCondition(Func<AdvancedPendingPlacementData, bool> condition)
        {
            int index = 0;
            while (index < Count)
            {
                if (condition(Queue[index]))
                {
                    Stop(index);
                    continue;
                }
                index++;
            }
        }

        // Remove all segments in the queueIndex
        public virtual bool Stop(int queueIndex)
        {
            if (!IsActive
                || !queueIndex.IsValidIndex(queue))
                return false;


            // Remove placed instances (whose indexes are stored in the snappedIndexes list) directly
            while (current.snappedIndexes.Count > 0)
            {
                current.RemoveAt(current.snappedIndexes[0]);
            }

            // Remove all placement segments
            while (current.SegmentCount > 0)
            {
                current.firstInstance.PlacerComponent.OnPlacementStop(new BuildingPlacementUpdateData
                {
                    targetSegmentIndex = -1,
                    sourceSegmentIndex = -1,
                    segmentCount = current.SegmentCount
                });

                DeleteCurrentSegment(0);
                current.RemoveAt(0);
            }

            ResetProperties();

            return true;
        }

        public bool StopSegment(int segmentIndex, bool isPlacementComplete)
        {
            if (!IsActive
                || Count == 0
                || !current.IsValidSegmentIndex(segmentIndex))
                return false;

            // If this is a placed snapped instance then remove it directly and do nothing else
            if (!current.snappedIndexes.Contains(segmentIndex))
                DeleteCurrentSegment(segmentIndex);

            current.RemoveAt(segmentIndex);

            if (!isPlacementComplete)
            {
                for (int index = 0; index < current.SegmentCount; index++)
                {
                    if (!current.instances[index].IsPlacementInstance)
                        continue;

                    current.instances[index].PlacerComponent.OnPlacementStop(new BuildingPlacementUpdateData
                    {
                        targetSegmentIndex = segmentIndex,
                        sourceSegmentIndex = index,
                        segmentCount = current.SegmentCount
                    });
                }
            }

            // No more active placement instances? Disable placement
            if (current.SegmentCount == 0)
                ResetProperties();

            return true;
        }

        private void ResetProperties()
        {
            SegmentationActive = false;

            if (Count > 0)
            {
                AdvancedPendingPlacementData removedPlacement = current;

                queue.RemoveAt(0);
                OnStop(removedPlacement);
                RaisePlacementStopped(removedPlacement);
            }

            if (Count == 0)
            {
                globalEvent.RaiseBuildingPlacementResetGlobal(this);

                if(FactionSlot.IsLocalPlayerFaction())
                    selector.Unlock(buildingMgr);
            }
            else
                OnStart(current.firstInstance);
        }

        protected virtual void OnStop(AdvancedPendingPlacementData removedPlacement) { }
        #endregion

        #region Complete
        protected bool AttemptComplete()
        {
            if (current.segmentData.enabled)
            {
                if (current.segmentData.minAmount > current.SegmentCount)
                {
                    while (current.SegmentCount > 1)
                        StopSegment(current.SegmentCount - 1, isPlacementComplete: false);

                    SegmentationActive = false;

                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = ErrorMessage.placementSegmentAmountNotMet,

                        source = current.mainTask.TargetObject,
                        amount = current.segmentData.minAmount
                    });
                    return false;
                }
            }

            //if ((disablePlacementOnAnyInvalid && !SegmentationActive)
            if(SegmentationActive && disablePlacementOnAnyInvalidSegment)
            {
                // Do not allow placement for all current instances if at least one them does not meet placement requirements 
                ErrorMessage errorMessage;
                for (int index = 0; index < current.SegmentCount; index++)
                {
                    if ((errorMessage = CanComplete(index)) != ErrorMessage.none)
                    {
                        OnComplete(errorMessage, new CompletedPlacementData { });
                        Stop();
                        return false;
                    }
                }

                // All placement instances meet placement conditions, place each one individually
                while (IsActive && current.SegmentCount > 0)
                    CompleteOnConditionMet(segmentIndex: 0);

                return true;
            }

            // Only place instances that meet conditions and stop placing instances that do not
            while (IsActive && current.SegmentCount > 0)
            {
                if (!Complete(index: 0))
                {
                    if (!SegmentationActive)
                        break;

                    StopSegment(segmentIndex: 0, isPlacementComplete: false);
                }
            }

            return true;
        }

        public ErrorMessage CanPlace(IBuildingPlacer buildingPlacer)
        {
            if (!buildingPlacer.IsValid())
                return ErrorMessage.invalid;
            if(buildingPlacer.Placed)
                return ErrorMessage.alreadyPlaced;

            IBuilding instance = buildingPlacer.Building;

            if (IsGridPlacementActive)
            {
                if (placerMgr.GridHandler.IsOccupied(
                    area: instance.PlacerComponent.GridOptions.area,
                    bottomLeftWorldPosition: instance.PlacerComponent.GridOptions.ApplyPivotPointReverse(instance.transform.position, GridAreaPivotPoint.bottomLeft),
                    horizontal: instance.PlacerComponent.GridOptions.IsHorizontal))
                    return ErrorMessage.placementGridOccupied;
            }

            return ErrorMessage.none;
        }

        private ErrorMessage CanComplete() => CanComplete(segmentIndex: 0);
        private ErrorMessage CanComplete(int segmentIndex)
        {
            if (!current.IsValidSegmentIndex(segmentIndex))
                return ErrorMessage.invalid;

            IBuilding instance = current.instances[segmentIndex];

            if (!instance.IsPlacementInstance)
                return ErrorMessage.none;
            else if (!instance.PlacerComponent.CanPlace)
                return instance.PlacerComponent.CanPlaceError;

            if (reservePlacementResources)
                gameMgr.GetService<IResourceManager>().ReleaseResources(
                    current.instanceTasks[segmentIndex].RequiredResources,
                    FactionSlot.ID
                );

            ErrorMessage errorMsg = current.instanceTasks[segmentIndex].CanComplete();

            // Reserve resources again after releasing them for task completion check
            // In case this is not called prior to an actual placement command
            if (reservePlacementResources)
                gameMgr.GetService<IResourceManager>().UpdateReserveResources(
                    current.instanceTasks[segmentIndex].RequiredResources,
                    FactionSlot.ID
                );

            return errorMsg;
        }

        public bool Complete() => AttemptComplete();

        private bool Complete(int index)
        {
            if (!index.IsValidIndex(current.instances))
                return false;

            ErrorMessage errorMessage = CanComplete(index);
            if (errorMessage != ErrorMessage.none)
            {
                OnComplete(errorMessage, new CompletedPlacementData { });
                return false;
            }

            return CompleteOnConditionMet(index);
        }

        private bool CompleteOnConditionMet(int segmentIndex)
        {
            if (!segmentIndex.IsValidIndex(current.instances))
                return false;

            IBuilding instance = current.instances[segmentIndex];

            if (!instance.IsPlacementInstance)
            {
                StopSegment(segmentIndex, isPlacementComplete: true);
                return true;
            }

            current.instances[segmentIndex].PlacerComponent.OnPlacementPreComplete();

            current.instanceTasks[segmentIndex].OnComplete();

            buildingMgr.CreatePlacedBuilding(
                instance,
                instance.transform.position,
                instance.transform.rotation,
                new InitBuildingParameters
                {
                    factionID = instance.FactionID,
                    free = false,

                    setInitialHealth = false,

                    giveInitResources = true,

                    buildingCenter = instance.PlacerComponent.PlacementCenter,

                    playerCommand = FactionSlot.IsLocalPlayerFaction(),

                    placedData = new BuildingPlacedData
                    {
                        isSegment = SegmentationActive,
                        segmentDirection = segmentDirection
                    }
                });

            CompletedPlacementData completedPlacement = new CompletedPlacementData 
            {
                code = instance.Code,
                task = current.mainTask,
                position = instance.transform.position,
                rotation = instance.transform.rotation 
            };

            // To reset the building placement state
            StopSegment(segmentIndex, isPlacementComplete: true);

            OnComplete(ErrorMessage.none, completedPlacement);

            return true;
        }

        protected virtual void OnComplete(ErrorMessage errorMsg, CompletedPlacementData completedPlacement) { }
        #endregion

        #region Segmentation
        protected void EnableSegmentation()
        {
            if (!SegmentationEnabled
                || !IsActive
                || SegmentationActive
                || !current.firstInstance.PlacerComponent.SegmentData.enabled)
                return;

            SegmentationInitialPosition = current.firstInstance.transform.position;

            if (IsGridPlacementActive)
            {
                firstSegmentArea = current.firstInstance.PlacerComponent.GridOptions.area;
                firstSegmentLength = current.firstInstance.PlacerComponent.SegmentData.segmentLength;
            }

            SegmentationActive = true;
        }

        protected void OnSegmentationUpdate(Vector3 finalPosition)
        {
            // Determine the amount of segments/parts of the building type to have depending on the distance between the starting and last drag and drop position
            float distance = Mathf.Ceil(Vector3.Distance(finalPosition, SegmentationInitialPosition));

            int segments = 1;
            // Calculate the amount of the expected segments to be in this placement
            // Going through every existing segment to take into account different segment lengths
            foreach (var instance in current.instances)
            {
                if (distance - instance.PlacerComponent.SegmentData.segmentLength <= 0)
                    break;

                distance -= instance.PlacerComponent.SegmentData.segmentLength;
                segments++;
            }

            if (distance > 0)
                segments += Mathf.FloorToInt((distance / current.firstInstance.PlacerComponent.SegmentData.segmentLength));

            // If the amount of segments does not match the amount of placement instances, then fix that
            while (segments != current.SegmentCount)
            {
                if (segments < current.SegmentCount)
                {
                    int stopIndex = current.SegmentCount - 1;
                    if (current.SegmentCount > 2)
                        stopIndex = current.SegmentCount - 2;
                    if (!StopSegment(stopIndex, isPlacementComplete: false))
                        break;
                }
                else
                {
                    if (Add(current.mainTask, options: new BuildingPlacementOptions { isAddingSegment = true }) != ErrorMessage.none)
                        break;
                }
            }

            // Go through all the segment instances and assign their position and rotation accordingly

            // Determine the segments direction and consider snapped buildings and whether snapping direction is an option
            segmentDirection = (finalPosition - SegmentationInitialPosition).normalized;

            // Here we have snapped into placed buildings and are allowed to change the direction according to them.
            if (current.snappedIndexes.Count > 0
                && (segmentSnapAlignAlways || controls.Get(segmentSnapKey)))
            {
                Vector3 snapSegmentsDirection = current.instances[current.snappedIndexes[0]].PlacerComponent.PlacedData.segmentDirection;
                // Use the Dot operation and the result's sign to determine the direction of the current segments
                float dot = Vector3.Dot(snapSegmentsDirection, segmentDirection);
                if (Mathf.Abs(dot) >= 0.5f)
                {
                    segmentDirection = snapSegmentsDirection * Mathf.Sign(dot);
                }
                else
                {
                    Vector3 nextSegmentationDirection = Vector3.Cross(
                        snapSegmentsDirection, Vector3.up).normalized;

                    segmentDirection = nextSegmentationDirection * Mathf.Sign(Vector3.Dot(segmentDirection, nextSegmentationDirection));
                }
            }
            // In case there is no changing the direction with the snapped instances and we have grid placement enabled.
            else if (IsGridPlacementActive)
            {
                if (placerMgr.GridHandler.TryGetCellPosition(finalPosition, out finalPosition))
                {
                    // Determine whether the vertical or the horizontal axis is the closest to the current segment direction
                    // And update the direction accordingly
                    float xDistance = Mathf.Abs(finalPosition.x - SegmentationInitialPosition.x);
                    float yDistance = Mathf.Abs(finalPosition.z - SegmentationInitialPosition.z);

                    if (xDistance > yDistance)
                        finalPosition.z = SegmentationInitialPosition.z;
                    else
                        finalPosition.x = SegmentationInitialPosition.x;

                    segmentDirection = (finalPosition - SegmentationInitialPosition).normalized;
                }
            }
            // Finally calculate the resulting rotation from the segments direction
            Quaternion segmentRotation = Quaternion.LookRotation(segmentDirection);

            // Holds the length of the previous segment lengths
            float accumSegmentLength = 0;


            Int2D currFirstArea = current.firstInstance.PlacerComponent.GridOptions.area;

            for (int index = 0; index < current.SegmentCount; index++)
            {
                IBuilding instance = current.instances[index];

                // Update the next segment position according to length and update the Y position to fit on the placable terrain
                Vector3 nextInstancePosition = SegmentationInitialPosition;

                // if the first instance is one that has been snapped
                // we can check whether the first instance has changed from the original one and adjust the starting position of the segments accordingly
                if (IsGridPlacementActive
                    && current.firstInstance.IsPlacementInstance
                    && (firstSegmentArea.x != currFirstArea.x
                            || firstSegmentArea.y != currFirstArea.y))
                {
                    if (current.firstInstance.PlacerComponent.GridOptions.IsHorizontal)
                        nextInstancePosition.z += (segmentDirection.z > 0.0f ? 1 : -1) * (current.firstInstance.PlacerComponent.SegmentData.segmentLength - firstSegmentLength) / 2.0f;
                    else
                        nextInstancePosition.x += (segmentDirection.x > 0.0f ? 1 : -1) * (current.firstInstance.PlacerComponent.SegmentData.segmentLength - firstSegmentLength) / 2.0f;
                }

                // Add only half of the segment length for the initial segment
                // And for each subsequent segment, add half of its length before updating its position
                accumSegmentLength += instance.PlacerComponent.SegmentData.segmentLength / 2.0f;

                if (index > 0)
                {
                    // Then for segments past the initial one, we use the accumulated segment length + half of its segment length to position it
                    // Using half segment lengths here because conditions for building segment prefabs include having the transform pivot point on the botton center
                    // as well as having the parent prefab object's scale set to (1,1,1)
                    nextInstancePosition += (segmentDirection * accumSegmentLength);
                    // Then add the other half of the segment length so it is considered for positioning the next segment
                    accumSegmentLength += instance.PlacerComponent.SegmentData.segmentLength / 2.0f;
                }

                // In case of grid placement, snap the segment position into the cell position
                if (IsGridPlacementActive)
                {
                    // when snapping the segment position, we either
                    // get the cell position from the position of the segment (whose pivot is in the bottom middle of the segment) if the first instance is not snapped and is a non-square shape
                    // or apply reverse pivoting if not
                    if (index != 0
                        && current.instances[index].IsPlacementInstance
                        && current.instances[index].PlacerComponent.GridOptions.area.x == current.instances[index].PlacerComponent.GridOptions.area.y)
                        placerMgr.GridHandler.TryGetCellPosition(nextInstancePosition, out nextInstancePosition);
                    else
                    {
                        nextInstancePosition = instance.PlacerComponent.GridOptions.ApplyPivotPointReverse(nextInstancePosition);
                    }
                }
                else
                {
                    terrainMgr.SampleHeight(instance.transform.position, instance.PlacerComponent.PlacableTerrainAreas, out nextInstancePosition.y);
                    nextInstancePosition.y += placerMgr.BuildingPositionYOffset;
                }

                if (!instance.IsPlacementInstance)
                {
                    // Check if position still matches the snapped one.
                    Vector3 expectedPosition = IsGridPlacementActive
                        ? instance.PlacerComponent.GridOptions.ApplyPivotPoint(nextInstancePosition)
                        : nextInstancePosition;

                    if(Vector3.Distance(expectedPosition, instance.transform.position) > segmentUnsnapRange)
                    {
                        // Unsnap
                        RevertSnappedInstance(index, nextInstancePosition, segmentRotation);
                    }
                    continue;
                }
                
                // Update segment rotation according to direction
                instance.transform.rotation = segmentRotation;

                // Check if the individual segment can be placed in this new position
                instance.PlacerComponent.OnPlacementUpdate(nextInstancePosition);
            }
        }

        #endregion

        #region Helper Functions
        // Deletes a segment from the current placement process without replacing it or clearing its slot.
        // This is intended to be used as a helper function prior to replacing the segment slot with a snapped target building or a new segment type.
        private bool DeleteCurrentSegment(int index)
        {
            if (!current.IsValidSegmentIndex(index))
                return false;

            if (reservePlacementResources)
                gameMgr.GetService<IResourceManager>().ReleaseResources(
                    current.instanceTasks[index].RequiredResources,
                    current.instanceTasks[index].FactionID
                );

            IBuilding replacedInstance = current.instances[index];

            current.instanceTasks[index].OnCancel();

            if (replacedInstance.IsValid())
                replacedInstance.Health.DestroyLocal(false, null);

            globalEvent.RaiseBuildingPlacementStopGlobal(replacedInstance);

            current.instanceTasks[index] = null;
            current.instances[index] = null;

            return true;
        }
        
        #endregion
    }
}
