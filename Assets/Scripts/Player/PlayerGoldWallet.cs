using System;
using UnityEngine;

public class PlayerGoldWallet : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField] private int currentGold = 0;

    public int CurrentGold => currentGold;

    // 나중에 UI가 붙을 때를 위해 변경 이벤트 미리 선언
    public event Action<int, int> GoldChanged;

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
}