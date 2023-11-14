using UnityEngine;


[CreateAssetMenu(fileName = "KartMovementConfig", menuName = "ScriptableObjects/KartMovementConfig")]
public class KartMovementConfig : ScriptableObject
{
    public float MaxSpeed = 20f;
    public float Acceleration = 5f;
    public float MaxSteeringAngle = 45f;
    public float BoostDuration = 1f;
    public float BoostSpeed = 10f;
}
