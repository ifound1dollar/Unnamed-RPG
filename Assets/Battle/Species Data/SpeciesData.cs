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

    [Header("Exactly 3 possible Special Abilities")]
    [SerializeField] List<string> specialAbilities;

    [Header("Evolution data")]
    [SerializeField] int evolutionStage = 1;
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

    public int EvolutionStage { get { return evolutionStage; } }
    public List<EvolutionData> Evolutions { get { return evolutions; } }

    public List<string> SpecialAbilities { get { return specialAbilities; } }
}
