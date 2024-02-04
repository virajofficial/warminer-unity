using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Demo
{
    public class BuildingPlacementMaterialHandler : MonoBehaviour, IEntityPreInitializable, IMonoBehaviour
    {
        [System.Serializable]
        private struct PlacementMaterialData
        {
            public Renderer renderer;

            public Material material;
            public int materialID;
        }

        #region Attributes
        private IBuilding building;

        [SerializeField]
        private PlacementMaterialData[] placementMaterials = new PlacementMaterialData[0];
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            building = entity as IBuilding;

            if (!building.IsValid())
            {
                // ERROR
                return;
            }

            if(!building.IsPlacementInstance)
                return;

            foreach(PlacementMaterialData data in placementMaterials)
            {
                // ERROR HANDLING FOR INVALID INPUT
                if (!data.renderer.IsValid()
                    || !data.materialID.IsValidIndex(data.renderer.materials))
                    continue;

                data.renderer.materials[data.materialID] = data.material;
            }
        }

        public void Disable()
        {
        }
        #endregion
    }
}
