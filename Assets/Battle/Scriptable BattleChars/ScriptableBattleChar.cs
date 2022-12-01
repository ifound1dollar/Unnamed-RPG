using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Scriptable BattleChar")]
public class ScriptableBattleChar : ScriptableObject
{
    [Header("Required (at least one ability required)")]
    [SerializeField] SpeciesData speciesData;
    [SerializeField] int level;
    [SerializeField] string ability1;
    [SerializeField] string ability2;
    [SerializeField] string ability3;
    [SerializeField] string ability4;

    [Header("Optional")]
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


    public string[] GetAbilitiesAsArray()
    {
        return new string[4] { ability1, ability2, ability3, ability4 };
    }
}
