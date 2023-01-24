using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Evolution Data")]
public class EvolutionData : ScriptableObject
{
    public enum EvoEvent { UNDEFINED, LevelUp, IsSlain, SlayTarget, ItemUse, OverworldEvent }

    [SerializeField] SpeciesData evolutionSpecies;
    [SerializeField] EvoEvent eventType;

    [Header("Evolution event data, corresponds to specified event type")]
    [SerializeField] int level;
    [SerializeField] string slayTargetName;
    [SerializeField] string itemName;
    [SerializeField] string overworldEventName;


    public SpeciesData EvolutionSpecies { get { return evolutionSpecies; } }
    public EvoEvent EventType { get { return eventType; } }

    public int Level { get { return level; } }
    public string SlayTargetName { get { return slayTargetName; } }
    public string ItemName { get { return itemName; } }
    public string OverworldEventName { get { return overworldEventName; } }
}
