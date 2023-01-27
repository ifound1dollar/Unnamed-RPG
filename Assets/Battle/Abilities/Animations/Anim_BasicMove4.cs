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
        //custom basic attack animation may not have movement at all

        playerUnit.PlayAttackAnimation();       //0.25s per one-way movement, total 0.5s
        yield return new WaitForSeconds(0.30f); //wait for first half of attack animation + 0.05s
        enemyUnit.PlayShakingAnimation();       //0.1s for first/last half-movement, 0.2s for other 3, total 0.8s
        yield return new WaitForSeconds(0.80f); //wait for entire shaking animation
    }

    public override IEnumerator PlayerMissAnimation(BattleUnit playerUnit, BattleUnit enemyUnit)
    {
        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.50f); //wait for entire attack animation
    }

    public override IEnumerator EnemyHitAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.50f); //wait for entire attack animation
    }

    public override IEnumerator EnemyMissAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.50f); //wait for entire attack animation
    }
}
