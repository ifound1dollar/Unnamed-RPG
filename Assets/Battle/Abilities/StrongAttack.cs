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

        AbilityType = BattleType.Vital;
        Category = Category.Physical;
        MakesContact = true;
    }

    public override IEnumerator UseAbility(AbilityData data)
    {
        yield break;
    }
    protected override void CalcSpecificScore(BattleChar user, BattleChar target)
    {
        CalcDamagingScore(user, target, EstimateDamage(user, target));
    }
}
