using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Terrain;
using RTSEngine.Event;
using RTSEngine.Entities;

// See this for https://matteolopiccolo.medium.com/math-in-unity-create-a-graphic-grid-with-cartesian-plane-and-line-renderer-part-i-4bf15c4ba1e2
// Drawing Grid using LineRenderer

namespace RTSEngine.BuildingExtension
{

    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GlobalGridPlacementMesh : MonoBehaviour, IGridPlacementMesh 
    {
        #region Attributes
        [SerializeField, Tooltip("Assign the material of the grid mesh for free cells. When not assigned, the default material will be used.")]
        private Material freeMaterial = null;
        [SerializeField, Tooltip("Assign the material of the grid mesh for occupied cells. When not assigned, the default material will be used.")]
        private Material occupiedMaterial = null;

        [SerializeField, Tooltip("The color assigned to the material of the grid mesh when a cell is free for placement.")]
        private Color freeColor = Color.green;
        [SerializeField, Tooltip("The color assigned to the material of the grid mesh when a cell is occupied and does not allow for placement.")]
        private Color occupiedColor = Color.red;

        [SerializeField, Tooltip("Enable this field to have the grid mesh only visible when the local player is placing buildings.")]
        private bool displayOnLocalPlayerPlacementOnly = true;

        // Mesh
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        // Services
        protected IGameManager gameMgr { private set; get; } 
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }

        // Mesh data
        private List<Vector3> verticies;
        private List<int> freeIndices;
        private List<int> occupiedIndices;
        // lower-left corners of each valid grid cell
        private List<Vector3> positions;
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();

            this.gameMgr = gameMgr;
            placerMgr = this.gameMgr.GetService<IBuildingPlacement>();

            if (!placerMgr.GridHandler.IsValid()
                || !placerMgr.GridHandler.IsEnabled)
                return;

            terrainMgr = this.gameMgr.GetService<ITerrainManager>();
            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();

            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;
            globalEvent.BuildingDeadGlobal += HandleBuildingDeadGlobal;

            if (displayOnLocalPlayerPlacementOnly)
            {
                globalEvent.BuildingPlacementStartGlobal += HandleBuildingPlacementStartGlobal;
                globalEvent.BuildingPlacementResetGlobal += HandleBuildingPlacementResetGlobal;
            }

            GenerateMeshData();
            GenerateGridMesh();

            Toggle(!displayOnLocalPlayerPlacementOnly);
        }

        private void OnDestroy()
        {
            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacedGlobal;
            globalEvent.BuildingDeadGlobal -= HandleBuildingDeadGlobal;

            globalEvent.BuildingPlacementStartGlobal -= HandleBuildingPlacementStartGlobal;
            globalEvent.BuildingPlacementResetGlobal -= HandleBuildingPlacementResetGlobal;
        }
        #endregion

        #region Handling Events: Building / Placement
        private void HandleBuildingPlacementResetGlobal(IBuildingPlacementHandler placementHandler, EventArgs args)
        {
            if (placementHandler.FactionSlot.IsLocalPlayerFaction())
                Toggle(false);
        }

        private void HandleBuildingPlacementStartGlobal(IBuilding building, EventArgs args)
        {
            if (building.IsLocalPlayerFaction())
                Toggle(true);
        }

        private void HandleBuildingDeadGlobal(IBuilding building, DeadEventArgs args)
        {
            if (building.IsPlacementInstance)
                return;

            GenerateGridMesh();
        }

        private void HandleBuildingPlacedGlobal(IBuilding building, EventArgs args)
        {
            GenerateGridMesh();
        }
        #endregion

        #region Generating/Updating Mesh Data
        private void GenerateMeshData()
        {
            int cellSize = placerMgr.GridHandler.CellSize;

            positions = new List<Vector3>();

            for (int x = terrainMgr.HeightCacheLowerLeftCorner.x; x < terrainMgr.HeightCacheUpperRightCorner.x; x += cellSize)
                for (int y = terrainMgr.HeightCacheLowerLeftCorner.y; y < terrainMgr.HeightCacheUpperRightCorner.y; y += cellSize)
                {
                    Vector3 position = new Vector3(x, 0.0f, y);
                    if (!terrainMgr.SampleHeight(position, placerMgr.PlacableTerrainAreas, out float height))
                        continue;

                    height += placerMgr.BuildingPositionYOffset;
                    position.y = height;

                    positions.Add(position);
                }
        }

        private void GenerateGridMesh()
        {
            var mesh = new Mesh();

            verticies = new List<Vector3>();

            int cellSize = placerMgr.GridHandler.CellSize;

            freeIndices = new List<int>();
            occupiedIndices = new List<int>();

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 position = positions[i];

                verticies.Add(new Vector3(position.x, position.y, position.z + cellSize));
                verticies.Add(new Vector3(position.x + cellSize, position.y, position.z + cellSize));
                verticies.Add(new Vector3(position.x, position.y, position.z));
                verticies.Add(new Vector3(position.x + cellSize, position.y, position.z));

                var nextIndices = placerMgr.GridHandler.IsOccupied(position)
                    ? occupiedIndices
                    : freeIndices;

                nextIndices.Add(4 * i + 0);
                nextIndices.Add(4 * i + 1);
                nextIndices.Add(4 * i + 2);
                nextIndices.Add(4 * i + 2);
                nextIndices.Add(4 * i + 1);
                nextIndices.Add(4 * i + 3);
            }

            mesh.subMeshCount = 2;
            mesh.vertices = verticies.ToArray();

            mesh.SetIndices(occupiedIndices.ToArray(), MeshTopology.Lines, 0);
            mesh.SetIndices(freeIndices.ToArray(), MeshTopology.Lines, 1);

            meshFilter.mesh = mesh;

            meshRenderer.materials = new Material[2];

            meshRenderer.materials[0] = occupiedMaterial.IsValid() ? occupiedMaterial : new Material(Shader.Find("Sprites/Default"));
            meshRenderer.materials[0].color = occupiedColor;

            meshRenderer.materials[1].color = freeColor;
            meshRenderer.materials[1] = freeMaterial.IsValid() ? freeMaterial : new Material(Shader.Find("Sprites/Default"));
        }
        #endregion

        #region Toggling Grid Mesh
        public void Toggle(bool enable)
        {
            meshRenderer.enabled = enable;
        }
        #endregion
    }
}
