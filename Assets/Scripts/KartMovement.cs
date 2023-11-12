using PrimeTween;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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

    [SerializeField] private ParticleSystem _particleSystemLeft;
    [SerializeField] private ParticleSystem _particleSystemRight;
    [SerializeField] private ParticleSystem _turboParticleFX;
    [SerializeField] private GameObject _kartVisuals;
    [SerializeField] private float _maxDriftKartRotation = 20f;
    [SerializeField] private float _kartRotatioResetSpeed = 10f;

    [field: SerializeField] public float BaseMaxSpeed { get; private set; }
    public float MaxSpeed { get; private set; }
    public float SteerValue => MoveInput.x;
    public Vector2 MoveInput { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsBoostActive { get; private set; }
    public RaycastHit GroundInfo => _groundInfo;

    public bool IsJumping { get; private set; }
    private bool _isDrifting;
    public bool IsDrifting
    {
        get => _isDrifting; private set
        {
            if (value)
            {
                Debug.Log("START DRIFT");
                _particleSystemLeft.Play();
                _particleSystemRight.Play();
            }
            else
            {
                Debug.Log("END DRIFT");
                _particleSystemLeft.Stop();
                _particleSystemRight.Stop();
            }
            _isDrifting = value;
        }
    }

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

        // Start Drifting
        if (Input.GetKey(KeyCode.Space))
        {
            IsDrifting = true;
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

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivateBoost();
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

    private void StopDrift()
    {
        if (!IsDrifting) return;
        IsDrifting = false;

        ActivateBoost();
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

    private void FixedUpdate()
    {
        Physics.Raycast(transform.position, -transform.up, out _groundInfo, 1f);
        HandleMove();
        HandleSteer();
        HandleGravity();
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
        float maxSteeringAngleBasedOnCurrentSpeed = Mathf.Lerp(0, _maxSteeringAngle, _rigidbody.velocity.magnitude / BaseMaxSpeed);
        float yRotation = MoveInput.x * maxSteeringAngleBasedOnCurrentSpeed * (isMovingForward ? 1 : -1);
        transform.Rotate(Vector3.up, yRotation);
        if (IsDrifting)
        {
            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, _maxDriftKartRotation * MoveInput.x, 0), Time.deltaTime);
            //_rigidbody.AddForce(transform.right * MoveInput.x);
        }
        else
        {
            _kartVisuals.transform.localRotation = Quaternion.Lerp(_kartVisuals.transform.localRotation, Quaternion.Euler(0, 0, 0), Time.fixedDeltaTime * _kartRotatioResetSpeed);
        }
    }

    /// <summary>
    /// Boosts desired speed for 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ActivateBoost()
    {
        StartCoroutine(BoostCoroutine());
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

    IEnumerator BoostCoroutine()
    {
        if (_volume.profile.TryGet<LensDistortion>(out LensDistortion lensDistortion))
        {
            _turboParticleFX.Play();
            IsBoostActive = true;
            MaxSpeed = BaseMaxSpeed + _maxBoostSpeed;
            Tween.Custom(0, -1, _boostDuration * 0.8f, onValueChange: newVal => lensDistortion.intensity.Override(newVal));
            float startTime = Time.time;
            float endTime = Time.time + _boostDuration;
            while (Time.time < endTime)
            {
                lensDistortion.intensity.Override(Mathf.Lerp(0, _lensDistortionOverride, (Time.time - startTime) / (_boostDuration / 2)));
                yield return null;
            }
            _turboParticleFX.Stop();
            lensDistortion.intensity.Override(0);
            MaxSpeed = BaseMaxSpeed;
            IsBoostActive = false;
        };
    }
}
