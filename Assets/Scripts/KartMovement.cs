using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartMovement : MonoBehaviour
{
    private RaycastHit _groundInfo;
    [SerializeField] private float _gravity;
    [SerializeField] private float _speed;
    [SerializeField] private float _acceleration;
    [SerializeField] private float _maxSteeringAngle = 45f;
    [SerializeField] private float _kartGroundNormalRotationSpeed = 10f;

    [SerializeField] private TextMeshProUGUI _speedText;

    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private float _maxWheelRotation = 60f;
    [SerializeField] private float _wheelRotationSpeed = .1f;

    public float SteerValue => MoveInput.x;
    public Vector2 MoveInput { get; private set; }
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    private Rigidbody _rigidbody;
    public RaycastHit GroundInfo => _groundInfo;

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
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, 1f))
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
        float yRotation = MoveInput.x * (_rigidbody.velocity.magnitude / _speed) * _maxSteeringAngle * (isMovingForward ? 1 : -1);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + yRotation, transform.eulerAngles.z);
    }

    private void HandleMove()
    {
        if (MoveInput.y > 0)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, _speed, Time.fixedDeltaTime * _acceleration);
        }
        else if (MoveInput.y < 0)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, -_speed / 2, Time.fixedDeltaTime * _acceleration);
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.fixedDeltaTime * _acceleration);
        }

        Vector3 desiredVelocity = transform.forward * CurrentSpeed;
        _rigidbody.velocity = desiredVelocity;
        Debug.DrawRay(transform.position, desiredVelocity * 5, Color.green, 2f);
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(transform.position, transform.forward * 5, Color.red, 2f);
    }
}
