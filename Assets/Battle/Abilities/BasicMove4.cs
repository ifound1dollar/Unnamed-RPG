using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicMove4 : Ability
{
    public BasicMove4()
    {
        Name = "Basic Move 4";
        Power = 40;
        Accuracy = 100;
        Energy = 1;
        Description = "A simple damaging attack.";

        AbilityType = BattleType.Vital;
        Category = Category.Physical;
        MakesContact = true;
    }

    public override IEnumerator UseAbility(AbilityData data)
    {
        CalcDamageToDeal(data);
        data.Target.TakeDamage(data.Damage);

        yield return UpdateHudAndDelay(data);       //call for all HUD updates and delays
        yield return UpdateDialogUniversal(data);   //do not call this method with custom dialog

        yield break;
    }
    protected override void CalcSpecificScore(BattleChar user, BattleChar target)
    {
        CalcDamagingScore(user, target, EstimateDamage(user, target));
    }
}
