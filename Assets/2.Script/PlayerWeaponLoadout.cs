using System;
using UnityEngine;

/// <summary>무기 구매/장착 상태와 전투력 가중치를 관리합니다.</summary>
public class PlayerWeaponLoadout : MonoBehaviour
{
    readonly bool[] _owned = new bool[20];
    int _equippedIndex = -1;
    AutoLightningStrikeSkill _autoLightningStrikeSkill;

    void Awake()
    {
        _autoLightningStrikeSkill = GetComponent<AutoLightningStrikeSkill>();
        if (_autoLightningStrikeSkill != null)
            _autoLightningStrikeSkill.SetUnlocked(false);
    }

    public event Action LoadoutChanged;
    public int EquippedWeaponIndex => _equippedIndex;
    public int OwnedWeaponCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _owned.Length; i++)
                if (_owned[i]) count++;
            return count;
        }
    }

    public int CurrentEquippedWeaponPower => GetWeaponPower(_equippedIndex);

    public bool IsOwned(int index)
    {
        return index >= 0 && index < _owned.Length && _owned[index];
    }

    public bool IsEquipped(int index)
    {
        return _equippedIndex == index;
    }

    public bool BuyAndEquip(int index)
    {
        if (index < 0 || index >= _owned.Length) return false;
        _owned[index] = true;
        UnlockWeaponPassiveIfNeeded(index);
        return Equip(index);
    }

    public bool Equip(int index)
    {
        if (!IsOwned(index)) return false;
        _equippedIndex = index;
        LoadoutChanged?.Invoke();
        return true;
    }

    public int GetWeaponCost(int index)
    {
        if (index < 0) return int.MaxValue;
        if (index == 0) return 40;
        if (index == 1) return 75;
        return int.MaxValue;
    }

    public int GetWeaponPower(int index)
    {
        if (index < 0) return 0;
        if (index == 0) return 45;
        if (index == 1) return 80;
        return 0;
    }

    public string GetOwnedStateString()
    {
        char[] chars = new char[_owned.Length];
        for (int i = 0; i < _owned.Length; i++)
            chars[i] = _owned[i] ? '1' : '0';
        return new string(chars);
    }

    public void LoadState(string ownedState, int equippedIndex)
    {
        if (!string.IsNullOrEmpty(ownedState))
        {
            int max = Mathf.Min(_owned.Length, ownedState.Length);
            for (int i = 0; i < max; i++)
                _owned[i] = ownedState[i] == '1';
        }

        _equippedIndex = (_owned.Length > equippedIndex && equippedIndex >= 0 && _owned[equippedIndex])
            ? equippedIndex
            : -1;

        if (IsOwned(0))
            UnlockWeaponPassiveIfNeeded(0);
        else if (_autoLightningStrikeSkill != null)
            _autoLightningStrikeSkill.SetUnlocked(false);

        LoadoutChanged?.Invoke();
    }

    public void ResetLoadoutForTest()
    {
        for (int i = 0; i < _owned.Length; i++)
            _owned[i] = false;
        _equippedIndex = -1;

        if (_autoLightningStrikeSkill != null)
            _autoLightningStrikeSkill.SetUnlocked(false);

        LoadoutChanged?.Invoke();
    }

    void UnlockWeaponPassiveIfNeeded(int weaponIndex)
    {
        // 1번 무기(번개검) 구매 시 자동 번개 패시브를 해금합니다.
        if (weaponIndex != 0) return;

        if (_autoLightningStrikeSkill == null)
            _autoLightningStrikeSkill = gameObject.AddComponent<AutoLightningStrikeSkill>();

        _autoLightningStrikeSkill.SetUnlocked(true);
    }

}
