using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum SpecialtyStat { Undefined, HP, Strength, Mastery, Armor, Resistance, Agility }

public class BattleChar
{
    public SpeciesData SpeciesData { get; }

    public Ability[] Abilities { get; set; } = new Ability[4];
    public string Name      { get; set; }
    public bool PlayerTeam  { get; set; }
    public int Level        { get; set; }
    public int Energy       { get; set; }
    public int MaxEnergy    { get; set; }
    public int HP           { get; set; }

    //specialty stats
    public SpecialtyStat SpecialtyUp { get; set; }
    public SpecialtyStat SpecialtyDown { get; set; }

    //stat getters (rounded from float to int)
    public int MaxHP        { get => CalcMaxHP(); }
    public int Strength     { get => CalcStrength(); }
    public int Mastery      { get => CalcMastery(); }
    public int Armor        { get => CalcArmor(); }
    public int Resistance   { get => CalcResistance(); }
    public int Agility      { get => CalcAgility(); }

    //raw stats
    public float RawMaxHP       { get; set; }
    public float RawStrength    { get; set; }
    public float RawMastery     { get; set; }
    public float RawArmor       { get; set; }
    public float RawResistance  { get; set; }
    public float RawAgility     { get; set; }

    //stat modifiers
    public int StrMod   { get; set; }
    public int MasMod   { get; set; }
    public int ArmMod   { get; set; }
    public int ResMod   { get; set; }
    public int AgiMod   { get; set; }
    public int AccMod   { get; set; }
    public int DodMod   { get; set; }
    public int CrtMod   { get; set; }

    //status effects
    public int Frozen   { get; set; }
    public int Burned   { get; set; }
    public int Poisoned { get; set; }
    public int Infected { get; set; }
    public int Cursed   { get; set; }
    public int Stunned  { get; set; }
    public int Berserk  { get; set; }

    //single-turn effects
    public bool Protected       { get; set; }
    public bool ImmuneStatus    { get; set; }
    public string ReflectStatus { get; set; } = "";
    public int ReflectDamage    { get; set; }

    //team effects
    public int HealingMist      { get; set; }
    public int Thorns           { get; set; }

    //field effects

    //traps
    public int ElectricSpikes   { get; set; }

    //tracking and operational
    public Ability UsedAbility  { get; set; }
    public bool Recharging      { get; set; }
    public bool Delaying        { get; set; }
    public int TurnsActive      { get; set; }
    public bool WasActive       { get; set; }


    public BattleChar(ScriptableBattleChar data, AIDifficulty difficulty, bool playerTeam)
    {
        SpeciesData = data.SpeciesData;

        //name/nickname
        if (data.Nickname == "")
        {
            Name = SpeciesData.SpeciesName;
        }
        else
        {
            Name = data.Nickname;
        }

        //max energy / energy (+1 max energy per 2 levels, plus 10 base)
        MaxEnergy = (data.MaxEnergy == 0) ? (int)(Level * 0.5) + 10 : data.MaxEnergy;
        Energy = MaxEnergy;

        //level
        Level = data.Level;

        //abilities
        string[] abilityNames = data.GetAbilitiesAsArray(difficulty);
        for (int i = 0; i < 4; i++)
        {
            if (abilityNames[i] == "")
            {
                Abilities[i] = new EmptyAbility();
                continue;
            }

            //try to create an instance of this ability; if fails, make BadAbility
            try
            {
                Abilities[i] = Activator.CreateInstance(Type.GetType(abilityNames[i])) as Ability;
            }
            catch
            {
                Abilities[i] = new BadAbility();
            }
        }

        //team
        PlayerTeam = playerTeam;

        //raw stats
        RawMaxHP = (data.RawMaxHP == 0) ? CalcRawFromBase(SpeciesData.HP, isHP: true) : data.RawMaxHP;
        RawStrength = (data.RawStrength == 0) ? CalcRawFromBase(SpeciesData.Strength) : data.RawStrength;
        RawMastery = (data.RawMastery == 0) ? CalcRawFromBase(SpeciesData.Mastery) : data.RawMastery;
        RawArmor = (data.RawArmor == 0) ? CalcRawFromBase(SpeciesData.Armor) : data.RawArmor;
        RawResistance = (data.RawResistance == 0) ? CalcRawFromBase(SpeciesData.Resistance) : data.RawResistance;
        RawAgility = (data.RawAgility == 0) ? CalcRawFromBase(SpeciesData.Agility) : data.RawAgility;

        //specialty stats
        if (data.SpecialtyUp == SpecialtyStat.Undefined)
        {
            //get random specialty stat, but don't allow to be the same as SpecialtyDown
            do
            {
                int rand = UnityEngine.Random.Range(1, 7);
                SpecialtyUp = (SpecialtyStat)rand;
            } while (SpecialtyUp == SpecialtyDown);
        }
        if (data.SpecialtyDown == SpecialtyStat.Undefined)
        {
            do
            {
                int rand = UnityEngine.Random.Range(1, 7);
                SpecialtyDown = (SpecialtyStat)rand;
            } while (SpecialtyDown == SpecialtyUp);
        }

        //HP (is only left undefined if -1, can actually be 0)
        HP = (data.ActualHP == -1) ? MaxHP : data.ActualHP; //NOTE: must come after specialty stats
        if (HP > MaxHP) { HP = MaxHP; }
    }

