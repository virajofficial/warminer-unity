using RTSEngine.Selection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VehicleController : MonoBehaviour
{
    public UnitSelection selectedUnit;
    NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (selectedUnit.IsSelected)
        {
            Debug.Log("Velocity = " + agent.velocity);
        }
        
    }
}
