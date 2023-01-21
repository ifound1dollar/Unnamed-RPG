using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Scriptable BattleChar")]
public class ScriptableBattleChar : ScriptableObject
{
    [Header("- REQUIRED DATA -")]
    [SerializeField] SpeciesData speciesData;
    [SerializeField] int level;

    [Header("Easy Abilities - minimum 1")]
    [SerializeField] string eAbility1;
    [SerializeField] string eAbility2;
    [SerializeField] string eAbility3;
    [SerializeField] string eAbility4;

    [Header("Normal Abilities - minimum 1")]
    [SerializeField] string nAbility1;
    [SerializeField] string nAbility2;
    [SerializeField] string nAbility3;
    [SerializeField] string nAbility4;

    [Header("Hard Abilities - minimum 1")]
    [SerializeField] string hAbility1;
    [SerializeField] string hAbility2;
    [SerializeField] string hAbility3;
    [SerializeField] string hAbility4;


    [Header("- OPTIONAL DATA -")]
    [SerializeField] string nickname;
    [SerializeField] int maxEnergy;
    [Tooltip("Value of -1 implies that it was left unchanged (0 could mean actual 0HP)")]
    [SerializeField] int actualHP = -1;

    [Tooltip("Passive Ability is assigned by species data if undefined")]
    [SerializeField] string passiveAbility; //make PassiveAbility object
    [Tooltip("Impossible Ability cannot normally be learned, but has a chance to replace Ability 4")]
    [SerializeField] string impossibleAbility;

    [Header("Raw stat overrides (float)")]
    [SerializeField] float rawMaxHP;
    [SerializeField] float rawStrength;
    [SerializeField] float rawMastery;
    [SerializeField] float rawArmor;
    [SerializeField] float rawResistance;
    [SerializeField] float rawAgility;

    [Header("Specialty stats (natures), must be different")]
    [SerializeField] SpecialtyStat specialtyUp;
    [SerializeField] SpecialtyStat specialtyDown;


    public SpeciesData SpeciesData  { get { return speciesData; } }
    public int Level                { get { return level; } }
    public string Nickname          { get { return nickname; } }
    public int MaxEnergy            { get { return maxEnergy; } }
    public int ActualHP             { get { return actualHP; } }

    public string PassiveAbility    { get { return passiveAbility; } }  //make PassiveAbility object
    public string ImpossibleAbility { get { return impossibleAbility; } }

    public float RawMaxHP           { get { return rawMaxHP; } }
    public float RawStrength        { get { return rawStrength; } }
    public float RawMastery         { get { return rawMastery; } }
    public float RawArmor           { get { return rawArmor; } }
    public float RawResistance      { get { return rawResistance; } }
    public float RawAgility         { get { return rawAgility; } }

    public SpecialtyStat SpecialtyUp    { get { return specialtyUp; } }
    public SpecialtyStat SpecialtyDown  { get { return specialtyDown; } }


    //HIDDEN FROM EDITOR, ONLY FOR PLAYER CHARACTER STORAGE
    public int XP { get; set; }
    public List<string> PastAbilities { get; set; }


    //constructor for making save character
    public ScriptableBattleChar(BattleChar battleChar)
    {
        speciesData = battleChar.SpeciesData;
        nickname = battleChar.Name;
        level = battleChar.Level;
        //DO NOT DO MAX ENERGY ATM
        actualHP = battleChar.HP;

        eAbility1 = nAbility1 = hAbility1 = battleChar.Abilities[0].ToString();
        eAbility2 = nAbility2 = hAbility2 = battleChar.Abilities[1].ToString();
        eAbility3 = nAbility3 = hAbility3 = battleChar.Abilities[2].ToString();
        eAbility4 = nAbility4 = hAbility4 = battleChar.Abilities[3].ToString();
        passiveAbility = battleChar.SpecialAbility;

        rawMaxHP = battleChar.RawMaxHP;
        rawStrength = battleChar.RawStrength;
        rawMastery = battleChar.RawMastery;
        rawArmor = battleChar.RawArmor;
        rawResistance = battleChar.RawResistance;
        rawAgility = battleChar.RawAgility;

        specialtyUp = battleChar.SpecialtyUp;
        specialtyDown = battleChar.SpecialtyDown;

        XP = battleChar.XP;
        PastAbilities = battleChar.PastAbilities;
    }

    /// <summary>
    /// Returns Abilities as string array of fixed length 4, depending on Difficulty
    /// </summary>
    /// <param name="difficulty">Difficulty determining which Abilities to return</param>
    /// <returns>Array of Ability names as strings</returns>
    public string[] GetAbilitiesAsArray(AIDifficulty difficulty)
    {
        //add to List in order ONLY IF not empty
        List<string> list = new();
        if (difficulty == AIDifficulty.Easy || difficulty == AIDifficulty.Wild)
        {
            //add in order only if not empty
            if (eAbility1 != "") { list.Add(eAbility1); }
            if (eAbility2 != "") { list.Add(eAbility2); }
            if (eAbility3 != "") { list.Add(eAbility3); }
            if (eAbility4 != "") { list.Add(eAbility4); }
        }
        else if (difficulty == AIDifficulty.Normal)
        {
            if (nAbility1 != "") { list.Add(nAbility1); }
            if (nAbility2 != "") { list.Add(nAbility2); }
            if (nAbility3 != "") { list.Add(nAbility3); }
            if (nAbility4 != "") { list.Add(nAbility4); }
        }
        else
        {
            if (hAbility1 != "") { list.Add(hAbility1); }
            if (hAbility2 != "") { list.Add(hAbility2); }
            if (hAbility3 != "") { list.Add(hAbility3); }
            if (hAbility4 != "") { list.Add(hAbility4); }
        }

        //replace empty strings in array with ordered list items, then return array
        string[] strings = new string[4] { "", "", "", "" };
        for (int i = 0; i < list.Count; i++)
        {
            strings[i] = list[i];
        }
        return strings;
    }
}
