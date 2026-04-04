using System;
using UnityEngine;

public class PlayerGoldWallet : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField] private int currentGold = 0;

    public int CurrentGold => currentGold;

    // 나중에 UI가 붙을 때를 위해 변경 이벤트 미리 선언
    public event System.Action<int, int> GoldChanged;

    /// <summary>
    /// 골드를 획득할 때 호출
    /// amount: 변동량
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentGold += amount;
        GoldChanged?.Invoke(currentGold, amount);

        Debug.Log($"Gold gained: {amount}, current gold: {currentGold}");
    }

    /// <summary>
    /// 강화/구매 시 호출
    /// </summary>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (currentGold < amount)
        {
            return false;
        }

        currentGold -= amount;
        GoldChanged?.Invoke(currentGold, -amount);

        Debug.Log($"Gold spent: {amount}, current gold: {currentGold}");
        return true;
    }

    /// <summary>
    /// 세이브파일의 골드 값을 복원할 때 호출
    /// 로드 직후 HUD가 최신 값을 반영하도록 이벤트도 함께 발생시킴
    /// </summary>
    public bool TrySetGold(int amount, bool notifyListeners = true)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"{nameof(PlayerGoldWallet)}: gold cannot be negative.", this);
            return false;
        }

        int previousGold = currentGold;
        currentGold = amount;

        if (notifyListeners)
        {
            GoldChanged?.Invoke(currentGold, currentGold - previousGold);
        }

        return true;
    }
}