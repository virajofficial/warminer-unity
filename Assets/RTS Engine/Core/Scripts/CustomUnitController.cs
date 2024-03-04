using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using System;

public class CustomUnitController : MonoBehaviour, IEntityPreInitializable
{
    protected IGameManager GameMgr {  get; private set; }
    protected IEntity Entity { get; private set; }
    protected IUnit Unit { get; private set; }
    protected IResourceCollector ResCollector { get; private set; }
    protected IDropOffSource ResDropOff {  get; private set; }
    public void OnEntityPreInit(IGameManager GameMgr, IEntity Entity)
    {
        this.GameMgr = GameMgr;
        this.Entity = Entity;
        this.Unit = Entity as IUnit;

        this.ResCollector = Unit.CollectorComponent;
        this.ResDropOff = Unit.DropOffSource;

        // Resource Collector Events
        this.ResCollector.TargetUpdated += ResourceCollectorTargetUpdated;
        this.ResCollector.OnTargetMaxWorkerReached += ResourceWorkerMaxReached;

        // Resource Dropoff Events
        this.ResDropOff.ActiveStatusUpdate += StatusUpdatedForDropOff;

    }

    public void ResourceCollectorTargetUpdated(IEntityTargetComponent TargetRes,TargetDataEventArgs EventArgs)
    {
        // do stuff here when resource target changes
    }
    public void ResourceWorkerMaxReached(IResourceCollector ResCollector, SetTargetInputDataEventArgs EventArgs)
    {
        // When selected resource target has no worker space do something here...
    }

    public void StatusUpdatedForDropOff(IEntityComponent EntityComp, EventArgs EventArgs)
    {
        // do things when the drop off source status changes
    }

    public void Disable()
    {
        // Always remove your methods from the events stack helps keep your gc clear
        this.ResCollector.TargetUpdated -= ResourceCollectorTargetUpdated;
        this.ResCollector.OnTargetMaxWorkerReached -= ResourceWorkerMaxReached;
        this.ResDropOff.ActiveStatusUpdate -= StatusUpdatedForDropOff;
    }
}
