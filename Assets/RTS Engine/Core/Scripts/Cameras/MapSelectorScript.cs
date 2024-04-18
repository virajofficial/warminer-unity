using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Game;

public class MapSelectorScript : MonoBehaviour
{
    public static MapSelectorScript Instance;
    public List<GameObject> factions = new List<GameObject>();
    public GameManager gm;
    public int SelectedFaction;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CamPostionSet(gm.LocalFactionSlotID);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("local Faction = " + gm.LocalFactionSlotID);
    }

    public Vector3 CamPostionSet(int facId)
    {
        for (int i = 0; i < factions.Count; i++)
        {
            MeshRenderer[] comList = factions[i].transform.GetComponentsInChildren<MeshRenderer>();
            if (i != facId)
            {
                foreach (MeshRenderer comp in comList)
                {
                    comp.enabled = false;
                }
            }
            else
            {
                foreach (MeshRenderer comp in comList)
                {
                    comp.enabled = true;
                }
            }
        }
        return GameObject.Find("command_center_" + facId.ToString()).transform.position;
    }
}
