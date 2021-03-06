﻿using UnityEngine;
using System.Collections;

public class SpotlightTeleporter : MonoBehaviour {

    public bool IsOffOnStart = false;
    GameObject m_LightShaft;

    private Light m_Light;
    private ParticleSystem[] m_Particles = new ParticleSystem[2];

    public Color m_InactiveColor;
    public Color m_ActiveColor;

    public Material m_InactiveMat;
    public Material m_ActiveMat;

    public Vector3 TeleportPoint;

    public Vector3 GetTelePoint() { return TeleportPoint; }

    void Awake()
    {
        m_LightShaft = transform.FindChild("prop_lightshaft").gameObject;
        m_Light = transform.FindChild("Spotlight").GetComponent<Light>();

        m_Particles[0] = transform.FindChild("volumetric").GetComponent<ParticleSystem>();
        m_Particles[1] = transform.FindChild("volumetric (1)").GetComponent<ParticleSystem>();



        if (IsOffOnStart)
        {
            m_LightShaft.GetComponent<MeshRenderer>().enabled = false;
            m_LightShaft.GetComponent<MeshCollider>().enabled = false;
            m_Light.enabled = false;

            for (int i = 0; i < m_Particles.Length; ++i)
            {
                m_Particles[i].Stop();
            }
        }
    }
	
	public void IsActive (bool _active)
    {
        if (_active == true)
        {
            m_LightShaft.GetComponent<MeshRenderer>().enabled = true;
            m_LightShaft.GetComponent<MeshCollider>().enabled = true;
            m_Light.enabled = true;

            for (int i = 0; i < m_Particles.Length; ++i)
            {
                m_Particles[i].Stop();
            }
        }

        else
        {
            m_LightShaft.GetComponent<MeshRenderer>().enabled = false;
            m_LightShaft.GetComponent<MeshCollider>().enabled = false;
            m_Light.enabled = false;

            for (int i = 0; i < m_Particles.Length; ++i)
            {
                m_Particles[i].Play();
            }
        }
    }

    public void Highlight(bool _isHighlighted)
    {
        if (_isHighlighted == true)
        {
            m_LightShaft.GetComponent<MeshRenderer>().material = m_ActiveMat;
            m_Light.color = m_ActiveColor;
        }

        else
        {
            m_LightShaft.GetComponent<MeshRenderer>().material = m_InactiveMat;
            m_Light.color = m_InactiveColor;
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GetTelePoint(), new Vector3(2.5f, 1f, 2f));
    }
}
