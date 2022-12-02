using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyAbility : Ability
{
    public EmptyAbility()
    {
        Name = "EMPTY";
    }
    public override void UseAbility(AbilityData data)
    {
        Debug.Log("Tried to use Empty Ability.");
    }
    protected override void CalcSpecificScore(BattleChar user, BattleChar target)
    {
        //Score remains at 0
    }
}
