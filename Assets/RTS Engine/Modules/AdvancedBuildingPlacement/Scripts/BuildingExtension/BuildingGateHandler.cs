using System;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Logging;
using RTSEngine.UI;

using UnityEngine;
using UnityEngine.Events;

namespace RTSEngine.BuildingExtension
{
    public class BuildingGateHandler : EntityComponentBase, IBuildingGateHandler
    {
        #region Attributes
        public enum ActionType : byte { toggleGate = 0, setGateOpen = 1 }

        [SerializeField, Tooltip("The initial state of the gate when the building is initiailized.")]
        private bool isOpenOnInit = false;

        public bool IsOpen { private set; get; }

        private IEntityObstacleHandler obstacleHandler;

        [SerializeField, Tooltip("Defines information used to display a task in the task panel that toggles the gate status, when the building is selected.")]
        private EntityComponentTaskUIAsset toggleGateTaskUI = null;

        [SerializeField, Tooltip("Event invoked when the gate is open.")]
        private UnityEvent openEvent = new UnityEvent();
        [SerializeField, Tooltip("Event invoked when the gate is closed.")]
        private UnityEvent closeEvent = new UnityEvent();
        #endregion

        #region Raising Events
        public event CustomEventHandler<IBuildingGateHandler, EventArgs> GateToggled;
        private void RaiseGateToggled()
        {
            var handler = GateToggled;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            obstacleHandler = Entity.GetComponentInChildren<IEntityObstacleHandler>();

            if (!obstacleHandler.IsValid())
            {
                logger.LogError($"[{GetType().Name}] This component requires a component that implements the '{typeof(IEntityObstacleHandler).Name}' interface to be attached to the parent or child objects. For example, when using Unity's default Navmesh system, a suitable component would be the '{typeof(NavMeshEntityObstacle)}'.'", source: this);
                return;
            }

            ToggleGateLocal(isOpenOnInit, playerCommand: false);
        }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
        {
            switch ((ActionType)actionID)
            {
                case ActionType.toggleGate:
                    ToggleGateLocal(!IsOpen, input.playerCommand);
                    break;
                case ActionType.setGateOpen:
                    ToggleGateLocal(input.includeMovement, input.playerCommand);
                    break;
            }
            return base.LaunchActionLocal(actionID, input);
        }
        #endregion

        #region Toggling Gate
        public ErrorMessage CanToggleGate()
        {
            if (!Entity.IsInteractable)
                return ErrorMessage.invalid;

            return ErrorMessage.none;
        }
        public ErrorMessage ToggleGate(bool playerCommand)
        {
            return LaunchAction((byte)ActionType.toggleGate, new SetTargetInputData
            {
                playerCommand = playerCommand
            });
        }

        public ErrorMessage SetGateOpen(bool open, bool playerCommand)
        {
            return LaunchAction((byte)ActionType.toggleGate, new SetTargetInputData
            {
                includeMovement = open,
                playerCommand = playerCommand
            });
        }

        public ErrorMessage ToggleGateLocal(bool open, bool playerCommand)
        {
            if (!Entity.CanLaunchTask)
                return ErrorMessage.taskSourceCanNotLaunch;

            ErrorMessage errorMsg;
            if ((errorMsg = CanToggleGate()) != ErrorMessage.none)
            {
                if (playerCommand && RTSHelper.IsLocalPlayerFaction(Entity))
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity
                    });

                return errorMsg;

            }

            IsOpen = open;
            obstacleHandler.SetActive(!IsOpen);

            RaiseGateToggled();
            if (IsOpen)
                openEvent.Invoke();
            else
                closeEvent.Invoke();

            return ErrorMessage.none;
        }
        #endregion

        #region UI Tasks
        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (toggleGateTaskUI.IsValid() && taskAttributes.data.code == toggleGateTaskUI.Data.code)
            {
                ToggleGate(playerCommand: true);
                return true;
            }
            return base.OnTaskUIClick(taskAttributes);
        }

        protected override bool OnTaskUICacheUpdate(List<EntityComponentTaskUIAttributes> taskUIAttributesCache, List<string> disabledTaskCodesCache)
        {
            return RTSHelper.OnSingleTaskUIRequest(
                this,
                taskUIAttributesCache,
                disabledTaskCodesCache,
                toggleGateTaskUI);
        }
        #endregion
    }

    public interface IBuildingGateHandler
    {
        bool IsOpen { get; }

        event CustomEventHandler<IBuildingGateHandler, EventArgs> GateToggled;

        ErrorMessage SetGateOpen(bool open, bool playerCommand);
        ErrorMessage ToggleGate(bool playerCommand);
    }
}
