using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum SpecialtyStat { UNDEFINED, HP, Strength, Mastery, Armor, Resistance, Agility }
public enum StatusEffect { None, Frozen, Burned, Poisoned, Infected, Cursed, Stunned, Berserk }

public class BattleChar
{
    public struct TrackData
    {
        public bool DidSwapIn { get; set; }
        public bool DidApplyStatus { get; set; }
        public bool DidReceiveStatus { get; set; }

        public int DamageDealt { get; set; }
        public int DamageTaken { get; set; }
        public int DamageHealed { get; set; }

        public void Reset()
        {
            DidSwapIn = false;
            DidApplyStatus = false;
            DidReceiveStatus = false;

            DamageDealt = 0;
            DamageTaken = 0;
            DamageHealed = 0;
        }
    }

    public SpeciesData SpeciesData { get; }

    public Ability[] Abilities { get; set; } = new Ability[4];
    public string SpecialAbility { get; set; }
    public string Name      { get; set; }
    public bool PlayerTeam  { get; set; }
    public int Level        { get; set; }
    public int XP           { get; set; }
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
    public StatusEffect StatusActive    { get; set; }
    public int StatusDuration           { get; set; }


    //single-turn effects
    public bool Protected               { get; set; }
    public bool ImmuneStatus            { get; set; }
    public StatusEffect? ReflectStatus  { get; set; }   //null when not ready, None when ready
    public int ReflectDamage            { get; set; }


    //multi-turn effects
    public int Trapped          { get; set; }


    //team effects
    public int HealingMist      { get; set; }
    public int Thorns           { get; set; }


    //field effects


    //traps
    public int ElectricSpikes   { get; set; }


    //tracking and operational
    public Ability UsedAbility  { get; set; }
    public int MultiTurnAbility { get; set; }
    public bool IsRecharging    { get; set; }
    public bool IsDelaying      { get; set; }
    public bool IsFlying        { get; set; }
    public bool IsUnderground   { get; set; }

    public int TurnsActive      { get; set; }
    public bool WasActive       { get; set; }
    public bool WasSlain        { get; set; }
    public bool LeveledUp       { get; set; }
    public int EvoIndex         { get; set; } = -1;
    public List<string> PastAbilities { get; set; } = new();
    public TrackData TurnData { get; set; } = new();


    public BattleChar(ScriptableBattleChar data, BattleAI.AIDifficulty difficulty, bool playerTeam, int argLevel = 0)
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

        //level from data if undefined, else from argument
        Level = (argLevel == 0) ? data.Level : argLevel;

        //abilities, NOW IN ORDER WITH EMPTY AT THE END
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

