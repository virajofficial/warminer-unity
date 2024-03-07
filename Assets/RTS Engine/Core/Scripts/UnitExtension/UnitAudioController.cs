using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitAudioController : MonoBehaviour
{
    [SerializeField]
    private AudioSource unitAudioSource;
    [SerializeField]
    private NavMeshAgent unitAgent;
    [SerializeField]
    private List<AudioClip> audioClips;

    private void Start()
    {
        unitAudioSource = GetComponent<AudioSource>();
        unitAgent = GetComponent<NavMeshAgent>();
        unitAudioSource.volume = 0;
    }

    private void Update()
    {
        /*if(unitAgent.velocity.magnitude != 0.1f)
        {
            unitAudioSource.clip = audioClips[0];
            unitAudioSource.volume = 1;
        }
        else
        {
            unitAudioSource.volume = 0;
        }*/
    }


}
