using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public enum Category { Status, Self, Physical, Magic }

public abstract class Ability
{
    public string Name { get; set; }
    public string Description { get; set; }
    public BattleType AbilityType { get; set; }
    public Category Category { get; set; }
    public bool MakesContact { get; set; }
    public int Energy { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public AbilityAnimation Animation { get; set; }


    public int Priority { get; set; }
    public bool Delayed { get; set; }
    public bool Recharge { get; set; }
    public bool Interrupts { get; set; }
    public bool IsMovement { get; set; }


    public int ConsecutiveUses { get; set; }
    public int TotalUses { get; set; }
    public bool Blocked { get; set; }
    public int Score { get; set; }




    /// <summary>
    /// Performs all operations of Ability use, overridden by each subclass
    /// </summary>
    /// <param name="data">Contains contextual data like user, target, turn number, damage, etc.</param>
    /// <returns></returns>
    public abstract IEnumerator UseAbility(AbilityData data);

    /// <summary>
    /// Calculates Score of this Ability, overridden for custom implementation by each subclass
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    protected abstract void CalcSpecificScore(BattleAI.AIContextObject aiContext);




    /// <summary>
    /// Plays hit animation of this Ability and typical target animations (shaking, damage flash, etc.)
    /// </summary>
    /// <param name="playerUnit">Player BattleUnit</param>
    /// <param name="enemyUnit">Enemy BattleUnit</param>
    /// <param name="isPlayer">Whether this Ability is owned by player or enemy</param>
    /// <returns></returns>
    public IEnumerator PlayHitAnimation(BattleUnit playerUnit, BattleUnit enemyUnit, bool isPlayer)
    {
        if (Animation == null)
        {
            Debug.Log("Animation in Ability is null; returning.");
            yield return new WaitForSeconds(1.0f);
            yield break;
        }

        if (isPlayer)
        {
            yield return Animation.PlayerHitAnimation(playerUnit, enemyUnit);
            yield return new WaitForSeconds(0.5f);  //wait 0.5s before playing damage anim
            enemyUnit.PlayDamagedAnimation();
        }
        else
        {
            yield return Animation.EnemyHitAnimation(enemyUnit, playerUnit);
            yield return new WaitForSeconds(0.5f);
            playerUnit.PlayDamagedAnimation();
        }
        yield return new WaitForSeconds(0.1f);  //wait for DamagedAnimation to be fully dark
    }

    /// <summary>
    /// Plays miss animation of this Ability, not playing typical target animations
    /// </summary>
    /// <param name="playerUnit">Player BattleUnit</param>
    /// <param name="enemyUnit">Enemy BattleUnit</param>
    /// <param name="isPlayer">Whether this Ability is owned by player or enemy</param>
    /// <returns></returns>
    public IEnumerator PlayMissAnimation(BattleUnit playerUnit, BattleUnit enemyUnit, bool isPlayer)
    {
        if (Animation == null)
        {
            Debug.Log("Animation in Ability is null; returning.");
            yield return new WaitForSeconds(1.0f);
            yield break;
        }

        if (isPlayer)
        {
            yield return Animation.PlayerMissAnimation(playerUnit, enemyUnit);
        }
        else
        {
            yield return Animation.EnemyMissAnimation(enemyUnit, playerUnit);
        }
        yield return new WaitForSeconds(0.5f);
    }




    //PROTECTED damage calculators and dialog/HUD update methods
    /// <summary>
    /// Calculates damage to deal with this Ability and assigns to AbilityData object
    /// </summary>
    /// <param name="data">Contains contextual data like user, target, turn number, damage, etc.</param>
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

    /// <summary>
    /// Estimates damage to deal, ignoring 90-110% random multiplier and critical strike chance
    /// </summary>
    /// <param name="user">Ability's user</param>
    /// <param name="target">Ability's target</param>
    /// <returns>Estimated damage as integer</returns>
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
    /// Updates dialog with effectiveness/crit message (if any) then delays for textDelay seconds
    /// </summary>
    /// <param name="data">Contains contextual data like user, target, turn number, damage, etc.</param>
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
        
        if (text != "")
        {
            yield return data.DialogBox.DialogSet(text);
            yield return new WaitForSeconds(data.TextDelay);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }
    }

