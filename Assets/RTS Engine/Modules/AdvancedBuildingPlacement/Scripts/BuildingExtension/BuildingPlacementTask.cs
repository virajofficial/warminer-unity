using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.ResourceExtension;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    [System.Serializable]
    public class BuildingPlacementTask : IBuildingPlacementTask
    {
        public bool IsInitialized { private set; get; } = false;

        [SerializeField, EnforceType(typeof(IBuilding)), Tooltip("Prefab of the building type.")]
        protected GameObject prefabObject = null;
        public IBuilding TargetObject { private set; get; }

        [Space(), SerializeField, Tooltip("Resources required to launch the building's placement.")]
        protected ResourceInput[] requiredResources = new ResourceInput[0];
        public IReadOnlyList<ResourceInput> RequiredResources => requiredResources;
        [Space(), SerializeField, Tooltip("Input the faction units/buildings required to create this faction entity.")]
        protected FactionEntityRequirement[] factionEntityRequirements = new FactionEntityRequirement[0];
        public IReadOnlyList<FactionEntityRequirement> FactionEntityRequirements => factionEntityRequirements;

        public int FactionID { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }

        public void Init(IGameManager gameMgr, int factionID)
        {
            if (!prefabObject.IsValid())
                return;

            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();
            this.resourceMgr = this.gameMgr.GetService<IResourceManager>();

            TargetObject = prefabObject.GetComponent<IBuilding>();
            FactionID = factionID;

            IsInitialized = true;
        }

        public virtual ErrorMessage CanComplete()
        {
            if (!IsInitialized)
                return ErrorMessage.inactive;
            else if (!RTSHelper.TestFactionEntityRequirements(factionEntityRequirements, gameMgr.GetFactionSlot(FactionID).FactionMgr))
                return ErrorMessage.taskMissingFactionEntityRequirements;
            else if (!resourceMgr.HasResources(requiredResources, FactionID))
                return ErrorMessage.taskMissingResourceRequirements;

            return ErrorMessage.none;
        }

        //what happens regarding the task requirements when an instance of this task is launched?
        public virtual void OnComplete()
        {
            if (!IsInitialized)
                logger.LogError($"[{GetType().Name}] Component must be initialized before it can be used!");

            resourceMgr.UpdateResource(FactionID, requiredResources, add: false);
        }

        public virtual ErrorMessage CanStart() => CanComplete();

        public virtual void OnStart()
        {
        }

        public virtual void OnCancel() 
        {
        }
    }
}
