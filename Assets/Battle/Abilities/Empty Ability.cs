using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyAbility : Ability
{
    public EmptyAbility()
    {
        Name = "EMPTY";
    }

    public override IEnumerator UseAbility(AbilityData data)
    {
        Debug.Log("Tried to use Empty Ability.");
        yield break;
    }
    protected override void CalcSpecificScore(BattleAI.AIContextObject aiContext)
    {
        //Score remains at 0
    }

    public override bool IsUsable(BattleChar battleChar)
    {
        return false;
    }
}
