using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class AbilityAnimation : MonoBehaviour
{
    //[SerializeField] List<Sprite> animationSprites;

    public string Name { get; protected set; }
    //public List<Sprite> AnimationSprites { get { return animationSprites; } }


    public abstract void Setup();
    public abstract IEnumerator PlayerHitAnimation(BattleUnit playerUnit, BattleUnit enemyUnit);
    public abstract IEnumerator PlayerMissAnimation(BattleUnit playerUnit, BattleUnit enemyUnit);
    public abstract IEnumerator EnemyHitAnimation(BattleUnit enemyUnit, BattleUnit playerUnit);
    public abstract IEnumerator EnemyMissAnimation(BattleUnit enemyUnit, BattleUnit playerUnit);
}