    /// <summary>
    /// Updates HUD of both user and target, then delays while animations play
    /// </summary>
    /// <param name="data">Contains contextual data like user, target, turn number, damage, etc.</param>
    /// <returns></returns>
    protected IEnumerator UpdateHudAndDelay(AbilityData data)
    {
        ///Updates HUD of player and enemy then delays

        data.PlayerHud.UpdateHUD((data.User.PlayerTeam) ? data.User : data.Target);
        data.EnemyHud.UpdateHUD((data.User.PlayerTeam) ? data.Target : data.User);
        yield return new WaitForSeconds(1.0f);  //1 second is roughly duration of HUD animation
    }




    //universal score calcuation (only called by EnemyAI)
    /// <summary>
    /// Calculates Score of this Ability based on context
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
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
        if (aiContext.Difficulty != BattleAI.AIDifficulty.Easy)
        {
            ConditionalScoreAdjustments(aiContext);
        }

        //FINALLY, ensure Score is non-negative
        Score = Mathf.Max(Score, 0);
    }

    /// <summary>
    /// Does general Score calculation for all damaging Abilities, higher damage = higher Score
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <param name="estimatedDamage">Estimated damage to deal to target (player)</param>
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

    /// <summary>
    /// Adjusts Score of this Ability based on certain conditions
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    void ConditionalScoreAdjustments(BattleAI.AIContextObject aiContext)
    {
        //IF MakesContact and target Thorns(etc.) is active, reduce Score slightly
        if (MakesContact && aiContext.Player.Thorns > 0)
        {
            Score = Mathf.RoundToInt(Score * 0.90f);
        }
    }




    //overridable operational methods
    /// <summary>
    /// Determines whether this Ability will land or miss this turn
    /// </summary>
    /// <param name="user">Ability's user</param>
    /// <param name="target">Ability's target</param>
    /// <returns>Whether attack landed, true if successful</returns>
    public virtual bool CheckAccuracy(BattleChar user, BattleChar target)
    {
        ///Finds whether the attack landed or not, can be overridden for custom behaviors

        //always true if accuracy = 0
        if (Accuracy == 0) { return true; }

        //always misses if target IsFlying or IsUnderground (can be overridden, remember)
        if (target.IsFlying || target.IsUnderground) { return false; }
        
        //if random 0-99 less than Accuracy modified by flat 10% per user AccMod or target DodMod
        if (Random.Range(0, 100) < (Accuracy + (user.AccMod * 10) - (target.DodMod * 10)))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates this Ability's Energy cost in the context of the user this turn
    /// </summary>
    /// <param name="battleChar">Owner of this Ability</param>
    /// <returns>Ability's Energy cost this turn</returns>
    public virtual int EnergyCost(BattleChar battleChar)
    {
        ///Returns Energy cost of this Ability, can be overridden for custom behaviors

        //double Energy cost if user is Cursed
        return (battleChar.StatusActive == StatusEffect.Cursed) ? Energy * 2 : Energy;
    }

    /// <summary>
    /// Determines whether this Ability is usable this turn
    /// </summary>
    /// <param name="battleChar">Owner of this Ability</param>
    /// <returns>Whether Ability is usable, true if it is</returns>
    public virtual bool IsUsable(BattleChar battleChar)
    {
        ///Base method returns only whether user has enough Energy to use Ability and that
        /// it is not Blocked. This can be overridden for Abilities with special requirements.
        ///NOTE: Is overridden to false in EmptyAbility and BadAbility

        return (EnergyCost(battleChar) <= battleChar.Energy && !Blocked);
    }

    /// <summary>
    /// Resets this Ability's tracking data, overridden by Abilities with custom data
    /// </summary>
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
