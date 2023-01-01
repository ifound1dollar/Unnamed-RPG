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

    [SerializeField] Sprite backSprite;
    [SerializeField] Sprite frontSprite;

    [SerializeField] int hp = 100;
    [SerializeField] int strength = 100;
    [SerializeField] int mastery = 100;
    [SerializeField] int armor = 100;
    [SerializeField] int resistance = 100;
    [SerializeField] int agility = 100;

    [SerializeField] List<LearnedAbility> learnedAbilities;

    public string SpeciesName   { get { return speciesName; } }
    public BattleType Type1     { get { return type1; } }
    public BattleType Type2     { get { return type2; } }
    public float XPRatio        { get { return xpRatio; } }
    public Sprite BackSprite    { get { return backSprite; } }
    public Sprite FrontSprite   { get { return frontSprite; } }
    public int HP               { get { return hp; } }
    public int Strength         { get { return strength; } }
    public int Mastery          { get { return mastery; } }
    public int Armor            { get { return armor; } }
    public int Resistance       { get { return resistance; } }
    public int Agility          { get { return agility; } }
    public List<LearnedAbility> LearnedAbilities { get { return learnedAbilities; } }
}
