using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public enum DriftLevel
{
    NONE,
    BASE,
    MEDIUM,
    STRONG,
}

public enum SteerDirection
{
    LEFT = -1,
    RIGHT = 1
}

public class KartMovement : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField] private float _lensDistortionOverride;

    [SerializeField] private KartMovementConfig _movementConfig;

    private RaycastHit _groundInfo;
    [SerializeField] private float _gravity;
    [SerializeField] private float _kartGroundNormalRotationSpeed = 10f;

    [SerializeField] private TextMeshProUGUI _speedText;

    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private float _maxWheelRotation = 60f;
    [SerializeField] private float _wheelRotationSpeed = .1f;

    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _kartVisuals;

    [SerializeField] private ParticleSystem _turboParticleFX;
    [SerializeField] private ParticleSystem _particleSystemLeft;
    [SerializeField] private ParticleSystem _particleSystemRight;
    [SerializeField] private float _maxDriftKartRotation = 20f;
    [SerializeField] private float _kartRotatioResetSpeed = 10f;

    [Header("Driting")]
    [SerializeField] private float _mediumBoostDriftTime = 1f;
    [SerializeField] private float _strongBoostDriftTime = 3f;

    [SerializeField] private Color _baseBoostColor = Color.cyan;
    [SerializeField] private Color _mediumBoostColor = Color.yellow;
    [SerializeField] private Color _strongBoostColor = Color.magenta;

    public DriftLevel DriftLevel { get; private set; } = DriftLevel.NONE;
    public SteerDirection DriftDirection { get; private set; }
    [field: SerializeField] public float DriftCentrifugalForce { get; private set; }
    public float MaxSpeed { get; private set; }
    public float SteerValue => MoveInput.x;
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsBoostActive { get; private set; }
    public RaycastHit GroundInfo => _groundInfo;
    public bool IsJumping { get; private set; }
    public bool IsDrifting { get; private set; }
    public float DriftStartTime { get; private set; }
    public float TimeSinceStartedDrifting => Time.time - DriftStartTime;
    // Controller controls these
    public bool IsHoldingDrift { get; set; }
    public bool Throttle { get; set; }
    public bool Reverse { get; set; }
    public Vector2 MoveInput { get; set; }

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
    private void OnEnable()
    {
        var animationController = GetComponentInChildren<KartAnimationController>();
        animationController.OnLand += OnLand;
    }


    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        MaxSpeed = _movementConfig.MaxSpeed;
    }


    private void Update()
    {
        UpdateUI();
        KeepKartParallelToGround();
        HandleWheelRotation();
        HandleDriftLevel();
    }

    private void FixedUpdate()
    {
        Physics.Raycast(transform.position, -transform.up, out _groundInfo, 1f);
        HandleMove();
        HandleSteer();
        HandleGravity();
    }


    public void OnJump()
    {
        IsJumping = true;
        _animator.SetTrigger("OnJump");
    }

    private void OnLand()
    {
        IsJumping = false;
        // Do nothing if when landing there is not steer input
        if (Mathf.Approximately(SteerValue, 0f)) return;

        DriftDirection = SteerValue > 0f ? SteerDirection.RIGHT : SteerDirection.LEFT;

        Debug.Log("IS HOLDING DRIGT: " + IsHoldingDrift);
        // Only start drifting if player holding Space
        if (IsHoldingDrift)
        {
            StartDrifting();
        }
    }




    private void HandleDriftLevel()
    {
        if (IsDrifting)
        {
            if (TimeSinceStartedDrifting < _mediumBoostDriftTime)
            {
                if (DriftLevel != DriftLevel.BASE)
                {
                    Debug.Log("BASE BOOST");
                    DriftLevel = DriftLevel.BASE;
                    SwapParticleEffectsColor(_baseBoostColor);
                }
            }
            else if (TimeSinceStartedDrifting < _strongBoostDriftTime)
            {
                if (DriftLevel != DriftLevel.MEDIUM)
                {
                    Debug.Log("MEDIUM BOOST");
                    DriftLevel = DriftLevel.MEDIUM;
                    SwapParticleEffectsColor(_mediumBoostColor);
                }
            }
            else
            {
                if (DriftLevel != DriftLevel.STRONG)
                {
                    Debug.Log("STRONG");
                    DriftLevel = DriftLevel.STRONG;
                    SwapParticleEffectsColor(_strongBoostColor);
                }
            }
        }
    }

    private void SwapParticleEffectsColor(Color newColor)
    {
        var main = _particleSystemLeft.main;
        var mainRight = _particleSystemRight.main;
        main.startColor = newColor;
        mainRight.startColor = newColor;
        foreach (var child in _particleSystemLeft.GetComponentsInChildren<ParticleSystem>())
        {
            var childMain = child.main;
            childMain.startColor = newColor;
        }
        foreach (var child in _particleSystemRight.GetComponentsInChildren<ParticleSystem>())
        {
            var childMain = child.main;
            childMain.startColor = newColor;
        }
    }

    private void StartDrifting()
    {
        IsDrifting = true;
        DriftStartTime = Time.time;
        _particleSystemLeft.Play();
        _particleSystemRight.Play();
    }

    public void StopDrift()
    {
        if (!IsDrifting) return;
        IsDrifting = false;
        _particleSystemLeft.Stop();
        _particleSystemRight.Stop();
        ActivateBoost(DriftLevel);
        DriftLevel = DriftLevel.NONE;
    }

    /// <summary>
    /// Like Stop Drift, but doesn't activate boost
    /// </summary>
    public void CancelDrift()
    {
        if (!IsDrifting) return;
        IsDrifting = false;
        _particleSystemLeft.Stop();
        _particleSystemRight.Stop();
        DriftLevel = DriftLevel.NONE;
    }

    private void HandleWheelRotation()
    {
        var step = _wheelRotationSpeed * Time.deltaTime;

        _frontLeftWheel.localRotation = Quaternion.RotateTowards(_frontLeftWheel.localRotation, Quaternion.Euler(0, MoveInput.x * _maxWheelRotation, 0), step);
        _frontRightWheel.localRotation = Quaternion.RotateTowards(_frontRightWheel.localRotation, Quaternion.Euler(0, MoveInput.x * _maxWheelRotation, 0), step);
    }

    private void UpdateUI()
    {
        _speedText.text = string.Format("SPEED: {0:0.00}", transform.InverseTransformDirection(_rigidbody.velocity).magnitude);
    }

    private void HandleGravity()
    {
        _rigidbody.AddForce(-GroundInfo.normal * _gravity, ForceMode.Acceleration);
    }


    /// <summary>
    /// Keeps the Kart parallel to the ground
    /// </summary>
    private void KeepKartParallelToGround()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, 2f))
        {
            IsGrounded = true;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation, Time.deltaTime * _kartGroundNormalRotationSpeed);
        }
        else
        {
            IsGrounded = false;
        }

    }

    private void HandleSteer()
    {
        bool isMovingForward = transform.InverseTransformDirection(_rigidbody.velocity).z > 0;
        float maxSteeringAngleBasedOnCurrentSpeed;
        float yRotation;
        if (IsDrifting)
        {
            StopDrifingIfSlowedDownTooMuch();
            float driftDirectionModifier = DriftDirection == SteerDirection.RIGHT ? 1 : -1f;
            float positiveSteerValue = DriftDirection == SteerDirection.RIGHT ? Mathf.InverseLerp(-1, 1, SteerValue) : Mathf.InverseLerp(1, -1, SteerValue);

            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, _maxDriftKartRotation * positiveSteerValue * driftDirectionModifier, 0), Time.deltaTime);


            // Apply outward force ("centrifugal force")
            //_rigidbody.AddForce(transform.right * (DriftDirection == SteerDirection.RIGHT ? -1 : 1) * Time.fixedDeltaTime * DriftCentrifugalForce, ForceMode.Acceleration);
            maxSteeringAngleBasedOnCurrentSpeed = Mathf.Lerp(0, positiveSteerValue * _movementConfig.MaxDriftAngle * driftDirectionModifier, _rigidbody.velocity.magnitude / _movementConfig.MaxSpeed);
            yRotation = positiveSteerValue * maxSteeringAngleBasedOnCurrentSpeed;
        }
        else
        {
            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, 0, 0), Time.fixedDeltaTime * _kartRotatioResetSpeed);
            maxSteeringAngleBasedOnCurrentSpeed = Mathf.Lerp(0, _movementConfig.MaxSteeringAngle, _rigidbody.velocity.magnitude / _movementConfig.MaxSpeed);
            yRotation = SteerValue * maxSteeringAngleBasedOnCurrentSpeed * (isMovingForward ? 1 : -1);
        }
        transform.Rotate(Vector3.up, yRotation);
    }

    private void StopDrifingIfSlowedDownTooMuch()
    {
        if (CurrentSpeed < _movementConfig.MinSpeedToDrift) CancelDrift();
    }

    /// <summary>
    /// Boosts desired speed for 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ActivateBoost(DriftLevel driftLevel)
    {
        StartCoroutine(BoostCoroutine(driftLevel));
    }

    private void HandleMove()
    {
        if (Throttle)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, Time.fixedDeltaTime * _movementConfig.Acceleration);
        }
        else if (Reverse)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, -MaxSpeed / 2, Time.fixedDeltaTime * _movementConfig.Acceleration);
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.fixedDeltaTime * _movementConfig.Deceleration);
        }

        Vector3 desiredVelocity = transform.forward * CurrentSpeed;
        _rigidbody.velocity = desiredVelocity;
        Debug.DrawRay(transform.position, desiredVelocity * 5, Color.green);
    }

    private IEnumerator BoostCoroutine(DriftLevel driftLevel)
    {
        Debug.Log("START BOOST : " + DriftLevel);
        _turboParticleFX.Play();
        IsBoostActive = true;
        MaxSpeed = _movementConfig.MaxSpeed + (_movementConfig.BoostSpeed * ((float)(int)driftLevel + 1) / 3);
        float startTime = Time.time;
        float adaptedDuration = _movementConfig.BoostDuration * (((float)(int)driftLevel + 1) / 2);
        float endTime = Time.time + adaptedDuration;
        yield return new WaitForSeconds(adaptedDuration);
        _turboParticleFX.Stop();
        MaxSpeed = _movementConfig.MaxSpeed;
        IsBoostActive = false;
        Debug.Log("END BOOST");
    }
}
