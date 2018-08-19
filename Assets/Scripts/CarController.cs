using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float Acceleration;
    public float MaxVelocity;
    public float VelocityDampening;
    public float OnGroundDampening;

    public float TurningSpeed;
    public float MaximumDriftingAngle;
    public float DriftingAngleDampening;
    public float DriftingVelocityDampening;
    public float MaxTurningVelocity;
    public float ShowSkidMarksOnDriftingT;

    public float Gravity;
    public LayerMask GroundLayerMask;
    public float MaxGroundCheckDistance;

    public float TerrainRotationLerpT;
    public float MaxTerrainAngle;

    public TrailRenderer[] TrailRenderers;
    public Transform RotationRoot;

    public bool PlayerControlled;

    public LayerMask ObstacleLayerMask;
    public float ObstacleHitVelocityClamp;

    [HideInInspector]
    public float Energy;

    public List<Rotator> WheelRotators;
    public float InvulnerableToCarsStartTime;

    // Private

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _onGround;

    private Quaternion _targetRotation;
    private float _forwardInput;
    private float _steeringInput;

    private LinkedList<TimeSnapshot> _timeline;
    private LinkedListNode<TimeSnapshot> _currentSnapshot;
    private bool _playingTimeline;

    private float _invulnerableTimer;
    public bool Invulnerable;
    public bool Destroyed;
    public float DestroyOnImpactVelocity;

    public Vector3 Velocity { get { return _velocity; } } // quick hacks

    void Awake()
    {
        Invulnerable = true;
        _invulnerableTimer = InvulnerableToCarsStartTime;
        _controller = GetComponent<CharacterController>();
        Reset();
    }

    void Update()
    {
        if (GameManager.Instance.LevelEnded) return;

        if (GameManager.Instance.Rewinding)
        {
            Invulnerable = true;
            _invulnerableTimer = InvulnerableToCarsStartTime;
        }

        if (Invulnerable)
        {
            _invulnerableTimer -= Time.deltaTime;
            if (_invulnerableTimer <= 0f)
            {
                Invulnerable = false;
            }
        }

        var rotationScale = Vector3.ProjectOnPlane(_velocity, transform.up).magnitude * (Vector3.Dot(_velocity, transform.forward) > 0f ? -1f : 1f);
        for (var i = 0; i < WheelRotators.Count; i++)
        {
            WheelRotators[i].RotationScale = rotationScale;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.Rewinding && !GameManager.Instance.LevelEnded)
        {
            if (_playingTimeline)
            {
                if (_currentSnapshot.Previous != null)
                {
                    _currentSnapshot = _currentSnapshot.Previous;
                    ApplySnapshot(_currentSnapshot.Value);
                }
                // Else the car has reached zero time and should just be staying still
            }
            // Else the Time Manager applies the snapshot to the current car
            return;
        }

        if (_playingTimeline && !GameManager.Instance.LevelEnded)
        {
            // If there are snapshots left to be played back, play them. Otherwise, set all inputs to zero, let the physics play out and record additional snapshots
            if (_currentSnapshot.Next != null)
            {
                _currentSnapshot = _currentSnapshot.Next;
                ApplySnapshot(_currentSnapshot.Value);
                return;
            }
            else
            {
                if (PlayerControlled)
                {
                    // Not playing timeline anymore
                    _playingTimeline = false;
                    GameManager.Instance.TimeManager.SetTimeline(_timeline);
                }
                else
                {
                    _forwardInput = 0f;
                    _steeringInput = 0f;
                    _timeline.AddLast(GetTimeSnapshot());
                    _currentSnapshot = _timeline.Last;
                }
            }
        }

        UpdateOnGround();
        Movement();
    }

    /// <summary>
    /// Get a snapshot for storing the car's position, rotation and movement.
    /// </summary>
    /// <returns></returns>
    public TimeSnapshot GetTimeSnapshot()
    {
        return new TimeSnapshot(transform.position, _velocity, RotationRoot.transform.rotation, _targetRotation, _forwardInput, _steeringInput, Energy);
    }

    /// <summary>
    /// Applies the given snapshot to the car. After snapshots have been applied, the car can't be controlled anymore.
    /// </summary>
    public void ApplySnapshot(TimeSnapshot snapshot)
    {
        transform.position = snapshot.CarPosition;
        _velocity = snapshot.Velocity;
        RotationRoot.transform.rotation = snapshot.CarRotation;
        _targetRotation = snapshot.TargetRotation;
        _forwardInput = snapshot.ForwardInput;
        _steeringInput = snapshot.SteeringInput;
        Energy = snapshot.Energy;
    }

    public void SetTimeline(LinkedList<TimeSnapshot> timeline, LinkedListNode<TimeSnapshot> current)
    {
        _timeline = timeline;
        _currentSnapshot = current;
        _playingTimeline = true;
    }

    /// <summary>
    /// Reset the car controller such that it can be reused.
    /// </summary>
    public void Reset()
    {
        _velocity = Vector3.zero;
        _targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        _onGround = false;
        _forwardInput = 0f;
        _steeringInput = 0f;

        _timeline = new LinkedList<TimeSnapshot>();
        _currentSnapshot = null;
        _playingTimeline = false;
        Destroyed = false;
        Invulnerable = true;
        _invulnerableTimer = InvulnerableToCarsStartTime;
    }

    /// <summary>
    /// Refresh the _onGround state.
    /// </summary>
    private void UpdateOnGround()
    {
        _onGround = false;

        RaycastHit hit;
        if (Physics.Raycast(RotationRoot.transform.position + RotationRoot.transform.up * 0.1f, RotationRoot.transform.up * -1f, out hit, 0.1f + MaxGroundCheckDistance, GroundLayerMask))
        {
            _onGround = true;
        }
    }

    private void Movement()
    {
        var dt = Time.fixedDeltaTime;

        var showSkidMarks = false;

        if (PlayerControlled && !GameManager.Instance.LevelEnded)
        {
            _forwardInput = Mathf.Clamp(Input.GetAxis("Vertical"), -0.5f, 1f); // Backwards driving speed is half
            _steeringInput = Input.GetAxis("Horizontal");

            // Calculate energy loss
            var energyConsumption = Mathf.Abs(_forwardInput);
            if (Energy > 0f)
            {
                if (energyConsumption > 0f)
                {
                    Energy -= dt * GameManager.Instance.AccelerationEnergyConsumption * energyConsumption;
                }
                else
                {
                    Energy -= dt * GameManager.Instance.IdleEnergyConsumption;
                }
            }
            else
            {
                _forwardInput = 0f;
                _steeringInput = 0f;
                Energy = 0f;
            }
        }

        if (GameManager.Instance.LevelEnded)
        {
            _steeringInput = 0f;
            _forwardInput = 0f;
        }

        // Add acceleration only if on ground
        if (_onGround)
        {
            _velocity += RotationRoot.transform.forward * _forwardInput * Acceleration * dt;
        }

        // Figure out how much extra dampening should happen because of drifing angle
        // Most dampening happens when car is at MaxDriftingAngle degrees to velocity
        var angleDiff = Vector3.Angle(RotationRoot.transform.forward, Vector3.ProjectOnPlane(_velocity * (Vector3.Dot(_velocity.normalized, RotationRoot.transform.forward) > 0f ? 1f : -1f), RotationRoot.transform.up));
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
        var localUpVelocity = Vector3.ProjectOnPlane(_velocity, RotationRoot.transform.up);
        var goingForward = Vector3.Dot(_velocity.normalized, RotationRoot.transform.forward) > 0f;

        var targetAngleDiff = 0f;

        // If on ground, car can be turned
        // Maximum turning angle is between RotationRoot.transform.forward and _velocity and is based on velocity inversely
        if (_onGround && localUpVelocity.magnitude > 0.001f)
        {
            var turningT = Mathf.Clamp(localUpVelocity.magnitude / MaxTurningVelocity, 0f, 1f);

            targetAngleDiff = turningT *
                _steeringInput *
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
            var targetRotation = Quaternion.AngleAxis(targetAngleDiff, RotationRoot.transform.up) * RotationRoot.transform.rotation;
            RotationRoot.transform.rotation = Quaternion.Slerp(RotationRoot.transform.rotation, targetRotation, DriftingAngleDampening);
        }

        // If bumping into objects (front, frontleft, frontright), decrease speed
        var velocityNormalized = _velocity.normalized;
        var hitRaycastOrigin = transform.position + 0.1f * transform.up;
        var hitRaycastRange = _controller.radius + 0.4f;
        RaycastHit hit;
        if (Physics.Raycast(hitRaycastOrigin, velocityNormalized, out hit, hitRaycastRange, ObstacleLayerMask) ||
           Physics.Raycast(hitRaycastOrigin, velocityNormalized + transform.right * -0.75f, out hit, hitRaycastRange, ObstacleLayerMask) ||
           Physics.Raycast(hitRaycastOrigin, velocityNormalized + transform.right * 0.75f, out hit, hitRaycastRange, ObstacleLayerMask))
        {
            var hitDot = Vector3.Dot(_velocity.normalized, Vector3.ProjectOnPlane(hit.normal, Vector3.up));
            if (_velocity.magnitude > DestroyOnImpactVelocity && hitDot < -0.5f)
            {
                DestroySelf();
                return;
            }

            _velocity = Vector3.ClampMagnitude(_velocity, ObstacleHitVelocityClamp);
        }

        // If on ground, map _velocity to local up plane
        if (_onGround)
        {
            _velocity = Vector3.ProjectOnPlane(_velocity, RotationRoot.transform.up);
        }

        // Add gravity
        _velocity += Vector3.down * Gravity * dt;

        // If on ground, make the car rotate to match the terrain angle
        // Raycast 3 times from front center and both back wheels downwards to find the current plane.
        // Make the car rotate to that rotation smoothly
        if (_onGround)
        {
            var raycastDistance = 4f;
            var frontLeftOrigin = RotationRoot.transform.position + RotationRoot.transform.forward * 1f + RotationRoot.transform.up * 2.5f + RotationRoot.transform.right * -1f;
            var frontRightOrigin = RotationRoot.transform.position + RotationRoot.transform.forward * 1f + RotationRoot.transform.up * 2.5f + RotationRoot.transform.right * 1f;
            var backLeftOrigin = RotationRoot.transform.position + RotationRoot.transform.forward * -1f + RotationRoot.transform.up * 2.5f + RotationRoot.transform.right * -1f;
            var backRightOrigin = RotationRoot.transform.position + RotationRoot.transform.forward * -1f + RotationRoot.transform.up * 2.5f + RotationRoot.transform.right * 1f;

            RaycastHit frontLeftHit, frontRightHit, backLeftHit, backRightHit;
            bool frontLeftWasHit, frontRightWasHit, backLeftWasHit, backRightWasHit;
            frontLeftWasHit = Physics.Raycast(frontLeftOrigin, RotationRoot.transform.up * -1f, out frontLeftHit, raycastDistance, GroundLayerMask);
            frontRightWasHit = Physics.Raycast(frontRightOrigin, RotationRoot.transform.up * -1f, out frontRightHit, raycastDistance, GroundLayerMask);
            backLeftWasHit = Physics.Raycast(backLeftOrigin, RotationRoot.transform.up * -1f, out backLeftHit, raycastDistance, GroundLayerMask);
            backRightWasHit = Physics.Raycast(backRightOrigin, RotationRoot.transform.up * -1f, out backRightHit, raycastDistance, GroundLayerMask);

            // Make two triangles. If all vertices of both triangles are defined, average the normal plane
            // Otherwise, use one of them
            // Forward vector is forward mapped to the new normal
            // If neither can be defined, do not rotate
            Plane[] planes = new Plane[2];
            var firstDefined = frontLeftWasHit && backLeftWasHit && backRightWasHit;
            var secondDefined = frontLeftWasHit && frontRightWasHit && backRightWasHit;

            if (firstDefined)
            {
                planes[0] = new Plane(frontLeftHit.point, backRightHit.point, backLeftHit.point);
            }
            if (secondDefined)
            {
                planes[1] = new Plane(frontLeftHit.point, frontRightHit.point, backRightHit.point);
            }

            Vector3 normal = Vector3.zero;

            if (firstDefined && secondDefined)
            {
                normal = (planes[0].normal + planes[1].normal) * 0.5f;
            }
            else if (firstDefined || secondDefined)
            {
                normal = firstDefined ? planes[0].normal : planes[1].normal;
            }

            // Don't allow the car to be flipped over too much
            var upDiff = Quaternion.FromToRotation(transform.up, normal).eulerAngles;
            if (upDiff.x > 180f) upDiff.x -= 360f;
            if (upDiff.z > 180f) upDiff.z -= 360f;
            upDiff.x = Mathf.Clamp(upDiff.x, -MaxTerrainAngle, MaxTerrainAngle);
            upDiff.z = Mathf.Clamp(upDiff.z, -MaxTerrainAngle, MaxTerrainAngle);
            var clampedNormal = Quaternion.Euler(upDiff) * transform.up;

            if (normal.magnitude > 0f)
            {
                var projectedForward = Vector3.ProjectOnPlane(RotationRoot.transform.forward, clampedNormal);
                _targetRotation = Quaternion.LookRotation(projectedForward, clampedNormal);
            }
        }

        // If the car ever ends up upside down, just target it to be the right way
        // Also target the car to be upwards when it's in the air
        if (!_onGround || Vector3.Dot(RotationRoot.transform.up, Vector3.up) < 0f)
        {
            var projectedForward = Vector3.ProjectOnPlane(RotationRoot.transform.forward, Vector3.up);
            _targetRotation = Quaternion.LookRotation(projectedForward, Vector3.up);
        }

        RotationRoot.transform.rotation = Quaternion.Slerp(RotationRoot.transform.rotation, _targetRotation, TerrainRotationLerpT * (_onGround ? 1f : 0.15f));

        // No skidmarks on air
        if (!_onGround)
        {
            showSkidMarks = false;
        }

        TrailRenderers[0].emitting = showSkidMarks;
        TrailRenderers[1].emitting = showSkidMarks;

        _controller.Move(_velocity);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnergyCollectible"))
        {
            GameManager.Instance.CollectiblesLeft -= 1;
            Destroy(other.gameObject);
            Energy += GameManager.Instance.EnergyPerCollectible;
            Energy = Mathf.Clamp(Energy, 0f, GameManager.Instance.SpawnEnergy);
        }
        else if (other.CompareTag("PlayerTrigger"))
        {
            if (Invulnerable) return;

            var otherController = other.transform.parent.GetComponent<CarController>();
            if (otherController.Invulnerable) return;

            otherController.Destroyed = true;

            // Destroy self only, the other car will destroy themselves
            DestroySelf();
        }
        else if (other.CompareTag("ExitZone"))
        {
            GameManager.Instance.LevelEndState = LevelEndState.Success;
            var nextScene = other.GetComponent<ExitZoneController>().NextScene;
            GameManager.Instance.EndLevel(nextScene);
        }
    }

    private void DestroySelf()
    {
        Destroyed = true;

        var particles = GameManager.Instance.ExplosionPSPool.GetPooledObject();
        particles.transform.position = transform.position;

        // If we are the active car, tell gamemanager to switch camera to some other car
        if (GameManager.Instance.ActiveCar == this)
        {
            GameManager.Instance.ActiveCarDestroyed();
        }

        gameObject.SetActive(false);
    }
}
