using RTSEngine.Selection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VehicleController : MonoBehaviour
{
    [Header("Referances")]
    public UnitSelection selectedUnit;
    public new SkinnedMeshRenderer renderer;
    public Animator vehicleAnimator;
    public AudioSource vehicle_sfx;
    NavMeshAgent agent;
    
    [Header("Parameters")]
    public float speedFactor;
    public float idlePitchSFX = 1f;
    public float drivePitchSFX = -1f;
    public float vehicleVolumnSFX = 1;
    

    Material leftWheels;
    Material rightWheels;

    int leftValue = 1;
    int rightValue = 1;
    bool isTriggered = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        leftWheels = renderer.sharedMaterials[1];
        rightWheels = renderer.sharedMaterials[2];
        isTriggered = false;
        vehicle_sfx.Play();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 s = agent.transform.InverseTransformDirection(agent.velocity).normalized;
        float speed = s.z;
        float turn = s.x;
        //Debug.Log("vehicle speed = " + speed + ", turn = " + turn);
        //Debug.Log("left = " + leftWheels.GetFloat("_TracksSpeed"));

        if (turn > 0.1f)
        {
            rightValue = -1;
            leftValue = 1;
            vehicleAnimator.SetFloat("TankX", Mathf.Clamp(speed * speedFactor * rightValue, 0, 3));
            vehicleAnimator.SetFloat("TankZ", 0);
            Debug.Log("Right");
            //Turn right
        }
        else if (turn < -0.1)
        {
            rightValue = 1;
            leftValue = -1;
            vehicleAnimator.SetFloat("TankX", Mathf.Clamp(speed * speedFactor * rightValue, -3, 0));
            vehicleAnimator.SetFloat("TankZ", 0);
            Debug.Log("Left");
            //Turn left
        }
        else
        {
            rightValue = 1;
            leftValue = 1;
            vehicleAnimator.SetFloat("TankX", 0);
            vehicleAnimator.SetFloat("TankZ", Mathf.Clamp(speed * speedFactor, -3, 3));
            //Forward
        }
        rightWheels.SetFloat("_TracksSpeed", Mathf.Clamp(speed * speedFactor * rightValue, -3, 3));
        leftWheels.SetFloat("_TracksSpeed", Mathf.Clamp(speed * speedFactor * leftValue, -3, 3));
        //vehicleAnimator.SetFloat("TankX", Mathf.Clamp(speed * speedFactor * rightValue, -3, 3));

        if (selectedUnit.IsSelected)
        {
            vehicle_sfx.volume = vehicleVolumnSFX;
        }
        else
        {
            vehicle_sfx.volume = 0;
        }

        if (speed == 0)
            vehicle_sfx.pitch = idlePitchSFX;
        else
            vehicle_sfx.pitch = drivePitchSFX;
    }
}
