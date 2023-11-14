using System;
using UnityEngine;

public class KartAnimationController : MonoBehaviour
{

    public event Action OnLand;
    public void Land()
    {
        OnLand?.Invoke();
    }
}
