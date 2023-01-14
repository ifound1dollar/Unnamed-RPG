using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Learned Ability")]
public class LearnedAbility : ScriptableObject
{
    [SerializeField] int level;
    [SerializeField] string abilityName;

    public int Level { get { return level; } }
    public string Name { get { return abilityName; } }
}