    float CalcRawFromBase(int baseStat, bool isHP = false)
    {
        float baseAsPercent = baseStat * 0.01f;

        if (isHP)
        {
            //5 HP per level plus 20 base
            return baseAsPercent * ((Level * 5) + 20);
        }
        else
        {
            //3 points per level plus 10 base
            return baseAsPercent * ((Level * 3) + 10);
        }
    }

    int CalcMaxHP()
    {
        if (SpecialtyUp == SpecialtyStat.HP)
        {
            return (int)Mathf.Round(RawMaxHP * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.HP)
        {
            return (int)Mathf.Round(RawMaxHP * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawMaxHP);
        }
    }
    int CalcStrength()
    {
        if (SpecialtyUp == SpecialtyStat.Strength)
        {
            return (int)Mathf.Round(RawStrength * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.Strength)
        {
            return (int)Mathf.Round(RawStrength * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawStrength);
        }
    }
    int CalcMastery()
    {
        if (SpecialtyUp == SpecialtyStat.Mastery)
        {
            return (int)Mathf.Round(RawMastery * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.Mastery)
        {
            return (int)Mathf.Round(RawMastery * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawMastery);
        }
    }
    int CalcArmor()
    {
        if (SpecialtyUp == SpecialtyStat.Armor)
        {
            return (int)Mathf.Round(RawArmor * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.Armor)
        {
            return (int)Mathf.Round(RawArmor * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawArmor);
        }
    }
    int CalcResistance()
    {
        if (SpecialtyUp == SpecialtyStat.Resistance)
        {
            return (int)Mathf.Round(RawResistance * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.Resistance)
        {
            return (int)Mathf.Round(RawResistance * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawResistance);
        }
    }
    int CalcAgility()
    {
        if (SpecialtyUp == SpecialtyStat.Agility)
        {
            return (int)Mathf.Round(RawAgility * 1.1f);
        }
        else if (SpecialtyDown == SpecialtyStat.Agility)
        {
            return (int)Mathf.Round(RawAgility * 0.9f);
        }
        else
        {
            return (int)Mathf.Round(RawAgility);
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0 && Cursed > 0)
        {
            //if damage is negative (heal) and Cursed is active, reduce healing by 50%
            damage /= 2;
        }

        HP -= damage;                       //deal damage to actual HP (or heal)

        //if HP is negative, set to 0; if greater than MaxHP, set to MaxHP (int value)
        if (HP < 0)
        {
            HP = 0;
        }
        else if (HP > MaxHP)
        {
            HP = MaxHP;
        }
    }

    public bool HasActiveStatus()
    {
        if (Frozen > 0)         { return true; }
        else if (Burned > 0)    { return true; }
        else if (Poisoned > 0)  { return true; }
        else if (Infected > 0)  { return true; }
        else if (Cursed > 0)    { return true; }
        else if (Stunned > 0)   { return true; }
        else if (Berserk > 0)   { return true; }
        
        return false;
    }
    public bool SetStatusEffect(string effectName, int numTurns)
    {
        if (HP == 0) { return false; }

        if (ReflectStatus != "") { ReflectStatus = effectName; }

        //check for these AFTER ReflectStatus
        if (HasActiveStatus() || ImmuneStatus || Protected) { return false; }

        //handle new status effect
        if (effectName == "Frozen" && Frozen == 0)
        {
            Frozen = numTurns;
            return true;
        }
        else if (effectName == "Burned" && Burned == 0)
        {
            Burned = numTurns;
            return true;
        }
        else if (effectName == "Poisoned" && Poisoned == 0)
        {
            Poisoned = numTurns;
            return true;
        }
        else if (effectName == "Infected" && Infected == 0)
        {
            Infected = numTurns;
            return true;
        }
        else if (effectName == "Cursed" && Cursed == 0)
        {
            Cursed = numTurns;
            return true;
        }
        else if (effectName == "Stunned" && Stunned == 0)
        {
            Stunned = numTurns;
            return true;
        }
        else if (effectName == "Berserk" && Berserk == 0)
        {
            Berserk = numTurns;
            return true;
        }

        //if did not return above, then failed to apply
        return false;
    }
    public int CountModifierTotal()
    {
        ///Counts total magnitude of modifiers and returns it as integer

        return StrMod + MasMod + ArmMod + ResMod + AgiMod + AccMod + DodMod + CrtMod;
    }
    public bool SetModifier(string modifierName, int stages)
    {
        if (HP == 0) return false;

        //handle modifier to set
        if (modifierName == "StrMod")
        {
            StrMod += stages;
            if (StrMod > 4) { StrMod = 4; }
            else if (StrMod < 4) { StrMod = -4; }
            return true;
        }
        else if (modifierName == "MasMod")
        {
            MasMod += stages;
            if (MasMod > 4) { MasMod = 4; }
            else if (MasMod < 4) { MasMod = -4; }
            return true;
        }
        else if (modifierName == "ArmMod")
        {
            ArmMod += stages;
            if (ArmMod > 4) { ArmMod = 4; }
            else if (ArmMod < 4) { ArmMod = -4; }
            return true;
        }
        else if (modifierName == "ResMod")
        {
            ResMod += stages;
            if (ResMod > 4) { ResMod = 4; }
            else if (ResMod < 4) { ResMod = -4; }
            return true;
        }
        else if (modifierName == "AgiMod")
        {
            AgiMod += stages;
            if (AgiMod > 4) { AgiMod = 4; }
            else if (AgiMod < 4) { AgiMod = -4; }
            return true;
        }
        else if (modifierName == "AccMod")
        {
            AccMod += stages;
            if (AccMod > 4) { AccMod = 4; }
            else if (AccMod < 4) { AccMod = -4; }
            return true;
        }
        else if (modifierName == "DodMod")
        {
            DodMod += stages;
            if (DodMod > 4) { DodMod = 4; }
            else if (DodMod < 4) { DodMod = -4; }
            return true;
        }
        else if (modifierName == "CrtMod")
        {
            CrtMod += stages;
            if (CrtMod > 4) { CrtMod = 4; }
            else if (CrtMod < 4) { CrtMod = -4; }
            return true;
        }

        //failed if did not return above
        return false;
    }
    public string CheckEffectEndEarly()
    {
        //if NEGATIVE status effect has 1-2 turns remaining, 25% chance to end early
        if (Frozen > 0 && Frozen <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Frozen = 0;
                return (Name + " is no longer Frozen!");
            }
        }
        else if (Burned > 0 && Burned <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Burned = 0;
                return (Name + " is no longer Burned!");
            }
        }
        else if (Poisoned > 0 && Poisoned <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Poisoned = 0;
                return (Name + " is no longer Poisoned!");
            }
        }
        else if (Infected > 0 && Infected <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Infected = 0;
                return (Name + " is no longer Infected!");
            }
        }
        else if (Cursed > 0 && Cursed <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Cursed = 0;
                return (Name + " is no longer Cursed!");
            }
        }
        else if (Stunned > 0 && Stunned <= 2)
        {
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                Stunned = 0;
                return (Name + " is no longer Stunned!");
            }
        }

