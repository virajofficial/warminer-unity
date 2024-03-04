using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AITugController : MonoBehaviour
{
    public Transform targetTransform;
    public Transform dockTransform;
    public GameObject crate;
    public GameObject cables;
    Animator anim;

    [Header("Tug Parameters")]
    public float altitude = 20;
    public float movingSpeed = 5f;
    public float takeoffLandingSpeed = 2f;
    public float slerpSpeed = 0.5f;
    public float lerpSpeed = 0.5f;
    public float lerpPercent = 0f;
    public bool isFlying = false;

    private TugStatus tugStatus = TugStatus.TAKEOFF;
    private Vector3 moveEnd;
    private Vector3 initialPos;

    bool istakeoff;
    bool ismove;
    bool isstop;
    bool isTugRotated;
    bool isTugReturned;

    private void Start()
    {
        anim = GetComponent<Animator>();
        TugInitialValues();
    }

    private void Update()
    {
        if (isFlying){
            if (tugStatus == TugStatus.TAKEOFF) takeoff();
            if (tugStatus == TugStatus.FLYING) StartCoroutine(flying());
            if (tugStatus == TugStatus.LANDING) landing();
        }
        //testCam.transform.localPosition = new Vector3(-4.95f, 1.41f, 0.74f);
        //testCam.transform.rotation = Quaternion.Euler(0, 90, 0);
        
    }

    private void TugInitialValues()
    {
        istakeoff = false;
        ismove = false;
        isstop = false;
        isTugRotated = false;
        isTugReturned = false;
        lerpPercent = 0f;
        crate.SetActive(false);
        cables.SetActive(false);
        initialPos = transform.position;
    }

    private void takeoff()
    {
        Vector3 altitudeVect = new Vector3(transform.position.x, initialPos.y + altitude, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, altitudeVect, takeoffLandingSpeed * Time.deltaTime);
        if (!istakeoff) {
            anim.SetTrigger("takeoff");
            istakeoff = true;
        } 
        if (Vector3.Distance(transform.position, altitudeVect) < 0.1f)
        {
            tugStatus = TugStatus.FLYING;
            anim.ResetTrigger("takeoff");
        }
    }

    private IEnumerator flying()
    {
        

        yield return new WaitForSeconds(0);
        TugRotate();

        moveEnd = new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, moveEnd, movingSpeed * Time.deltaTime);
        
        if (!ismove)
        {
            anim.SetTrigger("move");
            ismove = true;
        }
        if (Vector3.Distance(transform.position, moveEnd) < 0.1f)
        {
            tugStatus = TugStatus.LANDING;
            anim.ResetTrigger("move");
            transform.localRotation = Quaternion.Euler(new Vector3(0, transform.rotation.y, 0));
        }
    }

    private void landing()
    {
        

        Vector3 landVect = new Vector3(transform.position.x, targetTransform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, landVect, takeoffLandingSpeed * Time.deltaTime);
        if (!isstop)
        {
            anim.SetTrigger("stop");
            isstop = true;
        }
        if (Vector3.Distance(transform.position, landVect) < 0.1f)
        {
            isFlying = false;
            tugStatus = TugStatus.TAKEOFF;
            anim.ResetTrigger("stop");
            if(isTugReturned)
                TugInitialValues();
            else
                StartCoroutine(TugReturn());
        }
    }

    private IEnumerator TugReturn()
    {
        TugInitialValues();
        targetTransform = dockTransform;
        yield return new WaitForSeconds(1);
        crate.SetActive(true);
        cables.SetActive(true);
        isFlying = true;
        isTugReturned = true;
    }

    private void TugRotate()
    {
        lerpPercent = Mathf.MoveTowards(lerpPercent, 1f, Time.deltaTime * lerpSpeed);

        Vector3 targetDirection = (targetTransform.position - transform.position);
        //Debug.Log("Target Direction = " + targetDirection);
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(targetDirection.x, transform.rotation.y, targetDirection.z));
        //Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpPercent);

        if (lerpPercent >= 1f)
        {
            //Debug.Log("lerp finished");
            isTugRotated = true;
        }
    }


}
public enum TugStatus
{
    TAKEOFF,
    LANDING,
    FLYING
}
