using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float Smoothing;

    void Update()
    {
        if (!GameManager.Instance.ActiveCar) return;

        var target = GameManager.Instance.ActiveCar.transform.position - GameManager.Instance.SceneCameraOffset;
        if (Vector3.Distance(transform.position, target) > 0.5f)
        {
            transform.position = Vector3.Lerp(transform.position, target, Smoothing);
        }
        else
        {
            transform.position = target;
        }
    }
}
