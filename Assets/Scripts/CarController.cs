using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float Acceleration;
    public float TurningSpeed;
    public float MaximumDriftingAngle;
    public float DriftingAngleDampening;
    public float DriftingVelocityDampening;
    public float MaxTurningVelocity;
    public float ShowSkidMarksOnDriftingT;

    public float MaxVelocity;
    public float VelocityDampening;
    public float OnGroundDampening;

    public float Gravity;
    public LayerMask GroundLayerMask;
    public float MaxGroundCheckDistance;

    public float TerrainRotationLerpT;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _onGround;

    public TrailRenderer[] TrailRenderers;

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
        if (Physics.SphereCast(transform.position + transform.up * (_controller.center.y + 0.1f), _controller.radius, transform.up * -1f, out hit, 0.1f + MaxGroundCheckDistance, GroundLayerMask))
        {
            _onGround = true;
        }
    }

    private void Movement()
    {
        var dt = Time.fixedDeltaTime;

        var showSkidMarks = false;

        var forwardInput = Mathf.Clamp(Input.GetAxis("Vertical"), -0.5f, 1f); // Backwards driving speed is half
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

        // Figure out how much extra dampening should happen because of drifing angle
        // Most dampening happens when car is at MaxDriftingAngle degrees to velocity
        var angleDiff = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(_velocity * (Vector3.Dot(_velocity.normalized, transform.forward) > 0f ? 1f : -1f), transform.up));
        var driftingDampeningT = Mathf.Clamp(angleDiff / MaximumDriftingAngle, 0f, 1f);
        if (driftingDampeningT > ShowSkidMarksOnDriftingT)
        {
            showSkidMarks = true;
        }

        _velocity = Vector3.ClampMagnitude(_velocity, MaxVelocity);

        // Dampen the velocity. Air drag + friction on ground + drifting friction
        var dampening = VelocityDampening; // Normal air drag. 1f = no drag
        if (_onGround)
        {
            dampening -= OnGroundDampening;
            dampening -= DriftingVelocityDampening * driftingDampeningT;
        }
        _velocity *= dampening;

        // Helpers
        var localUpVelocity = Vector3.ProjectOnPlane(_velocity, transform.up);
        var goingForward = Vector3.Dot(_velocity.normalized, transform.forward) > 0f;

        var targetAngleDiff = 0f;

        // If on ground, car can be turned
        // Maximum turning angle is between transform.forward and _velocity and is based on velocity inversely
        if (_onGround && localUpVelocity.magnitude > 0.001f)
        {
            var turningT = Mathf.Clamp(localUpVelocity.magnitude / MaxTurningVelocity, 0f, 1f);

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

        if (frontSideVelocity.magnitude > 0.001f)
        {
            var targetRotation = Quaternion.AngleAxis(targetAngleDiff, transform.up) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, DriftingAngleDampening);
        }

        // If bumping into objects, force car to go along the object
        // TODO

        // Add gravity
        _velocity += Vector3.down * Gravity * dt;

        // If on ground, map _velocity to local up plane
        if (_onGround)
        {
            _velocity = Vector3.ProjectOnPlane(_velocity, transform.up);
        }

        // If on ground, make the car rotate to match the terrain angle
        // Raycast 3 times from front center and both back wheels downwards to find the current plane.
        // Make the car rotate to that rotation slowly
        if (_onGround)
        {
            var frontOrigin = transform.position + transform.forward * 0.5f + transform.up * 0.5f;
            var backLeftOrigin = transform.position + transform.forward * -0.5f + transform.up * 0.5f + transform.right * -0.5f;
            var backRightOrigin = transform.position + transform.forward * -0.5f + transform.up * 0.5f + transform.right * 0.5f;

            RaycastHit frontHit, backLeftHit, backRightHit;
            bool frontWasHit, backLeftWasHit, backRightWasHit;
            frontWasHit = Physics.Raycast(frontOrigin, transform.up * -1f, out frontHit, 0.6f, GroundLayerMask);
            backLeftWasHit = Physics.Raycast(backLeftOrigin, transform.up * -1f, out backLeftHit, 0.6f, GroundLayerMask);
            backRightWasHit = Physics.Raycast(backRightOrigin, transform.up * -1f, out backRightHit, 0.6f, GroundLayerMask);

            // TODO
        }

        // No skidmarks on air
        if (!_onGround)
        {
            showSkidMarks = false;
        }

        TrailRenderers[0].emitting = showSkidMarks;
        TrailRenderers[1].emitting = showSkidMarks;

        _controller.Move(_velocity);
    }
}
