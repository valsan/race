using Cinemachine;
using UnityEngine;

public class KartCameraController : MonoBehaviour
{
    [SerializeField] private float _minOffset = 4f;
    [SerializeField] private float _maxOffset;
    [SerializeField] private CinemachineVirtualCamera _camera;
    private KartMovement _kartMovement;
    private void Awake()
    {
        _kartMovement = GetComponent<KartMovement>();
    }

    private void LateUpdate()
    {
        if (_kartMovement != null)
        {
            var transposer = _camera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset.z = Mathf.Lerp(_minOffset, _maxOffset, _kartMovement.CurrentSpeed / _kartMovement.MaxSpeed);
            }
        }
    }

}
