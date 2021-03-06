﻿using UnityEngine;
using System.Collections.Generic;

public class Vending_Behaviour : MonoBehaviour {

    public List<Transform> AllCans;
    int CanIndex;

    public GameObject m_CanPref;
    private GameObject m_DroppedCan;
    private bool m_CanDropped = false;
    private bool m_TimerStarted = false;
    private float m_AnimTimer;
    private float m_ElapsedTimer;
    private Animation m_VendingAnim;

    public Vector3 m_CanPosition;
    public Vector3 m_CanRotation;

    private AudioSource m_Audio;
    public AudioClip m_AudioClip;

	// Use this for initialization
	void Start ()
    {
        m_VendingAnim = GetComponentInChildren<Animation>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (m_VendingAnim.isPlaying)
        {
            if (!m_TimerStarted)
            {
                Debug.Log("Timer Started!");
                m_AnimTimer = Time.time;
                m_TimerStarted = true;

                GameObject audio = (GameObject)Instantiate(new GameObject(), gameObject.transform.position, Quaternion.identity);
                m_Audio = audio.AddComponent<AudioSource>();
                m_Audio.clip = m_AudioClip;
            }

            m_ElapsedTimer = Time.time - m_AnimTimer;

            if (!m_CanDropped)
            {
                if (m_ElapsedTimer >= .1)
                {
                    m_Audio.Play();
                }
                if (m_ElapsedTimer >= 3.25)
                {
                    Debug.Log("Can Dropped!");

                    m_VendingAnim.Stop();

                    if (!m_VendingAnim.isPlaying)
                    {
                        Transform can = AllCans[CanIndex];
                        CanIndex = (CanIndex + 1) % AllCans.Count;
                        can.GetComponent<Rigidbody>().MovePosition(m_CanPosition);
                        can.GetComponent<Rigidbody>().MoveRotation(Quaternion.Euler(m_CanRotation));
                        m_DroppedCan = can.gameObject;//(GameObject)Instantiate(m_CanPref, m_CanPosition, Quaternion.Euler(m_CanRotation));
                        Destroy(transform.GetChild(0).FindChild("CAN_anim").gameObject);
                        m_CanDropped = true;
                    }
                }
            }
        } 
	}
}
