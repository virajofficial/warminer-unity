using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public class AdvancedBuildingPlacer : BuildingPlacer
    {
        #region Attributes
        [HideInInspector]
        public Int2D tabID;

        //[Header("Segmentation")]
        [SerializeField]
        private BuildingPlacementSegmentData segmentData = new BuildingPlacementSegmentData
        {
            enabled = false,
            minAmount = 2,
            segmentLength = 1.32f
        };
        public override BuildingPlacementSegmentData SegmentData => segmentData;

        private int segmentIndex = -1;

        [SerializeField, Tooltip("Enter the amount of segments (current active instances in the ) that are required to trigger replacement of segments by the ones assigned in the fields below. Must be bigger or equal to 0.When the value is set to 0, the temporary placement building instance that the player can drag through the map before starting the segmentation placement will be set to the first segment replacement instance in case it is set. "), Min(0)]
        private int minSegmentCountForReplacement = 3;

        [SerializeField, Tooltip("Assign this field to allow the first building in the segmentation mode instances to be replaced by a different building type other than this one.")]
        private BuildingPlacementTask firstSegmentReplacement = null;
        [SerializeField, Tooltip("Assign this field to allow the last building in the segmentation mode instances to be replaced by a different building type other than this one.")]
        private BuildingPlacementTask lastSegmentReplacement = null;

        [System.Serializable]
        public struct SegmentPeriodReplacementData
        {
            [Tooltip("Enable this field to allow segments to be replaced periodically.")]
            public bool enabled;
            [Tooltip("How often will segments be replaced?")]
            public int period;
            [Tooltip("The segment type that will replace the default one periodically.")]
            public BuildingPlacementTask targetSegment;
            [Tooltip("Enable this option to disable replacing segments that are next to the edges, even if the periodic conditions apply to them.")]
            public bool disableNextToEdges;
        }

        [SerializeField, Tooltip("Properties that allow to replace the segments periodically.")]
        private SegmentPeriodReplacementData segmentPeriodReplacement = new SegmentPeriodReplacementData
        {
            enabled = true,
            period = 3,
            disableNextToEdges = true
        };

        [SerializeField, Tooltip("Only assign this field in case this is not the default segment instance of the drag and drop placement mode. This allows the special segment to be reset to the default segment in case the conditions that allowed for the segment to exist are not there anymore.")]
        private BuildingPlacementTask defaultSegmentReplacement = null;

        [System.Serializable]
        public struct SegmentSnapData
        {
            [Tooltip("Enable to allow segment to snap into already placed buildings.")]
            public bool enabled;
            [Tooltip("Distance between the segment and the target placed building to snap to.")]
            public float range;
            [Tooltip("Define which placed buildings this segment can snap to.")]
            public CodeCategoryField targetOptions;
        }

        [SerializeField, Tooltip("Properties that allow to snap placement segments into placed building instances.")]
        private SegmentSnapData segmentSnapping = new SegmentSnapData
        {
            enabled = false,
            range = 0.2f,
            targetOptions = new CodeCategoryField()
        };

        //[Header("Grid Placement")]
        [SerializeField, Tooltip("In case grid placement is enabled, this field defines the segment data.")]
        private BuildingPlacementGridData gridOptions = new BuildingPlacementGridData
        {
            area = new Int2D(2, 2),
            pivotPoint = GridAreaPivotPoint.bottomLeft
        };

        public override BuildingPlacementGridData GridOptions => gridOptions;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit() 
        {
            firstSegmentReplacement.Init(gameMgr, Building.FactionID);
            lastSegmentReplacement.Init(gameMgr, Building.FactionID);
            defaultSegmentReplacement.Init(gameMgr, Building.FactionID);
            segmentPeriodReplacement.targetSegment.Init(gameMgr, Building.FactionID);

            gridOptions.Init(Building);
        }

        protected override void OnPlacedInit()
        {
            if (!placerMgr.GridHandler.IsValid()
                || !placerMgr.GridHandler.IsEnabled)
                return;

            if(!placerMgr.GridHandler.TryGetCellPosition(gridOptions.ApplyPivotPointReverse(Building.transform.position), out Vector3 worldPosition))
            {
                // Error
                return;
            }
            Building.transform.position = gridOptions.ApplyPivotPoint(worldPosition);
        }
        #endregion

        #region Handling Placement Status
        protected override void OnPlacementStarted(BuildingPlacementUpdateData data)
        {
            segmentIndex = data.sourceSegmentIndex;

            // If current placement is not in segmentation mode, nothing to do.
            if (data.segmentCount < minSegmentCountForReplacement)
                return;

            if (data.sourceSegmentIndex == 0) // First segment
            {
                if (firstSegmentReplacement.IsInitialized)
                {
                    placerMgr.Add(
                        Building.FactionID,
                        firstSegmentReplacement,
                        new BuildingPlacementOptions
                        {
                            isReplacingExisting = true,
                            targetIndex = 0
                        });
                    return;
                }
            }
            else if (data.sourceSegmentIndex == data.segmentCount - 1) // Last segment
            {
                if (lastSegmentReplacement.IsInitialized)
                {
                    placerMgr.Add(
                        Building.FactionID,
                        lastSegmentReplacement,
                        new BuildingPlacementOptions
                        {
                            isReplacingExisting = true,
                            targetIndex = data.segmentCount - 1
                        });
                    return;
                }
            }
            // Periodic segment replacement
            else if (segmentPeriodReplacement.enabled
                && segmentPeriodReplacement.targetSegment.IsInitialized
                && data.sourceSegmentIndex % segmentPeriodReplacement.period == 0)
            {
                if (!segmentPeriodReplacement.disableNextToEdges
                    || (data.sourceSegmentIndex != 1 && data.sourceSegmentIndex != data.segmentCount - 2))
                    placerMgr.Add(
                        Building.FactionID,
                        segmentPeriodReplacement.targetSegment,
                        new BuildingPlacementOptions
                        {
                            isReplacingExisting = true,
                            targetIndex = data.sourceSegmentIndex
                        });
            }
        }

        protected override void OnPlacementStopped(BuildingPlacementUpdateData data)
        {
            // To replace segments back to the default ones
            if (data.segmentCount == 0
                || !defaultSegmentReplacement.IsInitialized)
                return;

            // Minimum required segment count for replacement not reached -> back to default segments.
            if (data.segmentCount < minSegmentCountForReplacement)
            {
                // First building in the segmentation mode and there is a valid replacement
                if (data.sourceSegmentIndex == 0)
                {
                    placerMgr.Add(Building.FactionID,defaultSegmentReplacement,
                        new BuildingPlacementOptions
                        {
                            isReplacingExisting = true,
                            targetIndex = 0
                        });

                    return;
                }
                // Last segment
                else if (data.sourceSegmentIndex == data.segmentCount - 1)
                {
                    placerMgr.Add(Building.FactionID,defaultSegmentReplacement,
                        new BuildingPlacementOptions
                        {
                            isReplacingExisting = true,
                            targetIndex = data.segmentCount - 1
                        });

                    return;
                }
            }

            // Making sure segments next to the edges are the default ones, in case that is enabled.
            if (segmentPeriodReplacement.disableNextToEdges
                && data.sourceSegmentIndex % segmentPeriodReplacement.period == 0
                && (data.sourceSegmentIndex == 1 || data.sourceSegmentIndex == data.segmentCount - 2))
            {
                placerMgr.Add(Building.FactionID,defaultSegmentReplacement,
                    new BuildingPlacementOptions
                    {
                        isReplacingExisting = true,
                        targetIndex = data.sourceSegmentIndex
                    });

                return;
            }
        }
        #endregion

        #region Handling Position Update
        protected override void OnPositionUpdate(Vector3 position)
        {
            if (segmentSnapping.enabled)
            {
                IBuilding snappableBuilding = RTSHelper.GetClosestEntity(
                    Building.transform.position,
                    Building.FactionMgr.Buildings,
                    target =>
                        segmentSnapping.targetOptions.Contains(target)
                        && Vector3.Distance(target.transform.position, Building.transform.position) <= segmentSnapping.range);

                if (snappableBuilding.IsValid())
                {
                    if(placerMgr.Add(Building.FactionID, null,
                        new BuildingPlacementOptions
                        {
                            isSnappingSegment = true,
                            snapTarget = snappableBuilding,
                            targetIndex = segmentIndex
                        }) == true)
                        return;
                }
            }

            if (!placerMgr.GridHandler.IsValid()
                || !placerMgr.GridHandler.IsEnabled)
            {
                base.OnPositionUpdate(position);
                return;
            }

            // Grid Placement position update
            Building.transform.position = gridOptions.ApplyPivotPoint(position);
        }
        #endregion

        #region Displaying Grid Area
#if UNITY_EDITOR
        [Header("Gizmos")]
        [Min(1.0f)]
        public bool displayGridArea = true;
        public Color gridAreaColor = Color.blue;

        private void OnDrawGizmosSelected()
        {
            if (!displayGridArea)
                return;

            IEntity entity = GetComponentInParent<IEntity>();

            if (!entity.IsValid())
                return;

            Vector3 size = new Vector3(gridOptions.area.x, 0.0f, gridOptions.area.y);

            Gizmos.color = gridAreaColor;
            Gizmos.DrawWireCube(
                new Vector3(entity.transform.position.x, 0.0f, entity.transform.position.z),
                size);
        }
#endif
        #endregion
    }
}
