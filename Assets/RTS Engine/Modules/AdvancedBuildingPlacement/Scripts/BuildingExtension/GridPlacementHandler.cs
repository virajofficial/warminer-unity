using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public class PlacementGridCell
    {
        public Int2D Position { private set; get; }
        public bool IsOccupied { set; get; }
        public float Height { private set; get; }

        public PlacementGridCell(Int2D position, float height)
        {
            this.Position = position;
            this.IsOccupied = false;
            this.Height = height;
        }
    }

    [System.Serializable]
    public class GridPlacementHandler : MonoBehaviour, IGridPlacementHandler
    {
        //[Header("Grid Placement")]
        [SerializeField, Tooltip("When enabled, the buildings are placed in a grid-style on the placable terrain areas")]
        private bool isEnabled = true;
        public bool IsEnabled => isEnabled;

        public int CellSize => 1;

        private Dictionary<Int2D, PlacementGridCell> gridDict;

        protected IGameManager gameMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.placerMgr = gameMgr.GetService<IBuildingPlacement>(); 

            if (!IsEnabled)
                return;

            gridDict = new Dictionary<Int2D, PlacementGridCell>();

            for(int x = terrainMgr.HeightCacheLowerLeftCorner.x; x < terrainMgr.HeightCacheUpperRightCorner.x; x += CellSize)
                for (int y = terrainMgr.HeightCacheLowerLeftCorner.y; y < terrainMgr.HeightCacheUpperRightCorner.y; y += CellSize)
                {
                    Int2D nextPosition = new Int2D
                    {
                        x = x,
                        y = y
                    };

                    if (terrainMgr.SampleHeight(new Vector3(nextPosition.x, 0.0f, nextPosition.y), placerMgr.PlacableTerrainAreas, out float height))
                    {
                        gridDict.Add(nextPosition, new PlacementGridCell(nextPosition, height + placerMgr.BuildingPositionYOffset));
                    }
                }

            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;
            globalEvent.BuildingDeadGlobal += HandleBuildingDeadGlobal;
        }

        #region Handling Event: Building Dead / Placed
        private void HandleBuildingDeadGlobal(IBuilding building, DeadEventArgs args)
        {
            if (building.IsPlacementInstance)
                return;

            UpdateCells(
                occupy: false,
                area: building.PlacerComponent.GridOptions.area,
                isHorizontal: building.PlacerComponent.GridOptions.IsHorizontal,
                worldPosition: building.PlacerComponent.GridOptions.ApplyPivotPointReverse(building.transform.position));
        }

        private void HandleBuildingPlacedGlobal(IBuilding building, EventArgs args)
        {
            UpdateCells(
                occupy: true,
                area: building.PlacerComponent.GridOptions.area,
                isHorizontal: building.PlacerComponent.GridOptions.IsHorizontal,
                worldPosition: building.PlacerComponent.GridOptions.ApplyPivotPointReverse(building.transform.position));
        }
        #endregion

        private void UpdateCells(bool occupy, Int2D area, bool isHorizontal, Vector3 worldPosition)
        {
            if (!TryGetCellPosition(worldPosition, out Int2D lowerLeftCellPosition))
            {
                // ERROR?
                return;
            }

            UpdateCells(occupy, area, isHorizontal, lowerLeftCellPosition);
        }

        private void UpdateCells(bool occupy, Int2D area, bool isHorizontal, Int2D lowerLeftCellPosition)
        {
            for (int i = 0; i < area.x; i++)
            {
                for (int j = 0; j < area.y; j++)
                {
                    if (gridDict.TryGetValue(
                        new Int2D {
                        x = (int)lowerLeftCellPosition.x + (isHorizontal ? i : j),
                        y = (int)lowerLeftCellPosition.y + (isHorizontal ? j : i) 
                        },
                        out PlacementGridCell cell))
                        cell.IsOccupied = occupy;
                }
            }
        }

        public bool TryGetCellPosition(Int2D position, out Vector3 worldPosition)
            => TryGetCellPosition(new Vector2(position.x, position.y), out worldPosition);
        public bool TryGetCellPosition (Vector3 position, out Vector3 worldPosition)
        {
            if (TryGetCellPosition(position, out Int2D cellPosition) && gridDict.ContainsKey(cellPosition))
            {
                worldPosition = new Vector3(cellPosition.x, gridDict[cellPosition].Height, cellPosition.y);
                return true;
            }
            else
            {
                worldPosition = position;
                return false;
            }
        }

        public bool TryGetCellPosition (Vector3 position, out Int2D cellPosition)
        {
            Vector3 clampedPosition = new Vector3(
                Mathf.Clamp(position.x, terrainMgr.HeightCacheLowerLeftCorner.x, terrainMgr.HeightCacheUpperRightCorner.x),
                position.y,
                Mathf.Clamp(position.z, terrainMgr.HeightCacheLowerLeftCorner.y, terrainMgr.HeightCacheUpperRightCorner.y));

            // Find the coordinates of the potential search cell where the input position is in
            cellPosition = new Int2D
            {
                x = ( ((int)clampedPosition.x - terrainMgr.HeightCacheLowerLeftCorner.x) / CellSize) * CellSize + terrainMgr.HeightCacheLowerLeftCorner.x,
                y = ( ((int)clampedPosition.z - terrainMgr.HeightCacheLowerLeftCorner.y) / CellSize) * CellSize + terrainMgr.HeightCacheLowerLeftCorner.y
            };

            return gridDict.ContainsKey(cellPosition);
        }

        public bool IsOccupied(Vector3 worldPosition)
        {
            if (!TryGetCellPosition(worldPosition, out Int2D cellPosition))
                return false;

            return IsOccupied(cellPosition);
        }

        public bool IsOccupied(Int2D cellPosition)
        {
            return gridDict.TryGetValue(cellPosition, out PlacementGridCell cell)
                && cell.IsOccupied;
        }

        public bool IsOccupied(Int2D area, Int2D bottomLeftCellPosition, bool horizontal)
        {
            for (int i = 0; i < area.x; i++)
            {
                for (int j = 0; j < area.y; j++)
                {
                    if (IsOccupied(new Int2D
                    {
                        x = bottomLeftCellPosition.x + (horizontal ? i : j),
                        y = bottomLeftCellPosition.y + (horizontal ? j : i)
                    }
                    ))
                        return true;
                }
            }

            return false;
        }

        public bool IsOccupied(Int2D area, Vector3 bottomLeftWorldPosition, bool horizontal)
        {
            return TryGetCellPosition(bottomLeftWorldPosition, out Int2D lowerLeftCellPosition)
                && IsOccupied(area, lowerLeftCellPosition, horizontal); 
        }
    }

}

