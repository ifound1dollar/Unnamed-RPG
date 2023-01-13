using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Scriptable BattleChar")]
public class ScriptableBattleChar : ScriptableObject
{
    [Header("- REQUIRED DATA -")]
    [SerializeField] SpeciesData speciesData;
    [SerializeField] int level;

    [Header("Easy Abilities - at least one")]
    [SerializeField] string eAbility1;
    [SerializeField] string eAbility2;
    [SerializeField] string eAbility3;
    [SerializeField] string eAbility4;

    [Header("Medium Abilities - at least one")]
    [SerializeField] string mAbility1;
    [SerializeField] string mAbility2;
    [SerializeField] string mAbility3;
    [SerializeField] string mAbility4;

    [Header("Hard Abilities - at least one")]
    [SerializeField] string hAbility1;
    [SerializeField] string hAbility2;
    [SerializeField] string hAbility3;
    [SerializeField] string hAbility4;

    [Header("Impossible Ability - can be empty, chance to replace Ability 4")]
    [SerializeField] string impossibleAbility;


    [Header("- OPTIONAL DATA -")]
    [SerializeField] string nickname;
    [SerializeField] int maxEnergy;
    [Tooltip("Value of -1 implies that it was left unchanged (0 could mean actual 0HP)")]
    [SerializeField] int actualHP = -1;


    [Header("Raw stat overrides")]
    [SerializeField] int rawMaxHP;
    [SerializeField] int rawStrength;
    [SerializeField] int rawMastery;
    [SerializeField] int rawArmor;
    [SerializeField] int rawResistance;
    [SerializeField] int rawAgility;

    [Header("Specialty stats (natures), must be different")]
    [SerializeField] SpecialtyStat specialtyUp;
    [SerializeField] SpecialtyStat specialtyDown;

    //[Header("Base stat overrides")]
    //[SerializeField] int baseMaxHP;
    //[SerializeField] int baseStrength;
    //[SerializeField] int baseMastery;
    //[SerializeField] int baseArmor;
    //[SerializeField] int baseResistance;
    //[SerializeField] int baseAgility;

    public SpeciesData SpeciesData { get { return speciesData; } }
    public int Level { get { return level; } }
    public string ImpossibleAbility { get { return impossibleAbility; } }
    public string Nickname { get { return nickname; } }
    public int MaxEnergy { get { return maxEnergy; } }

    public int ActualHP { get { return actualHP; } }
    public int RawMaxHP { get { return rawMaxHP; } }
    public int RawStrength { get { return rawStrength; } }
    public int RawMastery { get { return rawMastery; } }
    public int RawArmor { get { return rawArmor; } }
    public int RawResistance { get { return rawResistance; } }
    public int RawAgility { get { return rawAgility; } }

    public SpecialtyStat SpecialtyUp { get { return specialtyUp; } }
    public SpecialtyStat SpecialtyDown { get { return specialtyDown; } }

    //public int BaseMaxHP { get { return baseMaxHP; } }
    //public int BaseStrength { get { return baseStrength; } }
    //public int BaseMastery { get { return baseMastery; } }
    //public int BaseArmor { get { return BaseArmor; } }
    //public int BaseResistance { get { return baseResistance; } }
    //public int BaseAgility { get { return baseAgility; } }


    public string[] GetAbilitiesAsArray(AIDifficulty difficulty)
    {
        if (difficulty == AIDifficulty.Easy || difficulty == AIDifficulty.Wild)
        {
            return new string[4] { eAbility1, eAbility2, eAbility3, eAbility4 };
        }
        else if (difficulty == AIDifficulty.Medium)
        {
            return new string[4] { mAbility1, mAbility2, mAbility3, mAbility4 };
        }
        else
        {
            return new string[4] { hAbility1, hAbility2, hAbility3, hAbility4 };
        }
    }
}
