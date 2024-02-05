using RTSEngine.BuildingExtension;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Demo
{
    public class GateHeightModifier : MonoBehaviour, IEntityPreInitializable, IMonoBehaviour
    {
        public IEntity Entity { get; private set; }

        [SerializeField]
        private Transform gateObject = null;

        [SerializeField]
        private float closedHeight = 0.0f;

        [SerializeField]
        private float openHeight = -1.0f;

        [SerializeField]
        private float smoothTime = 0.3f;
        private float targetHeight;
        private float currVelocity;

        private IBuildingGateHandler gateHandler;

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            Entity = entity;
            gateHandler = entity.GetComponentInChildren<IBuildingGateHandler>();

            gateHandler.GateToggled += HandleGateToggled;

            HandleGateToggled(gateHandler, EventArgs.Empty);

            if (!gateObject.IsValid())
                enabled = false;
        }

        public void Disable()
        {
            gateHandler.GateToggled += HandleGateToggled;
        }

        private void HandleGateToggled(IBuildingGateHandler sender, EventArgs args)
        {
            currVelocity = 0.0f;

            targetHeight = gateHandler.IsOpen
                ? openHeight
                : closedHeight;
        }

        private void Update()
        {
            if(!gateObject.IsValid()
                || !Entity.CanLaunchTask)
                return;

            Vector3 nextPosition = gateObject.localPosition;
            nextPosition.y = Mathf.SmoothDamp(nextPosition.y, targetHeight, ref currVelocity, smoothTime);
            gateObject.localPosition = nextPosition;
        }
    }
}
