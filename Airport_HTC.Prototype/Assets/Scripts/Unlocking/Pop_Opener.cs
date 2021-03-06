﻿using UnityEngine;
using System.Collections;

public class Pop_Opener : MonoBehaviour {

    AudioSource m_Audio;
    VRTK_InteractableObject m_IntObj;
    public ParticleSystem m_PS;
    bool m_Opened = false;

    // Use this for initialization
    void Start () {
        m_Audio = GetComponent<AudioSource>();
        m_IntObj = GetComponent<VRTK_InteractableObject>();
		Debug.Assert (m_PS != null, gameObject);
	}
	
	// Update is called once per frame
	void Update ()
    {
	    if (m_IntObj.IsGrabbed())
        {
            if (!m_Opened)
            {
                m_Audio.Play();
                m_PS.Play();
                m_Opened = true;
            }
        }
	}
}
