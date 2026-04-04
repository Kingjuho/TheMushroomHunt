using System;
using UnityEngine;

/// <summary>
/// 단일 로컬 슬롯에 기록할 플레이어 진행 상태
/// 버섯 HP, 리스폰 타이머, 현재 타겟 같은 일시 상태는 의도적으로 제외됨
/// </summary>
[Serializable]
public class SaveData
{
    public int gold;                    // 보유 골드
    public Vector3 playerPosition;      // 플레이어의 월드 좌표    
    public int attackPower;             // 최종 공격력
    public float attacksPerSecond;      // 최종 공격속도
    public float moveSpeed;             // 최종 이동속도(NavMeshAgent 기준)
}
