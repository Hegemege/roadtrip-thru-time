using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float Acceleration;
    public float TurningSpeed;
    public float MaximumDriftingAngle;
    public float DriftingAngleDampening;

    public float MaxVelocity;
    public float VelocityDampening;

    public float Gravity;
    public LayerMask GroundLayerMask;
    public float MaxGroundCheckDistance;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _onGround;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        _velocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        UpdateOnGround();
        Movement();
    }

    private void UpdateOnGround()
    {
        _onGround = false;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position + Vector3.up * (_controller.radius + 0.1f), _controller.radius, Vector3.down, out hit, _controller.radius + 0.1f + MaxGroundCheckDistance, GroundLayerMask))
        {
            _onGround = true;
        }
    }

    private void Movement()
    {
        var dt = Time.fixedDeltaTime;

        var forwardInput = Input.GetAxis("Vertical");
        var steeringInput = Input.GetAxis("Horizontal");
        /*
        newForward *= forwardInput * Acceleration * dt;
        newForward = Quaternion.AngleAxis(steeringInput * TurningSpeed * dt, Vector3.up) * newForward;
        newForward += Vector3.down * Gravity * dt;
        */

        // Add acceleration only if on ground
        if (_onGround)
        {
            _velocity += transform.forward * forwardInput * Acceleration * dt;
        }

        _velocity = Vector3.ClampMagnitude(_velocity, MaxVelocity);
        _velocity *= VelocityDampening;

        var localUpVelocity = Vector3.ProjectOnPlane(_velocity, transform.up);
        var goingForward = Vector3.Dot(_velocity.normalized, transform.forward) > 0f;

        var targetAngleDiff = 0f;

        // If on ground, car can be turned
        // Maximum turning angle is between transform.forward and _velocity and is based on velocity inversely
        if (_onGround && localUpVelocity.magnitude > 0.001f)
        {
            var turningT = 1f - _velocity.magnitude / MaxVelocity;

            //Debug.Log(turningT);

            targetAngleDiff = turningT *
                steeringInput *
                MaximumDriftingAngle *
                (goingForward ? 1f : -1f);
        }

        // Turn the forward angle a bit towards horizontal velocity, after it has been mapped to the front of the car (in case of reversing)
        var frontSideVelocity = localUpVelocity;
        if (!goingForward)
        {
            // The velocity vector we want to turn towards is the negative
            frontSideVelocity *= -1f;
        }

        //Debug.Log(targetAngleDiff);

        if (frontSideVelocity.magnitude > 0.001f)
        {
            var targetRotation = Quaternion.AngleAxis(targetAngleDiff, transform.up) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, DriftingAngleDampening);
        }

        // If bumping into objects, force car to go along the object

        // If not on ground, add gravity
        if (!_onGround)
        {
            _velocity += Vector3.down * Gravity * dt;
        }


        //Debug.Log(_velocity);

        _controller.Move(_velocity);
    }
}
