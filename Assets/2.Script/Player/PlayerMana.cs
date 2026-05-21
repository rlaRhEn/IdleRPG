using System;
using UnityEngine;

/// <summary>플레이어 마나(현재/최대)와 변경 시 알림 이벤트를 제공합니다. HUD 등과 연결해 표시합니다.</summary>
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
