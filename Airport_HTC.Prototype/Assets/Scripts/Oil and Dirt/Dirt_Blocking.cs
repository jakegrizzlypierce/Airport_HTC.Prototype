﻿using UnityEngine;
using System.Collections;

public class Dirt_Blocking : MonoBehaviour {
	public Bibbits_PointOfInterest POIToMonitor;
    public GameObject m_BlockedItem;
    private string m_BlockedItemType;

    public bool m_NotStartBlocked = false;


	IEnumerator Start ()
    {
	    if (m_BlockedItem.tag == "Fusebox")
        {
            m_BlockedItemType = "Fusebox";
            m_BlockedItem.GetComponent<FuseboxBehaviour>().SetActive(m_NotStartBlocked);
        }
        if (m_BlockedItem.tag == "Spawner")
        {
            m_BlockedItemType = "Spawner";
            //m_BlockedItem.GetComponent<Bibbit_Group>().SetIfSpawningActive(m_NotStartBlocked);
        }

		yield return new WaitForSeconds (1.0f);

		POIToMonitor.DoActivation();
		POIToMonitor.OnPOIDeactivated += DoUnlock;
	}

	private void DoUnlock(Bibbits_PointOfInterest poi)
	{
		GetComponentInChildren<Animator> ().SetTrigger ("Deactivate");
		Unlock ();
	}

    public void Unlock()
    {
        if (m_BlockedItemType == "Fusebox")
        {
            m_BlockedItem.GetComponent<FuseboxBehaviour>().SetActive(true);
        }
        if (m_BlockedItemType == "Spawner")
        {
            //m_BlockedItem.GetComponent<Bibbit_Group>().SetIfSpawningActive(true);
        }
    }



}
