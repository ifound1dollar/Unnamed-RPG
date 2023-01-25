using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Battle/Species Data")]
public class SpeciesData : ScriptableObject
{
    [SerializeField] string speciesName;

    [SerializeField] BattleType type1;
    [SerializeField] BattleType type2;

    [SerializeField] float xpRatio = 1.0f;

    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;

    [SerializeField] int hp = 100;
    [SerializeField] int strength = 100;
    [SerializeField] int mastery = 100;
    [SerializeField] int armor = 100;
    [SerializeField] int resistance = 100;
    [SerializeField] int agility = 100;

    [SerializeField] List<LearnedAbility> learnedAbilities;

    [Header("Passive Abilities : 1 & 2 -> 45% chance each, 3 -> 10% chance")]
    [SerializeField] SpecialAbility passiveAbility1;
    [SerializeField] SpecialAbility passiveAbility2;
    [SerializeField] SpecialAbility passiveAbility3;

    [Header("Evolution Data")]
    [SerializeField] int currentEvoStage = 1;
    [SerializeField] int totalEvoStages = 1;
    [SerializeField] List<EvolutionData> evolutions;


    public string SpeciesName   { get { return speciesName; } }
    public BattleType Type1     { get { return type1; } }
    public BattleType Type2     { get { return type2; } }

    public float XPRatio        { get { return xpRatio; } }

    public Sprite FrontSprite { get { return frontSprite; } }
    public Sprite BackSprite    { get { return backSprite; } }

    public int HP               { get { return hp; } }
    public int Strength         { get { return strength; } }
    public int Mastery          { get { return mastery; } }
    public int Armor            { get { return armor; } }
    public int Resistance       { get { return resistance; } }
    public int Agility          { get { return agility; } }

    public List<LearnedAbility> LearnedAbilities { get { return learnedAbilities; } }

    public int CurrentEvoStage { get { return currentEvoStage; } }
    public int TotalEvoStages { get { return totalEvoStages; } }
    public List<EvolutionData> Evolutions { get { return evolutions; } }

    public SpecialAbility PassiveAbility1 { get { return passiveAbility1; } }
    public SpecialAbility PassiveAbility2 { get { return passiveAbility2; } }
    public SpecialAbility PassiveAbility3 { get { return passiveAbility3; } }
}
