using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Evolution Data")]
public class EvolutionData : ScriptableObject
{
    [SerializeField] SpeciesData evolutionSpecies;

    [Header("Evolution method data, at least one required")]
    [SerializeField] int level;
    [SerializeField] string itemName;
    [SerializeField] string eventName;
}
