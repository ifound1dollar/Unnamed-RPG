using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Category { Status, Self, Physical, Magic }

public abstract class Ability
{
    public string Name { get; set; }
    public string Description { get; set; }
    public BattleType AbilityType { get; set; }
    public Category Category { get; set; }
    public bool MakesContact { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public int Energy { get; set; }

    public int Priority { get; set; }
    public bool Delayed { get; set; }
    public bool Recharge { get; set; }

    public int ConsecutiveUses { get; set; }
    public int TotalUses { get; set; }
    public bool Blocked { get; set; }
    public int Score { get; set; }


    public abstract IEnumerator UseAbility(AbilityData data);
    protected abstract void CalcSpecificScore(BattleAI.AIContextObject aiContext);


    //PROTECTED damage calculators and dialog/HUD update methods
    protected void CalcDamageToDeal(AbilityData data)
    {
        if (data.Target.Protected)
        {
            //Damage remains at 0, Effectiveness at 0, CriticalHit at false
            return;
        }

        float damage;

        //base calculation
        damage = data.User.Level / 1.5f;
        damage += 20;
        damage /= 75;
        damage *= Power;

        //offense/defense stat ratio calculation
        if (Category == Category.Physical)
        {
            damage *= (float)data.User.Strength / data.Target.Armor;
        }
        else if (Category == Category.Magic)
        {
            damage *= (float)data.User.Mastery / data.Target.Resistance;
        }

        //effectiveness multiplier (assigns data.Effectiveness)
        float eff = Effectiveness.GetMultiplier(AbilityType, data.Target.SpeciesData.Type1, data.Target.SpeciesData.Type2);
        data.Effectiveness = eff;
        damage *= eff;

        //random value from 90-110%
        damage *= UnityEngine.Random.Range(0.9f, 1.1f);

        //same type as user modifier
        if (AbilityType == data.User.SpeciesData.Type1 || AbilityType == data.User.SpeciesData.Type2)
        {
            damage *= 1.3f;
        }

        //critical hit check (CrtMod increases chance by flat 10% per stage, plus 10% base chance)
        if (UnityEngine.Random.Range(0, 100) < (10 + (10 * data.User.CrtMod)))
        {
            damage *= 1.75f;
            data.CriticalHit = true;
        }

        //FINALLY, set AbilityData.Damage to calculated damage
        data.Damage = (int)Mathf.Round(damage);
    }
    protected int EstimateDamage(BattleChar user, BattleChar target)
    {
        ///Estimates damage dealt by user to target using this Ability. Does not get random
        /// damage value between 90-110% and does not check for critical hit.

        if (Effectiveness.GetMultiplier(AbilityType, target.SpeciesData.Type1, target.SpeciesData.Type2) == 0.0f
            || target.Protected)
        {
            //return 0 immediately if 0 effectiveness or target is protected
            return 0;
        }

        float damage;

        //base calculation
        damage = user.Level / 1.5f;
        damage += 20;
        damage /= 75;
        damage *= Power;

        //offense/defense stat ratio calculation
        if (Category == Category.Physical)
        {
            damage *= (float)user.Strength / target.Armor;
        }
        else if (Category == Category.Magic)
        {
            damage *= (float)user.Mastery / target.Resistance;
        }

        //effectiveness multiplier (assigns data.Effectiveness)
        damage *= Effectiveness.GetMultiplier(AbilityType, target.SpeciesData.Type1, target.SpeciesData.Type2);

        //same type as user modifier
        if (AbilityType == user.SpeciesData.Type1 || AbilityType == user.SpeciesData.Type2)
        {
            damage *= 1.3f;
        }

        //FINALLY, return estimated damage value
        return Mathf.RoundToInt(damage);
    }
    /// <summary>
    /// Updates dialog with effectiveness and/or crit, else clears dialog
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected IEnumerator UpdateDialogUniversal(AbilityData data)
    {
        ///Ackowledges effectiveness or critical strike if applicable
        ///OTHERWISE, effectively clears text

        string text = "";

        //effectiveness text (approximate because may not be exactly 1.0f)
        if (!Mathf.Approximately(data.Effectiveness, 1.0f))
        {
            if (data.Effectiveness > 1.0f)
            {
                text += "Bonus damage! ";
            }
            else if (data.Effectiveness > 0.0f) //0.0f will always be exact
            {
                text += "Reduced damage! ";
            }
            else
            {
                text += "No damage! ";
            }
        }

        //crit text
        if (data.CriticalHit)
        {
            text += "Critical hit!";
        }
        
        //update dialog even if empty (effectively clears dialog)
        yield return data.DialogBox.DialogSet(text);
        yield return new WaitForSeconds(data.TextDelay);
    }
    protected IEnumerator UpdateHudAndDelay(AbilityData data)
    {
        ///Updates HUD of player and enemy then delays

        data.PlayerHud.UpdateHUD((data.User.PlayerTeam) ? data.User : data.Target);
        data.EnemyHud.UpdateHUD((data.User.PlayerTeam) ? data.Target : data.User);
        yield return new WaitForSeconds(1.0f);  //1 second is roughly duration of HUD animation
    }


    //universal score calcuation (only called by EnemyAI)
    public void CalcScore(BattleAI.AIContextObject aiContext)
    {
        ///Calculates this Ability's Score in the context of this turn and enemy/player

        if (!IsUsable(aiContext.Enemy))
        {
            Score = 0;
            return;
        }

        //Score starts at 1 always
        Score = 1;

        //main Score calculation is handled in CalcSpecificScore
        CalcSpecificScore(aiContext);

        //delayed/recharge Score reduction
        if (Delayed)
        {
            //not 50% because one extra turn to regenerate Energy
            Score = Mathf.RoundToInt(Score * 0.60f);
        }
        if (Recharge)
        {
            //damage is instant with one extra turn to regenerate Energy
            Score = Mathf.RoundToInt(Score * 0.75f);
        }

        //if Score now 0 or 1, then is completely unusable or a last resort
        if (Score <= 1) { return; }

        //accuracy modification
        if (Accuracy > 0)
        {
            //Accuracy proportionally alters Score (ex. 90% Accuracy = 90% Score)
            Score = Mathf.RoundToInt((Accuracy / 100f) * Score);
        }
        else if (Category != Category.Self)
        {
            //if not Self category, is guaranteed hit and should increase Score
            Score = Mathf.RoundToInt(Score * 1.10f);
        }

        //energy cost modification (increase based on efficiency, 10% cost = 90% / 3 = +30% Score)
        float inverse = (1 - ((float)EnergyCost(aiContext.Enemy) / aiContext.Enemy.Energy));
        Score = Mathf.RoundToInt((1 + (inverse / 3)) * Score);

        //if not Easy, do conditional Score adjustments
        if (aiContext.Difficulty != AIDifficulty.Easy)
        {
            ConditionalScoreChecks(aiContext);
        }

        //FINALLY, ensure Score is non-negative
        Score = Mathf.Max(Score, 0);
    }
    protected void CalcDamagingScore(BattleAI.AIContextObject aiContext, int estimatedDamage)
    {
        ///Calculates Score of general damaging Abilities, which is always the same

        if (estimatedDamage == 0)
        {
            Score = 1;
            return;
        }

        //increase based on % current HP dealt by estimated damage (cap at 100%)
        float damagePercent = Mathf.Min((float)estimatedDamage / aiContext.Player.HP, 1.0f);
        Score += (int)(damagePercent * 100f);
        //NOTE: cap at 100% because two Abilities that are both lethal have the same value

        //if expected to be lethal, increase by additional 100
        if (damagePercent >= 1.0f)
        {
            Score += 100;
        }
    }
    void ConditionalScoreChecks(BattleAI.AIContextObject aiContext)
    {
        //IF MakesContact and target Thorns(etc.) is active, reduce Score slightly
        if (MakesContact && aiContext.Player.Thorns > 0)
        {
            Score = Mathf.RoundToInt(Score * 0.90f);
        }
    }


    //overridable operational methods
    public virtual bool CheckAccuracy(BattleChar user, BattleChar target)
    {
        ///Finds whether the attack landed or not, can be overridden for custom behaviors

        //always true if accuracy = 0
        if (Accuracy == 0) { return true; }
        
        //if random 0-99 less than Accuracy modified by flat 10% per user AccMod or target DodMod
        if (Random.Range(0, 100) < (Accuracy + (user.AccMod * 10) - (target.DodMod * 10)))
        {
            return true;
        }
        return false;
    }
    public virtual int EnergyCost(BattleChar battleChar)
    {
        ///Returns Energy cost of this Ability, can be overridden for custom behaviors

        //double Energy cost if user is Cursed
        return (battleChar.StatusActive == StatusEffect.Cursed) ? Energy * 2 : Energy;
    }
    public virtual bool IsUsable(BattleChar battleChar)
    {
        ///Base method returns only whether user has enough Energy to use Ability and that
        /// it is not Blocked. This can be overridden for Abilities with special requirements.
        ///NOTE: Is overridden to false in EmptyAbility and BadAbility

        return (EnergyCost(battleChar) <= battleChar.Energy && !Blocked);
    }
    public virtual void Reset()
    {
        ///Resets all tracking data corresponding to this Ability. Base method only resets
        /// properties shared by all Abilities. Can be overridden to reset any Ability-specific
        /// tracking data.

        ConsecutiveUses = 0;
        TotalUses = 0;
        Blocked = false;
    }
}