                //if successful, add name string to PastAbilities
                if (!PastAbilities.Contains(abilityNames[i]))
                {
                    PastAbilities.Add(abilityNames[i]);
                }
            }
            catch
            {
                Abilities[i] = new BadAbility();
            }
        }

        //impossible ability, if defined
        if (data.ImpossibleAbility != "" && UnityEngine.Random.Range(0, 100) == 0)
        {
            //has 1% chance to replace fourth ability, store old ability in case of failure
            Ability origAbility = Abilities[3];
            try
            {
                Abilities[3] = Activator.CreateInstance(Type.GetType(data.ImpossibleAbility)) as Ability;
            }
            catch
            {
                //if replacement fails, return to original fourth ability
                Abilities[3] = origAbility;
            }
        }

        //special ability
        if (data.PassiveAbility == "")
        {
            //if not defined, choose one of three from SpeciesData at random
            SpecialAbility = data.SpeciesData.PassiveAbilities[
                UnityEngine.Random.Range(0, data.SpeciesData.PassiveAbilities.Count)];
        }
        else
        {
            //else defined, so directly assign
            SpecialAbility = data.PassiveAbility;
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
        if (data.SpecialtyUp == SpecialtyStat.UNDEFINED)
        {
            //get random specialty stat, but don't allow to be the same as SpecialtyDown
            do
            {
                int rand = UnityEngine.Random.Range(1, 7);
                SpecialtyUp = (SpecialtyStat)rand;
            } while (SpecialtyUp == SpecialtyDown);
        }
        if (data.SpecialtyDown == SpecialtyStat.UNDEFINED)
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


    //stat calculators
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
        if (Trapped > 0)
        {
            return -2000;   //interpret as if static priority of -2, only goes before when -3/3
        }

        float value = 0.0f;
        if (SpecialtyUp == SpecialtyStat.Agility)
        {
            value = RawAgility * 1.1f;
        }
        else if (SpecialtyDown == SpecialtyStat.Agility)
        {
            value = RawAgility * 0.9f;
        }
        else
        {
            value = RawAgility;
        }

        return Mathf.RoundToInt(value);
    }




    //GENERAL OPERATIONS
    /// <summary>
    /// Deals damage (heals if negative argument), clamping between 0 and MaxHP
    /// </summary>
    /// <param name="damage">Damage to deal (negative to heal)</param>
    public void TakeDamage(int damage)
    {
        if (damage < 0 && StatusActive == StatusEffect.Cursed)
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

    /// <summary>
    /// Determines whether this BattleChar has an active status effect
    /// </summary>
    /// <returns>Whether a status effect is active, true if so</returns>
    public bool HasActiveStatus()
    {
        return (StatusActive != StatusEffect.None);
    }

    /// <summary>
    /// Attempts to apply a status effect to this BattleChar, returning whether successful
    /// </summary>
    /// <param name="effect">Status effect to apply</param>
    /// <param name="duration">Duration of status effect</param>
    /// <returns>Whether status effect was applied, true if successful</returns>
    public bool SetStatusEffect(StatusEffect effect, int duration)
    {
        if (HP == 0 || HasActiveStatus() || ImmuneStatus || Protected) { return false; }

        //check ReflectStatus ONLY IF a real attempt was made (as if rebounded)
        if (ReflectStatus != StatusEffect.None) //will be None only when ready, else null
        {
            ReflectStatus = effect;
            return false;
        }

        StatusActive = effect;
        StatusDuration = duration;
        return true;
    }

    /// <summary>
    /// Calculates total value of modifiers, adding if positive / subtracting if negative
    /// </summary>
    /// <returns>Total value of modifiers</returns>
    public int CountModifierTotal()
    {
        ///Counts total value of modifiers and returns it as integer

        return StrMod + MasMod + ArmMod + ResMod + AgiMod + AccMod + DodMod + CrtMod;
    }

    /// <summary>
    /// Attempts to set a modifier, returning number of stages actually set
    /// </summary>
    /// <param name="modifierName">Name of modifier to apply</param>
    /// <param name="stages">Number of stages to apply</param>
    /// <returns>Number of stages set, less than stages argument means partial/complete failure</returns>
    public int SetModifier(string modifierName, int stages)
    {
        if (HP == 0) return 0;

        //handle modifier to set
        int set = 0;
        if (modifierName == "StrMod")
        {
            StrMod += stages;
            if (StrMod > 4)
            {
                //subtract exceed amount (StrMod - 4) FROM stages argument to find actual set amount
                set = stages - (StrMod - 4);
                StrMod = 4;
            }
            else if (StrMod < -4)
            {
                set = stages - (StrMod + 4);
                StrMod = -4;
            }
        }
        else if (modifierName == "MasMod")
        {
            MasMod += stages;
            if (MasMod > 4)
            {
                set = stages - (MasMod - 4);
                MasMod = 4;
            }
            else if (MasMod < -4)
            {
                set = stages - (MasMod + 4);
                MasMod = -4;
            }
        }
        else if (modifierName == "ArmMod")
        {
            ArmMod += stages;
            if (ArmMod > 4)
            {
                set = stages - (ArmMod - 4);
                ArmMod = 4;
            }
            else if (ArmMod < -4)
            {
                set = stages - (ArmMod + 4);
                ArmMod = -4;
            }
        }
        else if (modifierName == "ResMod")
        {
            ResMod += stages;
            if (ResMod > 4)
            {
                set = stages - (ResMod - 4);
                ResMod = 4;
            }
            else if (ResMod < -4)
            {
                set = stages - (ResMod + 4);
                ResMod = -4;
            }
        }
        else if (modifierName == "AgiMod")
        {
            AgiMod += stages;
            if (AgiMod > 4)
            {
                set = stages - (AgiMod - 4);
                AgiMod = 4;
            }
            else if (AgiMod < -4)
            {
                set = stages - (AgiMod + 4);
                AgiMod = -4;
            }
        }
        else if (modifierName == "AccMod")
        {
            AccMod += stages;
            if (AccMod > 4)
            {
                set = stages - (AccMod - 4);
                AccMod = 4;
            }
            else if (AccMod < -4)
            {
                set = stages - (AccMod + 4);
                AccMod = -4;
            }
        }
        else if (modifierName == "DodMod")
        {
            DodMod += stages;
            if (DodMod > 4)
            {
                set = stages - (DodMod - 4);
                DodMod = 4;
            }
            else if (DodMod < -4)
            {
                set = stages - (DodMod + 4);
                DodMod = -4;
            }
        }
        else if (modifierName == "CrtMod")
        {
            CrtMod += stages;
            if (CrtMod > 4)
            {
                set = stages - (CrtMod - 4);
                CrtMod = 4;
            }
            else if (CrtMod < -4)
            {
                set = stages - (CrtMod + 4);
                CrtMod = -4;
            }
        }
        
        return set;
    }

    /// <summary>
    /// Checks whether an active status effect with 1 or 2 turns remaining will end early
    /// </summary>
    /// <returns>Status effect end acknowledgement, empty if no effect ended</returns>
    public string CheckEffectEndEarly()
    {
        ///If NEGATIVE status effect has 1 or 2 turns remaining, 25% chance to end early

        string text = "";
        if (StatusDuration == 1 || StatusDuration == 2)
        {
            //return (fail) if not between 0-24, 25% chance to be successful
            if (UnityEngine.Random.Range(0, 100) >= 25)
            {
                //returning empty string means no effect ended early
                return "";
            }

            switch (StatusActive)
            {
                case StatusEffect.Frozen:
                    {
                        text = (Name + " is no longer Frozen!");
                        break;
                    }
                case StatusEffect.Burned:
                    {
                        text = (Name + " is no longer Burned!");
                        break;
                    }
                case StatusEffect.Poisoned:
                    {
                        text = (Name + " is no longer Poisoned!");
                        break;
                    }
                case StatusEffect.Infected:
                    {
                        text = (Name + " is no longer Infected!");
                        break;
                    }
                case StatusEffect.Cursed:
                    {
                        text = (Name + " is no longer Cursed!");
                        break;
                    }
                case StatusEffect.Stunned:
                    {
                        text = (Name + " is no longer Stunned!");
                        break;
                    }
                default:    //equivalent to StatusEffect.None
                    {
                        text = "WARNING: Attempted to end None.";
                        break;
                    }
            }
        }

        //reset duration to 0 and effect enum to None, then return text
        StatusDuration = 0;
        StatusActive = StatusEffect.None;
        return text;
    }

    /// <summary>
    /// Determines whether this BattleChar can swap out of battle
    /// </summary>
    /// <returns>Whether this BattleChar can swap, true if so</returns>
    public bool CheckCanSwap()
    {
        if (Trapped > 0)
        {
            return false;
        }
        return true;
    }




    //RESETS
    /// <summary>
    /// Resets all modifiers to 0
    /// </summary>
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

    /// <summary>
    /// Resets status effect var to None and duration to 0
    /// </summary>
    public void ResetStatusEffects()
    {
        //reset any active status effect to duration 0

        StatusDuration = 0;
        StatusActive = StatusEffect.None;
    }

    /// <summary>
    /// Resets all single-turn operational effects
    /// </summary>
    public void ResetSingleTurnEffects()
    {
        Protected = false;
        ImmuneStatus = false;
        //ImmuneModifier = false;
        ReflectStatus = null;
        ReflectDamage = 0;        
    }

    /// <summary>
    /// Resets all multi-turn operational effects
    /// </summary>
    public void ResetMultiTurnEffects()
    {
        Trapped = 0;
    }

    /// <summary>
    /// Resets all temporary operational and tracking data of this BattleChar's Abilities
    /// </summary>
    public void ResetAbilities()
    {
        //reset tracking attributes of each Ability, called when swapping or at 0HP

        foreach (Ability ability in Abilities)
        {
            //method is universal, but is overridden when there are custom attributes
            ability.Reset();
        }
    }

    /// <summary>
    /// Resets all temporary operational and tracking data stored by this BattleChar
    /// </summary>
    public void ResetAll()
    {
        ResetModifiers();
        ResetStatusEffects();
        ResetSingleTurnEffects();
        ResetMultiTurnEffects();
        ResetAbilities();
        TurnData.Reset();

        UsedAbility = null;
        IsRecharging = false;
        IsDelaying = false;
        IsFlying = false;
        IsUnderground = false;
        TurnsActive = 0;
        MultiTurnAbility = 0;
    }

    /// <summary>
    /// Resets all temporary operational and tracking data AND all battle-relevant data
    /// </summary>
    public void ResetEndBattle()
    {
        ResetAll();
        WasActive = false;
        WasSlain = false;

        //team effects
        HealingMist = 0;
        Thorns = 0;

        //field effects
        //ANY RESETS HERE

        //traps
        ElectricSpikes = 0;
    }




    //MULTI TURN EFFECT OPERATIONS
    /// <summary>
    /// Decrements duration of all multi-turn operational effects
    /// </summary>
    public void DecrementMultiTurnEffects()
    {
        if (Trapped > 0)
        {
            Trapped--;
        }
    }




    //TRANSFERS
    /// <summary>
    /// Transfers all currently active team effects to BattleChar swapping in
    /// </summary>
    /// <param name="newChar">BattleChar swapping in</param>
    public void TransferTeamEffectsToNew(BattleChar newChar)
    {
        //transfer team effects from old (this) to new (argument)

        newChar.HealingMist = HealingMist;
        newChar.Thorns = Thorns;

        HealingMist = 0;
        Thorns = 0;
    }

    /// <summary>
    /// Transfers all currently active field effects to BattleChar swapping in
    /// </summary>
    /// <param name="newChar">BattleChar swapping in</param>
    public void TransferFieldEffectsToNew(BattleChar newChar)
    {
        //transfer field effects from old (this) to new (argument)

        //FIRST TRANSFER TO NEW (ARGUMENT)
        //THEN RESET VALUES ON OLD (THIS) TO 0
    }

    /// <summary>
    /// Transfers all currently active traps to BattleChar swapping in
    /// </summary>
    /// <param name="newChar">BattleChar swapping in</param>
    public void TransferTrapsToNew(BattleChar newChar)
    {
        //transfer traps from old (this) to new (argument)

        newChar.ElectricSpikes = ElectricSpikes;

        ElectricSpikes = 0;
    }

    /// <summary>
    /// Transfers all team effects, field effects, and traps to BattleChar swapping in
    /// </summary>
    /// <param name="newChar">BattleChar swapping in</param>
    public void TransferAllToNew(BattleChar newChar)
    {
        //transfer all transferrable data upon swap or 0HP

        TransferTeamEffectsToNew(newChar);
        TransferFieldEffectsToNew(newChar);
        TransferTrapsToNew(newChar);
    }




    //STATUS EFFECT OPERATIONS
    /// <summary>
    /// Does Burned operation to this BattleChar, dealing damage and acknowledging
    /// </summary>
    /// <returns>Damage acknowledgement string</returns>
    public string DoBurnedDamage()
    {
        //do Burned damage and return string

        TakeDamage((int)Mathf.Round(MaxHP / 10f));  //10% max HP damage

        return Name + " was damaged by its burn!";
    }

    /// <summary>
    /// Decrements duration of active status effect, acknowledging if effect ended
    /// </summary>
    /// <returns>Effect end acknowledgement string, if any</returns>
    public string DecrementStatusEffect()
    {
        ///Do action of and decrement any active status effect, returning dialog string

        if (StatusDuration <= 0)
        {
            //empty string indicates that no status effect ended
            return "";
        }

        string text = "";
        if (StatusDuration == 1)    //1 means this was last turn, will decrement below
        {
            switch (StatusActive)
            {
                case StatusEffect.Frozen:
                    {
                        text = (Name + " is no longer Frozen!");
                        break;
                    }
                case StatusEffect.Burned:
                    {
                        text = (Name + " is no longer Burned!");
                        break;
                    }
                case StatusEffect.Poisoned:
                    {
                        text = (Name + " is no longer Poisoned!");
                        break;
                    }
                case StatusEffect.Infected:
                    {
                        text = (Name + " is no longer Infected!");
                        break;
                    }
                case StatusEffect.Cursed:
                    {
                        text = (Name + " is no longer Cursed!");
                        break;
                    }
                case StatusEffect.Stunned:
                    {
                        text = (Name + " is no longer Stunned!");
                        break;
                    }
                default:    //equivalent to StatusEffect.None
                    {
                        text = "WARNING: Attempted to end None.";
                        break;
                    }
            }
        }

        //reset duration to 0, active enum to None, then return text
        StatusDuration = 0;
        StatusActive = StatusEffect.None;
        return text;
    }




    //TEAM EFFECT OPERATIONS
    /// <summary>
    /// Does operations of any active team effects, acknowledging actions
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
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

    /// <summary>
    /// Decrements durations of all team effects, acknowledging if effect ended
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
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
    /// <summary>
    /// Does operations of any active field effects, acknowledging actions
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
    public List<string> DoFieldEffects()
    {
        //perform action of any field effects

        List<string> dialog = new();

        //STUFF HERE

        return dialog;  //empty list indicates no field effect actions performed
    }

    /// <summary>
    /// Decrements durations of all field effects, acknowleding if effect ended
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
    public List<string> DecrementFieldEffects()
    {
        //decrement any active temporary field effects

        List<string> dialog = new();

        //STUFF HERE

        return dialog;  //empty list indicates no field effects ended
    }




    //TRAP OPERATIONS
    /// <summary>
    /// Does operations of any traps that this BattleChar hit, acknowledging actions
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
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
    /// Decrements durations of all traps underneath this Team, acknowledging if trap ended
    /// </summary>
    /// <returns>List of acknowledgement strings to print to dialog</returns>
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




    //XP AND EVOLUTION
    /// <summary>
    /// Adds XP to this BattleChar, ensuring it does not exceed maximum possible
    /// </summary>
    /// <param name="xp">XP to add</param>
    public void AddXP(int xp)
    {
        //add XP, then ensure is not greater than maximum at this xp ratio (1,000,000 base)
        XP += xp;
        XP = Mathf.RoundToInt(Mathf.Min(XP, 1000000 * SpeciesData.XPRatio));
    }

    /// <summary>
    /// Handles leveling up, increasing stats and checking if should learn an Ability
    /// </summary>
    /// <returns>Name of Ability to learn from level up, empty if none</returns>
    public string LevelUp()
    {
        ///Returns name of Ability if one is trying to be learned at new level

        //store all old CALCULATED stat values so stats can be increased by difference
        float oldRawMaxHP = CalcRawFromBase(SpeciesData.HP, isHP: true);
        float oldRawStrength = CalcRawFromBase(SpeciesData.Strength);
        float oldRawMastery = CalcRawFromBase(SpeciesData.Mastery);
        float oldRawArmor = CalcRawFromBase(SpeciesData.Armor);
        float oldRawResistance = CalcRawFromBase(SpeciesData.Resistance);
        float oldRawAgility = CalcRawFromBase(SpeciesData.Agility);
        int oldMaxHP = MaxHP;   //also store old HP so it can be increased with level

        Level++;

        //recalculate stats at new level, then add difference to actual raw stats
        RawMaxHP += (CalcRawFromBase(SpeciesData.HP, isHP: true) - oldRawMaxHP);
        RawStrength += (CalcRawFromBase(SpeciesData.Strength) - oldRawStrength);
        RawMastery += (CalcRawFromBase(SpeciesData.Mastery) - oldRawMastery);
        RawArmor += (CalcRawFromBase(SpeciesData.Armor) - oldRawArmor);
        RawResistance += (CalcRawFromBase(SpeciesData.Resistance) - oldRawResistance);
        RawAgility += (CalcRawFromBase(SpeciesData.Agility) - oldRawAgility);

        //if not 0HP, increase HP by difference from old to new MaxHP
        HP += (HP > 0) ? (MaxHP - oldMaxHP) : 0;

        //if found LearnedAbility matching this level, return its name
        foreach (LearnedAbility lAbility in SpeciesData.LearnedAbilities)
        {
            if (lAbility.Level == Level)
            {
                return lAbility.Name;
            }
        }

        //if no learned ability, return empty string
        return "";
    }

    public void CheckEvolutionConditions(BattleChar[] enemyChars)
    {
        foreach (EvolutionData evoData in SpeciesData.Evolutions)
        {
            //if Level only requirement
            if (LeveledUp && evoData.EventType == EvolutionData.EvoEvent.LevelUp && evoData.Level <= Level)
            {
                EvoIndex = SpeciesData.Evolutions.IndexOf(evoData);
                return;     //will only be one that meets requirement per battle, but return anyway
            }
            //if slays specified target and within valid level range
            else if (evoData.EventType == EvolutionData.EvoEvent.SlayTarget && evoData.Level <= Level)
            {
                foreach (BattleChar battleChar in enemyChars)
                {
                    if (battleChar.HP == 0 && battleChar.SpeciesData.SpeciesName == evoData.SlayTargetName)
                    {
                        //if correct species slain, set EvoIndex and return
                        EvoIndex = SpeciesData.Evolutions.IndexOf(evoData);
                        return;
                    }
                }
            }
            //if was slain and within valid level range
            else if (WasSlain && evoData.EventType == EvolutionData.EvoEvent.IsSlain && evoData.Level <= Level)
            {
                EvoIndex = SpeciesData.Evolutions.IndexOf(evoData);
                return;
            }
        }

    }




    //CONVERSION
    public ScriptableBattleChar ConvertToScriptable()
    {
        return new(this);
    }
}
