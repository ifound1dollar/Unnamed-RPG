using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityData
{
    public BattleChar User { get; set; }
    public BattleChar Target { get; set; }

    public int Damage { get; set; }
    public float Effectiveness { get; set; }
    public bool CriticalHit { get; set; }

    public int TurnNumber { get; set; }

    public AbilityData(BattleChar user, BattleChar target, int turnNumber)
    {
        User = user;
        Target = target;
        TurnNumber = turnNumber;
    }
}
