using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float MaxSnapshots;

    private LinkedList<TimeSnapshot> _snapshots;

    void Awake()
    {
        _snapshots = new LinkedList<TimeSnapshot>();
    }

    void Update()
    {

    }

    private void AddSnapshot()
    {

    }

    public void Reset()
    {

    }
}
