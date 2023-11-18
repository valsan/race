using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private KartMovement _kart;

    private InputAction _jumpAction;
    private InputAction _throttleAction;
    private InputAction _reverseAction;

    private PlayerInput _playerInput;
    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _jumpAction = _playerInput.actions["Jump"];
        _throttleAction = _playerInput.actions["Throttle"];
        _reverseAction = _playerInput.actions["Reverse"];

        _jumpAction.started += OnJumpPressed;
        _jumpAction.canceled += OnJumpReleased;

        _throttleAction.started += OnThrottlePressed;
        _throttleAction.canceled += OnThrottleReleased;

        _reverseAction.started += OnReversePressed;
        _reverseAction.canceled += OnReverseReleased;
    }

    public void OnMove(InputValue value)
    {
        _kart.MoveInput = value.Get<Vector2>();
    }

    public void OnJumpPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Jump Pressed");
        _kart.OnJump();
        _kart.IsHoldingDrift = true;
    }
    public void OnJumpReleased(InputAction.CallbackContext context)
    {
        Debug.Log("Jump Released");
        _kart.StopDrift();
        _kart.IsHoldingDrift = false;
    }

    public void OnThrottlePressed(InputAction.CallbackContext context)
    {
        Debug.Log("OnThrottlePressed");
        _kart.Throttle = true;
    }
    public void OnThrottleReleased(InputAction.CallbackContext context)
    {
        Debug.Log("OnThrottleReleased");
        _kart.Throttle = false;
    }

    public void OnReversePressed(InputAction.CallbackContext context)
    {
        Debug.Log("OnThrottlePressed");
        _kart.Reverse = true;
    }
    public void OnReverseReleased(InputAction.CallbackContext context)
    {
        Debug.Log("OnThrottleReleased");
        _kart.Reverse = false;
    }
}
