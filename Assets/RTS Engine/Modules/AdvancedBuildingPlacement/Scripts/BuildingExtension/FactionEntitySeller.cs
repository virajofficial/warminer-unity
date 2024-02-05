using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.UI;
using RTSEngine.ResourceExtension;
using RTSEngine.Entities;

namespace RTSEngine.BuildingExtension
{
    public class FactionEntitySeller : EntityComponentBase
    {
        #region Attributes
        [SerializeField, Tooltip("Task data that the local player can click on to sell the faction entity.")]
        private EntityComponentTaskUIAsset taskUI = null;

        [SerializeField, Tooltip("Resources to award the faction that owns the faction entity when it is sold.")]
        private ResourceInput[] sellResources = new ResourceInput[0];

        // Services
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            if (!Entity.IsFactionEntity())
            {
                Debug.LogError($"[EntitySeller] This component can only be attached to unit or building entities!", gameObject);
                return;
            }

            resourceMgr = gameMgr.GetService<IResourceManager>();
        }
        #endregion

        #region Handling UI
        protected override bool OnTaskUICacheUpdate(List<EntityComponentTaskUIAttributes> taskUIAttributesCache, List<string> disabledTaskCodesCache)
        {
            return RTSHelper.OnSingleTaskUIRequest(
                this,
                taskUIAttributesCache,
                disabledTaskCodesCache,
                taskUI,
                enforceCanLaunchTask: !Entity.IsBuilding(), // Only completely built buildings have CanLaunchTask = true
                showCondition: (!Entity.IsBuilding() || !(Entity as IBuilding).IsPlacementInstance)); // But we want to allow non-built buildings to be sellable
        }

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (taskUI.IsValid() && taskAttributes.data.code == taskUI.Key)
            {
                Sell();
                return true;
            }

            return false;
        }

        public void Sell()
        {
            if (!Entity.IsValid() || !Entity.IsFactionEntity() || Entity.IsFree)
                return;
 
            resourceMgr.UpdateResource(Entity.FactionID, sellResources, add: true);
            Entity.Health.Destroy(upgrade: false, null);
        }
        #endregion
    }
}
