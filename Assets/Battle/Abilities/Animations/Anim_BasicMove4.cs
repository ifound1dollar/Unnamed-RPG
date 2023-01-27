using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anim_BasicMove4 : AbilityAnimation
{
    public override void Setup()
    {
        Name = "Basic Move 4";
    }

    public override IEnumerator PlayerHitAnimation(BattleUnit playerUnit, BattleUnit enemyUnit)
    {
        //these methods will each implement custom animation, optionally doing custom basic attack animation
        playerUnit.PlayAttackAnimation();   //custom basic attack animation may not have movement at all
        yield return new WaitForSeconds(0.75f);
    }

    public override IEnumerator PlayerMissAnimation(BattleUnit playerUnit, BattleUnit enemyUnit)
    {
        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);
    }

    public override IEnumerator EnemyHitAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);
    }

    public override IEnumerator EnemyMissAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);
    }
}
