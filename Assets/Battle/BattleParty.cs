using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleParty : MonoBehaviour
{
    [Header("Minimum 1, maximum 5")]
    [SerializeField] ScriptableBattleChar[] team;

    public ScriptableBattleChar[] Team { get { return team; } }
}
