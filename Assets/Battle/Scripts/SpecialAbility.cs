using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Special Ability")]
public class SpecialAbility : ScriptableObject
{
    [SerializeField] string abilityName;
    [TextArea]
    [SerializeField] string abilityDescription;


    public string Name { get { return abilityName; } }
    public string Description { get { return abilityDescription; } }
}