        //returning empty string indicates that no effect ended early
        return "";
    }

    //RESETS
    public void ResetModifiers()
    {
        //reset all modifiers when swapping from BattleChar

        StrMod = 0;
        MasMod = 0;
        ArmMod = 0;
        ResMod = 0;
        AgiMod = 0;
        AccMod = 0;
        DodMod = 0;
        CrtMod = 0;
    }
    public void ResetStatusEffects()
    {
        //reset all status effects to 0 without bias

        Frozen = 0;
        Burned = 0;
        Infected = 0;
        Poisoned = 0;
        Cursed = 0;
        Stunned = 0;
        Berserk = 0;
    }
    public void ResetTurnEffects()
    {
        //reset all turn effects

        Protected = false;
        ImmuneStatus = false;
        //ImmuneModifier = false;
        ReflectStatus = null;
        ReflectDamage = 0;
    }
    public void ResetAbilities()
    {
        //reset tracking attributes of each Ability, called when swapping or at 0HP

        foreach (Ability ability in Abilities)
        {
            //method is universal, but is overridden when there are custom attributes
            ability.Reset();
        }
    }
    public void ResetAll()
    {
        //reset all temporary data upon swap or 0HP

        ResetModifiers();
        ResetStatusEffects();
        ResetTurnEffects();
        ResetAbilities();

        Recharging = false;
        Delaying = false;
        TurnsActive = 0;
    }

    //TRANSFERS
    public void TransferTeamEffectsToNew(BattleChar newChar)
    {
        //transfer team effects from old (this) to new (argument)

        newChar.HealingMist = HealingMist;
        newChar.Thorns = Thorns;

        HealingMist = 0;
        Thorns = 0;
    }
    public void TransferFieldEffectsToNew(BattleChar newChar)
    {
        //transfer field effects from old (this) to new (argument)

        //FIRST TRANSFER TO NEW (ARGUMENT)
        //THEN RESET VALUES ON OLD (THIS) TO 0
    }
    public void TransferTrapsToNew(BattleChar newChar)
    {
        //transfer traps from old (this) to new (argument)

        newChar.ElectricSpikes = ElectricSpikes;

        ElectricSpikes = 0;
    }
    public void TransferAllToNew(BattleChar newChar)
    {
        //transfer all transferrable data upon swap or 0HP

        TransferTeamEffectsToNew(newChar);
        TransferFieldEffectsToNew(newChar);
        TransferTrapsToNew(newChar);
    }

    //STATUS EFFECT OPERATIONS
    public string DoBurnedDamage()
    {
        //do Burned damage and return string

        TakeDamage((int)Mathf.Round(MaxHP / 10f));  //10% max HP damage

        return Name + " was damaged by its burn!";
    }
    public string DecrementStatusEffect()
    {
        //do action of and decrement any active status effect, returning dialog string

        if (Frozen > 0)
        {
            Frozen--;
            if (Frozen == 0) { return Name + " is no longer Frozen!"; }
        }
        else if (Burned > 0)
        {
            Burned--;
            if (Burned == 0) { return Name + " is no longer Burned!"; }
        }
        else if (Poisoned > 0)
        {
            Poisoned--;
            if (Poisoned == 0) { return Name + " is no longer Poisoned!"; }
        }
        else if (Infected > 0)
        {
            Infected--;
            if (Infected == 0) { return Name + " is no longer Infected!"; }
        }
        else if (Cursed > 0)
        {
            Cursed--;
            if (Cursed == 0) { return Name + " is no longer Cursed!"; }
        }
        else if (Stunned > 0)
        {
            Stunned--;
            if (Stunned == 0) { return Name + " is no longer Stunned!"; }
        }
        else if (Berserk > 0)
        {
            Berserk--;
            if (Berserk == 0) { return Name + " is no longer Berserk!"; }
        }

        //return empty string if did not return above (nothing ended)
        return "";
    }

    //TEAM EFFECT OPERATIONS
    public List<string> DoTeamEffects()
    {
        //perform action of any team effect (ex. Healing Mist)

        List<string> dialog = new();

        if (HealingMist > 0)
        {
            if (HP < MaxHP)
            {
                TakeDamage(-(int)Mathf.Round(MaxHP / 10f)); //heal 10% max HP
                dialog.Add("Healing Mist restored some of " + Name + "'s HP.");
            }
            //if already at full HP, do nothing and do not acknowledge
        }

        return dialog;  //empty list indicates no effect actions performed
    }
    public List<string> DecrementTeamEffects()
    {
        //decrement any active team effects

        List<string> dialog = new();
        string team = (PlayerTeam) ? "Player" : "Enemy";

        if (HealingMist > 0)
        {
            HealingMist--;
            if (HealingMist == 0)
            {
                dialog.Add(team + " team's Healing Mist ended.");
            }
        }
        if (Thorns > 0)
        {
            Thorns--;
            if (Thorns == 0)
            {
                dialog.Add(team + " team's Thorns ended.");
            }
        }

        return dialog;  //empty list indicates no team effects ended
    }

    //FIELD EFFECT OPERATIONS
    public List<string> DoFieldEffects()
    {
        //perform action of any field effects

        List<string> dialog = new();

        //STUFF HERE

        return dialog;  //empty list indicates no field effect actions performed
    }
    public List<string> DecrementFieldEffects()
    {
        //decrement any active temporary field effects

        List<string> dialog = new();

        //STUFF HERE

        return dialog;  //empty list indicates no field effects ended
    }

    //TRAP OPERATIONS
    public List<string> DoTrapsHit()
    {
        //check for active traps upon swap, doing action and returning dialog strings

        List<string> dialog = new();

        if (ElectricSpikes > 0)
        {
            if (SpeciesData.Type1 != BattleType.Air && SpeciesData.Type2 != BattleType.Air)
            {
                //electric spikes deal Electric-type damage, and don't affect Air type
                int damage = (int)(Mathf.Round(MaxHP / 10f)
                    * Effectiveness.GetMultiplier(BattleType.Electric, SpeciesData.Type1, SpeciesData.Type2));
                TakeDamage(damage);

                dialog.Add(Name + " was damaged by Electric Spikes!");
                ElectricSpikes = 0;
            }
        }

        return dialog;  //if list is empty, no traps were hit
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<string> DecrementTraps()
    {
        //decrement any active traps and return dialog strings with acknowledgements

        List<string> dialog = new();
        string team = (PlayerTeam) ? "player" : "enemy";

        if (ElectricSpikes > 0)
        {
            ElectricSpikes--;
            if (ElectricSpikes == 0)
            {
                dialog.Add("Electric spikes under " + team + " team wore off.");
            }
        }

        return dialog;  //returning empty list indicates that no traps wore off
    }

}
