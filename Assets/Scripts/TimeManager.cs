using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Snapshot count is kept at bay 
    private LinkedList<TimeSnapshot> _snapshots;
    private LinkedListNode<TimeSnapshot> _current;

    void Awake()
    {
        _snapshots = new LinkedList<TimeSnapshot>();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.ActiveCar || GameManager.Instance.ActiveCar.Destroyed) return;

        if (GameManager.Instance.RecordSnapshots)
        {
            var car = GameManager.Instance.ActiveCar;
            var snapShot = car.GetTimeSnapshot();
            _snapshots.AddLast(snapShot);
            _current = _snapshots.Last;
        }

        if (GameManager.Instance.Rewinding && _current.Previous != null)
        {
            _current = _current.Previous;
            GameManager.Instance.ActiveCar.ApplySnapshot(_current.Value);
        }
    }

    public void Reset()
    {
        _snapshots.Clear();
    }

    /// <summary>
    /// Clones the timeline into two, where the previous car will share the first half with the new car.
    /// </summary>
    /// <returns></returns>
    public void CutTimeline(CarController old)
    {
        // Clone the current timeline into a timeline for the old car
        var clone = new LinkedList<TimeSnapshot>();
        LinkedListNode<TimeSnapshot> currentInClone = null;
        for (var node = _snapshots.First; node != null; node = node.Next)
        {
            clone.AddLast(node.Value);

            if (_current == node)
            {
                currentInClone = clone.Last;
            }
        }

        old.SetTimeline(clone, currentInClone);

        // Cut the timeline so that anything after _current is removed
        var tail = _snapshots.Last;
        while (tail != _current)
        {
            _snapshots.RemoveLast();
            tail = _snapshots.Last;
        }
    }

    public void SetTimeline(LinkedList<TimeSnapshot> timeline)
    {
        _snapshots = timeline;
        _current = timeline.Last;
    }
}
