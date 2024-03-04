using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.EntityComponent;

namespace RTSEngine.Health
{
    public class ResourceHealth : EntityHealth, IResourceHealth
    {
        #region Attributes
        public IResource Resource { private set; get; }
        public override EntityType EntityType => EntityType.resource;

        [SerializeField, Tooltip("Transitional state activated when the first is collected for the first time.")]
        private EntityHealthState collectedState = new EntityHealthState();
        private bool collected = false;
        private int warehouseLoad = 30;
        #endregion

        #region Initializing/Terminating
        protected override void OnEntityHealthInit()
        {
            Resource = Entity as IResource;

            // If the health can not be decreased, meaning that the resource has infinite amount/health
            // Then lock the health value from being updated in the AddLocal method, check the LockHealth definition for more info.
            if (!CanDecrease)
                LockHealth = true;

            collected = false;
        }

        protected override void OnInitialHealthAdded()
        {
            stateHandler.Reset(States, CurrHealth);
        }
        #endregion

        #region Updating Health
        // Allow addition method to pass even if CanDecrease is set to false while updateValue < 0
        // That just means that the resource has infinite amount/health and we make sure that the health does not get decreased by having LockHealth = true on Init.
        public override ErrorMessage CanAdd(HealthUpdateArgs args)
        {
            if (IsDead)
                return ErrorMessage.healthDead;
            if (args.Value > 0 && !CanIncrease)
                return ErrorMessage.healthNoIncrease;

            return ErrorMessage.none;
        }

        protected override void OnHealthUpdated(HealthUpdateArgs args)
        {
            // If the resource hasn't been collected before now, activate the collected state. This is a unique behaviour for resources.
            if (!collected && args.Value < 0)
            {
                collected = true;
                stateHandler.Activate(collectedState);
            }

            globalEvent.RaiseResourceHealthUpdatedGlobal(Resource, args);
        }
        #endregion

        #region Destroying Resource
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseResourceDeadGlobal(Resource, new DeadEventArgs(upgrade, source, DestroyObjectDelay));
        }
        #endregion

        public void resourceUnloaded(int amount, string resourceName)
        {
            Debug.Log("resource unloaded = " + amount + $"({transform.gameObject.name})");
            CurrHealth += amount;
            if(CurrHealth >= warehouseLoad)
            {
                Debug.Log("Resource name = " + resourceName);
                foreach(Transform child in UnityEngine.Object.FindObjectsOfType<Transform>())
                {
                    if (child.name.Split('_')[0] == "warehouse" && child.name.Split('_')[1] == resourceName.ToLower() 
                        && child.GetComponent<Building>().FactionID == transform.GetComponent<ResourceBuilding>().FactionID)
                    {
                        Debug.Log("Targe Names: " + child.name);
                        child.GetChild(0).GetComponent<AITugController>().targetTransform = transform.GetChild(0);
                        child.GetChild(0).GetComponent<AITugController>().isFlying = true;
                    }
                }
                CurrHealth -= warehouseLoad;
                //gameMgr.FactionSlots[GetComponent<ResourceBuilding>().FactionID].initi
            }

        }
    }
}
