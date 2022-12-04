using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrongAttack : Ability
{
    public StrongAttack()
    {
        Name = "Strong Attack";
        Power = 65;
        Accuracy = 100;
        Energy = 4;
        Description = "A strong damaging attack.";
    }

    public override void UseAbility(AbilityData data)
    {

    }
    protected override void CalcSpecificScore(BattleChar user, BattleChar target)
    {
        CalcDamagingScore(user, target, EstimateDamage(user, target));
    }
}
