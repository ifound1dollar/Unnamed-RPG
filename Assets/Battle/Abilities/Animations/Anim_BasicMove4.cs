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
        Debug.Log("PlayerHitAnimation called.");

        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);

        enemyUnit.PlayDamagedAnimation();
        yield return new WaitForSeconds(0.5f);

        yield break;
    }

    public override IEnumerator PlayerMissAnimation(BattleUnit playerUnit, BattleUnit enemyUnit)
    {
        Debug.Log("PlayerMissAnimation called.");

        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);

        //enemyUnit.PlayDamagedAnimation();
        //yield return new WaitForSeconds(0.5f);

        yield break;
    }

    public override IEnumerator EnemyHitAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        Debug.Log("EnemyHitAnimation called.");

        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);

        playerUnit.PlayDamagedAnimation();
        yield return new WaitForSeconds(0.5f);

        yield break;
    }

    public override IEnumerator EnemyMissAnimation(BattleUnit enemyUnit, BattleUnit playerUnit)
    {
        Debug.Log("EnemyMissAnimation called.");

        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.75f);

        //playerUnit.PlayDamagedAnimation();
        //yield return new WaitForSeconds(0.5f);

        yield break;
    }
}
