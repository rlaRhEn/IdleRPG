using System;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [SerializeField] private float maxMana = 100f;
    private float currentMana;

    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public event Action<float, float> ManaChanged;

    void Awake()
    {
        currentMana = maxMana;
        Notify();
    }

    public void SetMaxMana(float newMax, bool refill)
    {
        maxMana = Mathf.Max(1f, newMax);
        if (refill)
            currentMana = maxMana;
        else
            currentMana = Mathf.Min(currentMana, maxMana);
        Notify();
    }

    void Notify()
    {
        ManaChanged?.Invoke(currentMana, maxMana);
    }
}
