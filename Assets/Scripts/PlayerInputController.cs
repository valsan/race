using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] KartMovement _kart;

    private InputAction _jumpAction;

    private PlayerInput _playerInput;
    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _jumpAction = _playerInput.actions["Jump"];

        _jumpAction.started += OnJumpPressed;
        _jumpAction.canceled += OnJumpReleased;
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

}
