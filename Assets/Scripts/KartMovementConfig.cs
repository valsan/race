using UnityEngine;


[CreateAssetMenu(fileName = "KartMovementConfig", menuName = "ScriptableObjects/KartMovementConfig")]
public class KartMovementConfig : ScriptableObject
{
    public float MaxSpeed = 20f;
    public float Acceleration = 5f;
    public float Deceleration = 5f; // Decelaration when not throttling
    public float MaxSteeringAngle = 45f;
    public float MaxDriftAngle = 1.5f;
    public float BoostDuration = 1f;
    public float BoostSpeed = 10f;
}
