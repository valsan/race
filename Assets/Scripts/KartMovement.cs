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

    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private float _maxWheelRotation = 60f;
    [SerializeField] private float _wheelRotationSpeed = .1f;

    [field: SerializeField] public float MaxSpeed { get; private set; }
    public float SteerValue => MoveInput.x;
    public Vector2 MoveInput { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public RaycastHit GroundInfo => _groundInfo;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

    }
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        UpdateUI();
        HandleKartRotation();
        HandleWheelRotation();

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivateBoost();
        }
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
    private void HandleKartRotation()
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
        float yRotation = MoveInput.x * (_rigidbody.velocity.magnitude / MaxSpeed) * _maxSteeringAngle * (isMovingForward ? 1 : -1);
        transform.Rotate(Vector3.up, yRotation);
    }

    [SerializeField] private float _maxBoostSpeed = 10f;
    [SerializeField] private float _boostDuration = 1f;
    public float CurrentBoostSpeed = 0f;
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

        //CurrentSpeed += CurrentBoostSpeed;
        Vector3 desiredVelocity = transform.forward * CurrentSpeed;
        _rigidbody.velocity = desiredVelocity;
        Debug.DrawRay(transform.position, desiredVelocity * 5, Color.green);
    }

    IEnumerator BoostCoroutine()
    {
        if (_volume.profile.TryGet<LensDistortion>(out LensDistortion lensDistortion))
        {
            float startTime = Time.time;
            float endTime = Time.time + _boostDuration;
            while (Time.time < endTime)
            {
                lensDistortion.intensity.Override(Mathf.Lerp(0, _lensDistortionOverride, (Time.time - startTime) / (_boostDuration / 2)));
                CurrentBoostSpeed = _maxBoostSpeed;
                yield return null;
            }
            CurrentBoostSpeed = 0f;
            lensDistortion.intensity.Override(0);
        };
    }
}
