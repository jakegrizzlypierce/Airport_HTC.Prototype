﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GroupManager : Singleton<GroupManager>
{
    private List<Bibbit_Group> m_Groups = new List<Bibbit_Group>();

    private Dictionary<Transform, Bibbit_Group> m_BibbitsToGroups = new Dictionary<Transform, Bibbit_Group>();

    private Dictionary<Bibbit_LineSpawner, Bibbit_Group> m_SpawnersToGroup = new Dictionary<Bibbit_LineSpawner, Bibbit_Group>();

    private Dictionary<GameObject, List<Transform>> m_HandlerToGrabbedBibbits = new Dictionary<GameObject, List<Transform>>();
    //private List<Transform> m_GrabbedBibbits = new List<Transform>();
    private Coroutine m_WarpToHandAndParentCoroutine = null;

    public void RegisterGroup(Bibbit_Group group)
    {
        Debug.Assert(!m_Groups.Contains(group));
        m_Groups.Add(group);

        if (group.Spawner != null)
        {
            Debug.Assert(!m_SpawnersToGroup.ContainsKey(group.Spawner));
            Debug.Assert(!m_SpawnersToGroup.ContainsValue(group));

            m_SpawnersToGroup.Add(group.Spawner, group);
        }
    }

    public void UnregisterGroup(Bibbit_Group group)
    {
        Debug.Assert(m_Groups.Contains(group));
        m_Groups.Remove(group);

        if (group.Spawner != null)
        {
            Debug.Assert(m_SpawnersToGroup.ContainsKey(group.Spawner));
            Debug.Assert(m_SpawnersToGroup[group.Spawner] == group);

            m_SpawnersToGroup.Remove(group.Spawner);
        }
    }

    public void RegisterSpawner(Bibbit_LineSpawner spawner)
    {
        spawner.OnBibbitSpawned += OnBibbitSpawned;
        spawner.OnBibbitUnspawned += OnBibbitUnspawned;
    }

    public void UnregisterSpawner(Bibbit_LineSpawner spawner)
    {
        spawner.OnBibbitSpawned -= OnBibbitSpawned;
        spawner.OnBibbitUnspawned -= OnBibbitUnspawned;
    }

    void OnBibbitSpawned(Bibbit_LineSpawner spawner, Transform bibbit)
    {
        VRTK_InteractableObject bibbitInteractableObject = bibbit.GetComponent<VRTK_InteractableObject>();
        bibbitInteractableObject.InteractableObjectGrabbed += DoInteractGrab;
        bibbitInteractableObject.InteractableObjectUngrabbed += DoInteractUngrab;

        Debug.Assert(m_SpawnersToGroup.ContainsKey(spawner));
        Bibbit_Group group = m_SpawnersToGroup[spawner];

        Debug.Assert(!m_BibbitsToGroups.ContainsKey(bibbit));

        m_BibbitsToGroups.Add(bibbit, group);
        group.AddBibbit(bibbit.gameObject);
    }

    void OnBibbitUnspawned(Bibbit_LineSpawner spawner, Transform bibbit)
    {
        VRTK_InteractableObject bibbitInteractableObject = bibbit.GetComponent<VRTK_InteractableObject>();
        bibbitInteractableObject.InteractableObjectGrabbed -= DoInteractGrab;
        bibbitInteractableObject.InteractableObjectUngrabbed -= DoInteractUngrab;

        Debug.Assert(m_SpawnersToGroup.ContainsKey(spawner));
        Bibbit_Group group = m_SpawnersToGroup[spawner];

        group.RemoveBibbit(bibbit.gameObject);
        m_BibbitsToGroups.Remove(bibbit);
    }

    void DoInteractGrab(object sender, InteractableObjectEventArgs e)
    {
        // TODO: Play sound and stop animation. clinel 2016-08-21.
        VRTK_InteractableObject bibbitInteractableObject = sender as VRTK_InteractableObject;
        GameObject bibbit = bibbitInteractableObject.gameObject;
        Transform bibbitTransform = bibbit.transform;

        List<Transform> grabbedBibbits;
        if (!m_HandlerToGrabbedBibbits.TryGetValue(e.interactingObject, out grabbedBibbits))
        {
            grabbedBibbits = new List<Transform>();
            m_HandlerToGrabbedBibbits.Add(e.interactingObject, grabbedBibbits);
        }

        // Note: We could grab a bibbit that is warping toward the hand.
        if (!grabbedBibbits.Contains(bibbitTransform))
        {
            if (m_BibbitsToGroups.ContainsKey(bibbitTransform))
            {
                Bibbit_Group spawner = m_BibbitsToGroups[bibbitTransform];

                List<Transform> neighbours = new List<Transform>();
                spawner.GetNeighbouringBibbits(bibbitTransform, ref neighbours, maxNbNeighbours: 5);

                Debug.Assert(!neighbours.Contains(bibbitTransform));

                spawner.RemoveBibbit(bibbit);
                bibbit.GetComponentInChildren<Animation>().Stop();
                m_BibbitsToGroups.Remove(bibbitTransform);
                grabbedBibbits.Add(bibbitTransform);

                int nbNeighbours = neighbours.Count;
                for (int i = 0; i < nbNeighbours; ++i)
                {
                    Transform neighbourBibbit = neighbours[i];

                    neighbourBibbit.GetComponentInChildren<Animation>().Stop();

                    spawner.RemoveBibbit(neighbourBibbit.gameObject);
                    m_BibbitsToGroups.Remove(neighbourBibbit);

                    grabbedBibbits.Add(neighbourBibbit);
                }

                m_WarpToHandAndParentCoroutine = StartCoroutine(WarpGrabbedBibbitsToHandAndParent(grabbedBibbits, e.interactingObject.transform));
            }
            else
            {
                Debug.Log("Bibbit grabbed " + bibbitTransform.name, bibbitTransform);
            }
        }
    }

    void DoInteractUngrab(object sender, InteractableObjectEventArgs e)
    {
        // TODO: Play sound and restart animation. clinel 2016-08-21.

        // Find closest spawner and add bibbit to it.
        // TODO: Use something else than distance to the spawner as it could be in some weird locations (pipe above, etc.). clinel 2016-08-13.

        Debug.Assert(m_WarpToHandAndParentCoroutine != null);
        // Note: Stop the coroutine just in case we ungrabbed them before they reached the hand.
        StopCoroutine(m_WarpToHandAndParentCoroutine);

        VRTK_InteractableObject bibbitInteractableObject = sender as VRTK_InteractableObject;
        GameObject bibbit = bibbitInteractableObject.gameObject;
        Transform bibbitTransform = bibbit.transform;

        Debug.Assert(m_HandlerToGrabbedBibbits.ContainsKey(e.interactingObject));
        List<Transform> grabbedBibbits = m_HandlerToGrabbedBibbits[e.interactingObject];
        Debug.Assert(grabbedBibbits.Contains(bibbitTransform));

        // Find closest spawner
        Bibbit_Group closestGroup = null;
        float closestDistance = float.MaxValue;

        int nbSpawners = m_Groups.Count;
        for (int i = 0; i < nbSpawners; ++i)
        {
            Bibbit_Group currentGroup = m_Groups[i];
            float distance = Vector3.Distance(bibbitTransform.position, currentGroup.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGroup = currentGroup;
            }
        }
        Debug.Assert(closestGroup != null);

        // Release ungrabbed bibbits
        int nbGrabbedBibbits = grabbedBibbits.Count;
        for (int i = 0; i < nbGrabbedBibbits; ++i)
        {
            Transform grabbedBibbit = grabbedBibbits[i];

            grabbedBibbit.GetComponent<Rigidbody>().isKinematic = false;
            grabbedBibbit.SetParent(null);
            grabbedBibbit.GetChild(0).transform.localPosition = Vector3.zero;
            grabbedBibbit.transform.rotation = Quaternion.Euler(Vector3.zero);
            Debug.Assert(!m_BibbitsToGroups.ContainsKey(grabbedBibbit));
            m_BibbitsToGroups[grabbedBibbit] = closestGroup;
            closestGroup.AddBibbit(grabbedBibbit.gameObject);
        }
        grabbedBibbits.Clear();
    }

    IEnumerator WarpGrabbedBibbitsToHandAndParent(List<Transform> bibbitsToWarp, Transform hand)
    {
        float GrabWarpSpeed = 4f;

        Dictionary<Transform, Vector3> bibbitsToInitialPosition = new Dictionary<Transform, Vector3>();

        int nbBibbitsToWarp = bibbitsToWarp.Count;
        for (int i = 0; i < nbBibbitsToWarp; ++i)
        {
            Transform bibbitToWarp = bibbitsToWarp[i];
            bibbitToWarp.transform.GetComponent<Rigidbody>().isKinematic = true;
            bibbitsToInitialPosition.Add(bibbitToWarp, bibbitToWarp.position);
        }

        while (bibbitsToInitialPosition.Count > 0)
        {
            foreach (Transform bibbit in bibbitsToWarp)
            {
                Vector3 initialPosition;

                if (bibbitsToInitialPosition.TryGetValue(bibbit, out initialPosition))
                {
                    float totalDistance = Vector3.Distance(initialPosition, hand.position);
                    if (totalDistance > 0f)
                    {
                        float ratio = Vector3.Distance(initialPosition, bibbit.position) / totalDistance;
                        float duration = totalDistance / GrabWarpSpeed;
                        ratio += Time.deltaTime / duration;
                        bibbit.transform.GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(initialPosition, hand.position, ratio));
                        if (ratio >= 1f)
                        {
                            bibbit.SetParent(hand);
                            bibbit.GetChild(0).transform.localPosition = Vector3.zero;
                            bibbitsToInitialPosition.Remove(bibbit);
                        }
                    }
                    else
                    {
                        bibbit.SetParent(hand);
                        bibbitsToInitialPosition.Remove(bibbit);
                    }
                }
            }

            yield return null;
        }
    }
}
