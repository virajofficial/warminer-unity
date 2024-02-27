using RTSEngine.Selection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VehicleController : MonoBehaviour
{
    public UnitSelection selectedUnit;
    NavMeshAgent agent;
    public new SkinnedMeshRenderer renderer;
    public float speedFactor;

    Material leftWheels;
    Material rightWheels;

    int leftValue = 1;
    int rightValue = 1;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        leftWheels = renderer.sharedMaterials[1];
        rightWheels = renderer.sharedMaterials[2];
    }

    // Update is called once per frame
    void Update()
    {
        if (selectedUnit.IsSelected)
        {
            Vector3 s = agent.transform.InverseTransformDirection(agent.velocity).normalized;
            float speed = s.z;
            float turn = s.x;
            Debug.Log("vehicle speed = " + speed + ", turn = " + turn);
            Debug.Log("left = " + leftWheels.GetFloat("_TracksSpeed"));
            
            if(turn > 0.1f)
            {
                rightValue = -1;
                leftValue = 1;
            }
            else if (turn < -0.1)
            {
                rightValue = 1;
                leftValue = -1;
            }
            else
            {
                rightValue = 1;
                leftValue = 1;
            }
            rightWheels.SetFloat("_TracksSpeed",Mathf.Clamp(speed * speedFactor * rightValue, -4,4));
            leftWheels.SetFloat("_TracksSpeed", Mathf.Clamp(speed * speedFactor * leftValue,-4,4));
            
        }
        
    }
}
