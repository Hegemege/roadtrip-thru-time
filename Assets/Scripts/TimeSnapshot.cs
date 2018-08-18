using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TimeSnapshot
{
    public Vector3 CarPosition;
    public Vector3 Velocity;
    public Quaternion CarRotation;
    public Quaternion TargetRotation;
    public float ForwardInput;
    public float SteeringInput;

    public TimeSnapshot(Vector3 position, Vector3 velocity, Quaternion rotation, Quaternion targetRotation, float forwardInput, float steeringInput)
    {
        CarPosition = position;
        Velocity = velocity;
        CarRotation = rotation;
        TargetRotation = targetRotation;
        ForwardInput = forwardInput;
        SteeringInput = steeringInput;
    }
}
