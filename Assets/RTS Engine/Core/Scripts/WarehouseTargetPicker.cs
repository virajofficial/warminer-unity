using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarehouseTargetPicker : MonoBehaviour
{
    [SerializeField, Tooltip("Define the faction entities that can be used as drop off points.")]
    public FactionEntityTargetPicker targetPicker = new FactionEntityTargetPicker();

    private void GetWarehouse()
    {
        //GameManager.
    }

}
