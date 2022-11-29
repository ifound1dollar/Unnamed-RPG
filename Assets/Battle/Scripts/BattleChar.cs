using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleChar
{
    public SpeciesData SpeciesData { get; }

    public Ability[] Abilities { get; set; } = new Ability[4];
    public string Name      { get; set; }
    public int Level        { get; set; }
    public int Energy       { get; set; }
    public int MaxEnergy    { get; set; }

    //stat getters (rounded from float to int)
    public int HP           { get => CalcHP(); }
    public int MaxHP        { get => CalcMaxHP(); }
    public int Strength     { get => CalcStrength(); }
    public int Mastery      { get => CalcMastery(); }
    public int Armor        { get => CalcArmor(); }
    public int Resistance   { get => CalcResistance(); }
    public int Agility      { get => CalcAgility(); }

    //status effects
    public int Cursed       { get; set; }

    //stat modifiers
    public int AccMod { get; set; }
    public int DodMod { get; set; }

    public BattleChar(SpeciesData speciesData)
    {
        ///WILL TAKE OPTIONAL SAVE CHARACTER FORMAT FOR PLAYER CHARACTERS IN THE FUTURE
        
        SpeciesData = speciesData;

        //TEMP
        Name = "TEMP NAME";
        Level = 1;
        Energy = 10;
        MaxEnergy = 10;

        Abilities[0] = new BasicMove4();
        Abilities[1] = new EmptyAbility();
        Abilities[2] = new BadAbility();
        Abilities[3] = new EmptyAbility();
        //TEMP
    }

    int CalcHP()
    {
        return 10;
    }
    int CalcMaxHP()
    {
        return 10;
    }
    int CalcStrength()
    {
        return 10;
    }
    int CalcMastery()
    {
        return 10;
    }
    int CalcArmor()
    {
        return 10;
    }
    int CalcResistance()
    {
        return 10;
    }
    int CalcAgility()
    {
        return 10;
    }
}
