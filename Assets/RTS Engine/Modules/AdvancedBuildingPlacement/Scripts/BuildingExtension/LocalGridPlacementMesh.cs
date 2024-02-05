using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public class LocalGridPlacementMesh : MonoBehaviour, IPostRunGameService
    {
        #region Data Types
        [System.Serializable]
        public struct CellRendererData
        {
            public Material material;
            public int materialID;
            public Color freeColor;
            public Color occupiedColor;
        }

        public struct ActiveCellRenderer
        {
            public MeshRenderer meshRenderer;
            public IEffectObject effectObject;
            public IBuilding targetBuilding;
            public bool isInnerArea;
        }

        public class BuildingPlacementGridMeshData
        {
            public bool enabled;
            public IBuilding targetBuilding;
            public List<List<ActiveCellRenderer>> cellRenderers;
        }
        #endregion

        #region Attributes
        [SerializeField, Tooltip("The range of grid cells past the defined area for the placement building instance to show the status of (occupied or free).")]
        private int range = 5;

        [SerializeField, Tooltip("An effect object (so that it is poolable) with a MeshRenderer component that represents one cell grid placement cell (make sure its size matches the current cell size)."), EnforceType(typeof(MeshRenderer), prefabOnly: true)]
        private GameObjectToEffectObjectInput singleCellRendererPrefab = null;

        [SerializeField, Tooltip("Disable the placement building instance's selection marker/renderer when showing the local grid placement mesh.")]
        private bool disableSelectionMarker = true;

        public bool IsEnabled { private set; get; }
        private List<BuildingPlacementGridMeshData> data;
        private Dictionary<IBuilding, int> placementBuildingToDataIndex;
        private int enabledDataCount;
        private Dictionary<Int2D, ActiveCellRenderer> cellRenderersPositions;

        [SerializeField, Tooltip("Material and color assigned to the cell renderers of the placement building area.")]
        private CellRendererData innerAreaCellData = new CellRendererData { freeColor = Color.green, occupiedColor = Color.red };

        [SerializeField, Tooltip("Material and color assigned to the cell renderers of the placement building area.")]
        private CellRendererData outerAreaCellData = new CellRendererData { freeColor = Color.grey, occupiedColor = Color.yellow };

        // Services
        protected IGameManager gameMgr { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Teraminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            if(!singleCellRendererPrefab.IsValid()
                || !singleCellRendererPrefab.Output.gameObject.GetComponent<MeshRenderer>())
            {
                logger.LogError($"[{GetType().Name}] The field 'Single Cell Renderer Prefab' effect object field must be assigned to an object with a MeshRenderer component!");
                return;
            }

            placementBuildingToDataIndex = new Dictionary<IBuilding, int>();
            data = new List<BuildingPlacementGridMeshData>();
            enabledDataCount = 0;

            cellRenderersPositions = new Dictionary<Int2D, ActiveCellRenderer>();

            this.gameMgr = gameMgr;
            placerMgr = this.gameMgr.GetService<IBuildingPlacement>();

            if (!placerMgr.GridHandler.IsValid()
                || !placerMgr.GridHandler.IsEnabled)
                return;

            terrainMgr = this.gameMgr.GetService<ITerrainManager>();
            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();

            globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal += HandleBuildingPlacementStopGlobal;
        }

        private void OnDisable()
        {
            if (!placerMgr.GridHandler.IsValid()
                || !placerMgr.GridHandler.IsEnabled)
                return;

            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementStopGlobal -= HandleBuildingPlacementStopGlobal;
        }
        #endregion

        #region Handling Events: Building Placement
        private void HandleBuildingPlacementStopGlobal(IBuilding building, EventArgs args)
        {
            if (building.IsLocalPlayerFaction())
            {
                Toggle(false, building);

            }

        }

        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs args)
        {
            if (building.IsLocalPlayerFaction())
                Toggle(true, building);
        }
        #endregion

        #region Grid Placement Mesh: Enabling/Disabling, Tracking
        private void Toggle(bool enable, IBuilding building)
        {
            if (!building.IsValid())
            {
                logger.LogError($"[{GetType().Name}] ");
                IsEnabled = false;

                return;
            }

            int index = 0;

            // Enabling
            if (enable)
            {
                // Try to get an already created slot but a disabled one (to avoid adding new elements to the data list and better handle garbage collection)
                BuildingPlacementGridMeshData nextData = new BuildingPlacementGridMeshData();
                while (index < data.Count)
                {
                    if(data[index].enabled)
                    {
                        index++;
                        continue;
                    }

                    nextData = data[index];
                    nextData.targetBuilding = building;
                    nextData.enabled = true;
                    break;
                }
                if(index == data.Count)
                {
                    nextData = new BuildingPlacementGridMeshData
                    {
                        enabled = true,
                        targetBuilding = building,
                        cellRenderers = new List<List<ActiveCellRenderer>>()
                    };
                    data.Add(nextData);
                    enabledDataCount++;
                }

                if (disableSelectionMarker)
                {
                    nextData.targetBuilding.SelectionMarker.Disable();
                    nextData.targetBuilding.SelectionMarker.IsLocked = true;
                }

                placerMgr.GridHandler.TryGetCellPosition(
                    nextData.targetBuilding.PlacerComponent.GridOptions.ApplyPivotPointReverse(
                        nextData.targetBuilding.transform.position,
                        GridAreaPivotPoint.bottomLeft),
                    out Int2D buildingLowerLeftCornerPosition);

                Int2D area = nextData.targetBuilding.PlacerComponent.GridOptions.area;

                for (int i = -range; i < area.x + range; i++)
                {
                    nextData.cellRenderers.Add(new List<ActiveCellRenderer>());

                    for (int j = -range; j < area.y + range; j++)
                    {
                        IEffectObject nextCellEffect = effectObjPool.Spawn(
                            singleCellRendererPrefab.Output,
                            new EffectObjectSpawnInput(
                                parent: null,
                                useLocalTransform: false,
                                spawnPosition: Vector3.zero,
                                spawnRotation: Quaternion.identity,
                                enableLifeTime: false));

                        nextData.cellRenderers[i + range].Add(new ActiveCellRenderer
                        {
                            effectObject = nextCellEffect,
                            meshRenderer = nextCellEffect.gameObject.GetComponent<MeshRenderer>(),
                            targetBuilding = building,
                            isInnerArea = IsCellIndexInnerArea(i, j, area)
                        });

                        // inner area vs outer area data
                        CellRendererData nextRendererData = (i >= 0 && i < area.x && j >= 0 && j < area.y)
                                ? innerAreaCellData
                                : outerAreaCellData;

                        nextData.cellRenderers[i + range][j + range].meshRenderer.materials[nextRendererData.materialID] = nextRendererData.material.IsValid()
                                ? nextRendererData.material : new Material(Shader.Find("Sprites/Default"));
                    }
                }

                nextData.targetBuilding.PlacerComponent.BuildingPlacementTransformUpdated += HandleBuildingPlacementTransformUpdated;
                placementBuildingToDataIndex.Add(nextData.targetBuilding, index);

                IsEnabled = true;

                HandleBuildingPlacementTransformUpdated(building, EventArgs.Empty);
            }
            // Disabling
            else
            {
                while (index < data.Count)
                {
                    BuildingPlacementGridMeshData nextData = data[index];

                    if (!nextData.enabled || nextData.targetBuilding != building)
                    {
                        index++;
                        continue;
                    }

                    Int2D area = nextData.targetBuilding.PlacerComponent.GridOptions.area;
                    for (int i = -range; i < area.x + range; i++)
                    {
                        for (int j = -range; j < area.y + range; j++)
                        {
                            if (placerMgr.GridHandler.TryGetCellPosition(
                                nextData.cellRenderers[i + range][j + range].effectObject.transform.position,
                                out Int2D cellPosition))
                                cellRenderersPositions.Remove(cellPosition);

                            nextData.cellRenderers[i + range][j + range].effectObject.Deactivate(useDisableTime: false);
                        }
                    }

                    nextData.cellRenderers.Clear();
                    nextData.enabled = false;

                    nextData.targetBuilding.PlacerComponent.BuildingPlacementTransformUpdated -= HandleBuildingPlacementTransformUpdated;
                    placementBuildingToDataIndex.Remove(nextData.targetBuilding);
                    nextData.targetBuilding = null;

                    enabledDataCount--;
                    break;
                }

                if (enabledDataCount == 0)
                    IsEnabled = false;
            }
        }

        private void HandleBuildingPlacementTransformUpdated(IBuilding targetBuilding, EventArgs args)
        {
            if (!targetBuilding.IsLocalPlayerFaction())
                return;

            BuildingPlacementGridMeshData nextData = data[placementBuildingToDataIndex[targetBuilding]];

            placerMgr.GridHandler.TryGetCellPosition(
                nextData.targetBuilding.PlacerComponent.GridOptions.ApplyPivotPointReverse(
                    nextData.targetBuilding.transform.position,
                    GridAreaPivotPoint.bottomLeft),
                out Int2D buildingLowerLeftCornerPosition);

            Int2D area = nextData.targetBuilding.PlacerComponent.GridOptions.area;

            // Remove the positions of the current cell renderers from the last building placement position
            for (int i = -range; i < area.x + range; i++)
            {
                for (int j = -range; j < area.y + range; j++)
                {
                    if (placerMgr.GridHandler.TryGetCellPosition(
                        nextData.cellRenderers[i + range][j + range].effectObject.transform.position,
                        out Int2D cellPosition) && cellRenderersPositions.TryGetValue(cellPosition, out ActiveCellRenderer nextCellRenderer))
                    {
                        // Check for the target building of cell renderer so we know the cell with that position belongs to the target building we are currently handling
                        if(nextCellRenderer.targetBuilding == targetBuilding)
                            cellRenderersPositions.Remove(cellPosition);
                    }
                }
            }

            for (int i = -range; i < area.x + range; i++)
            {
                for (int j = -range; j < area.y + range; j++)
                {
                    IEffectObject nextCellEffect = nextData.cellRenderers[i + range][j + range].effectObject;

                    Vector3 cellWorldPosition = new Vector3(
                        buildingLowerLeftCornerPosition.x + (targetBuilding.PlacerComponent.GridOptions.IsHorizontal ? i : j) + placerMgr.GridHandler.CellSize / 2.0f,
                        0.0f,
                        buildingLowerLeftCornerPosition.y + (targetBuilding.PlacerComponent.GridOptions.IsHorizontal ? j : i) + placerMgr.GridHandler.CellSize / 2.0f);

                    bool isInnerArea = nextData.cellRenderers[i + range][j + range].isInnerArea;
                    bool isCellExist = placerMgr.GridHandler.TryGetCellPosition(cellWorldPosition, out Vector3 cellBottomLeftPosition);
                    Int2D nextCellPosition = new Int2D { x = (int)cellBottomLeftPosition.x, y = (int)cellBottomLeftPosition.z };

                    if (!isCellExist
                            || cellRenderersPositions.ContainsKey(nextCellPosition))
                    {
                        // if the current cell is an inner area one and there is one occupying its current position that is not an inner area one
                        // then disable the non-inner area cell that usually belongs to another placement instance
                        if (isCellExist && isInnerArea
                            && !cellRenderersPositions[nextCellPosition].isInnerArea)
                        {
                            cellRenderersPositions[nextCellPosition].effectObject.gameObject.SetActive(false);
                            cellRenderersPositions.Remove(nextCellPosition);
                        }
                        else
                        {
                            nextCellEffect.gameObject.SetActive(false);
                            continue;
                        }
                    }

                    // Add the new positions for the cell renderers so that no two cells will spawn on top of each other.
                    cellRenderersPositions.Add(nextCellPosition, nextData.cellRenderers[i + range][j + range]);

                    nextCellEffect.gameObject.SetActive(true);
                    cellWorldPosition.y = cellBottomLeftPosition.y;
                    nextCellEffect.transform.position = cellWorldPosition;

                    bool isOccupied;
                    CellRendererData nextRendererData;

                    if(isInnerArea)
                    {
                        nextRendererData = innerAreaCellData;
                        isOccupied = !nextData.targetBuilding.PlacerComponent.CanPlace;
                    }
                    else
                    {
                        nextRendererData = outerAreaCellData;
                        isOccupied = placerMgr.GridHandler.IsOccupied(cellWorldPosition);
                    }

                    nextData.cellRenderers[i + range][j + range].meshRenderer.materials[nextRendererData.materialID].color = isOccupied
                            ? nextRendererData.occupiedColor : nextRendererData.freeColor;
                }
            }
        }

        private bool IsCellIndexInnerArea(int i, int j, Int2D area) => (i >= 0 && i < area.x && j >= 0 && j < area.y);
        #endregion
    }
}
