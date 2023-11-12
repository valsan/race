using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    LEFT,
    RIGHT
}

public class KartMovement : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField] private float _lensDistortionOverride;

    private RaycastHit _groundInfo;
    [SerializeField] private float _gravity;
    [SerializeField] private float _acceleration;
    [SerializeField] private float _maxSteeringAngle = 45f;
    [SerializeField] private float _kartGroundNormalRotationSpeed = 10f;

    [SerializeField] private TextMeshProUGUI _speedText;

    [Header("Boost")]
    [SerializeField] private float _maxBoostSpeed = 10f;
    [SerializeField] private float _boostDuration = 1f;

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
    [field: SerializeField] public float BaseMaxSpeed { get; private set; }
    [field: SerializeField] public float DriftCentrifugalForce { get; private set; }
    public float MaxSpeed { get; private set; }
    public float SteerValue => MoveInput.x;
    public Vector2 MoveInput { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsBoostActive { get; private set; }
    public RaycastHit GroundInfo => _groundInfo;
    public bool IsJumping { get; private set; }
    public bool IsDrifting { get; private set; }
    public float DriftStartTime { get; private set; }
    public float TimeSinceStartedDrifting => Time.time - DriftStartTime;


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

    private void OnLand()
    {
        IsJumping = false;
        // Do nothing if when landing there is not steer input
        if (Mathf.Approximately(SteerValue, 0f)) return;

        DriftDirection = SteerValue > 0f ? SteerDirection.RIGHT : SteerDirection.LEFT;

        // Only start drifting if player holding Space
        if (Input.GetKey(KeyCode.Space))
        {
            StartDrifting();
        }
    }


    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        MaxSpeed = BaseMaxSpeed;
    }
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        UpdateUI();
        KeepKartParallelToGround();
        HandleWheelRotation();
        HandleDriftLevel();

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivateBoost(DriftLevel.MEDIUM);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJump();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            StopDrift();
        }
    }


    private void FixedUpdate()
    {
        Physics.Raycast(transform.position, -transform.up, out _groundInfo, 1f);
        HandleMove();
        HandleSteer();
        HandleGravity();
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


    private void StopDrift()
    {
        if (!IsDrifting) return;
        IsDrifting = false;
        _particleSystemLeft.Stop();
        _particleSystemRight.Stop();
        ActivateBoost(DriftLevel);
        DriftLevel = DriftLevel.NONE;
    }
    private void OnJump()
    {
        IsJumping = true;
        _animator.SetTrigger("OnJump");
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
        float yRotation = 0f;
        if (IsDrifting)
        {
            float driftSteerValue = DriftDirection == SteerDirection.RIGHT ? SteerValue + 1.5f : SteerValue - 1.5f;
            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, _maxDriftKartRotation * driftSteerValue, 0), Time.deltaTime);

            // Apply outward force ("centrifugal force")
            _rigidbody.AddForce(transform.right * (DriftDirection == SteerDirection.RIGHT ? -1 : 1) * Time.fixedDeltaTime * DriftCentrifugalForce, ForceMode.Acceleration);
            maxSteeringAngleBasedOnCurrentSpeed = Mathf.Lerp(0, _maxSteeringAngle, _rigidbody.velocity.magnitude / BaseMaxSpeed);
            yRotation = driftSteerValue * maxSteeringAngleBasedOnCurrentSpeed;
        }
        else
        {
            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, 0, 0), Time.fixedDeltaTime * _kartRotatioResetSpeed);
            maxSteeringAngleBasedOnCurrentSpeed = Mathf.Lerp(0, _maxSteeringAngle, _rigidbody.velocity.magnitude / BaseMaxSpeed);
            yRotation = SteerValue * maxSteeringAngleBasedOnCurrentSpeed * (isMovingForward ? 1 : -1);
        }
        // TOOD: Steer more when drifting "inwards" and pull kart out when driftwing outwards



        transform.Rotate(Vector3.up, yRotation);
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
        if (MoveInput.y > 0)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, Time.fixedDeltaTime * _acceleration);
        }
        else if (MoveInput.y < 0)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, -MaxSpeed / 2, Time.fixedDeltaTime * _acceleration);
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.fixedDeltaTime * _acceleration);
        }

        Vector3 desiredVelocity = transform.forward * CurrentSpeed;
        _rigidbody.velocity = desiredVelocity;
        Debug.DrawRay(transform.position, desiredVelocity * 5, Color.green);
    }

    IEnumerator BoostCoroutine(DriftLevel driftLevel)
    {
        Debug.Log("START BOOST : " + DriftLevel);
        _turboParticleFX.Play();
        IsBoostActive = true;
        MaxSpeed = BaseMaxSpeed + (_maxBoostSpeed * ((float)(int)driftLevel + 1) / 3);
        float startTime = Time.time;
        float adaptedDuration = _boostDuration * (((float)(int)driftLevel + 1) / 2);
        float endTime = Time.time + adaptedDuration;
        yield return new WaitForSeconds(adaptedDuration);
        _turboParticleFX.Stop();
        MaxSpeed = BaseMaxSpeed;
        IsBoostActive = false;
        Debug.Log("END BOOST");
    }
}
