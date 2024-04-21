using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Game;
using TMPro;
using UnityEngine.UI;
using RTSEngine.Cameras;

public class MapSelectorScript : MonoBehaviour
{
    public static MapSelectorScript Instance;
    public List<GameObject> factions = new List<GameObject>();
    public GameManager gm;
    public GameObject factionUIPrefab;
    public Transform factionUIParent;
    public GameObject miniMapUI;
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
        //Debug.Log("local Faction = " + gm.ActiveFactionSlots);
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
        return GameObject.Find("command_center_" + facId.ToString()).transform.localPosition;
    }

    private void OnButtonClick(int id)
    {
        GetComponent<MainCameraKeyboardPanningHandler>().ChangeCamPosition(id);
        factionUIParent.parent.parent.parent.gameObject.SetActive(false);
        factionUIParent.parent.parent.parent.parent.GetChild(1).gameObject.SetActive(true);
        miniMapUI.SetActive(false);
    }
    public void OnBackButtonClick()
    {
        GetComponent<MainCameraKeyboardPanningHandler>().ChangeCamPosition(gm.LocalFactionSlotID);
        factionUIParent.parent.parent.parent.gameObject.SetActive(false);
        miniMapUI.SetActive(true);
    }

    public void FactionUIInit(int localFacId, int factionCount)
    {
        for (int i = 0; i < factionCount; i++)
        {
            if(i != localFacId)
            {
                GameObject factionBtn = Instantiate(factionUIPrefab, factionUIParent);
                factionBtn.name = i.ToString();
                factionBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Faction " + i.ToString();
                Object test = factionBtn.GetComponent<Button>().onClick.GetPersistentTarget(0);
                test = this.gameObject;
                factionBtn.GetComponent<Button>().onClick.AddListener(()=> OnButtonClick(int.Parse(factionBtn.name)));
            }
            
        }
        
    }
}
