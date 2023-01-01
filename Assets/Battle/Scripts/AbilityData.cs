using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityData
{
    public BattleChar User { get; set; }
    public BattleChar Target { get; set; }

    public int Damage { get; set; }
    public float Effectiveness { get; set; } = 1.0f;
    public bool CriticalHit { get; set; }

    public int TurnNumber { get; set; }
    public DialogBox DialogBox { get; set; }
    public float TextDelay { get; set; }
    public PlayerHud PlayerHud { get; set; }
    public EnemyHud EnemyHud { get; set; }

    public AbilityData(BattleChar user, BattleChar target, int turnNumber, DialogBox dialogBox,
        float textDelay, PlayerHud playerHud, EnemyHud enemyHud)
    {
        User = user;
        Target = target;
        TurnNumber = turnNumber;
        DialogBox = dialogBox;
        TextDelay = textDelay;
        PlayerHud = playerHud;
        EnemyHud = enemyHud;
    }
}
