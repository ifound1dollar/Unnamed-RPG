using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType { None, Physical, Magic }

public abstract class Ability
{
    public string Name { get; set; }
    public string Description { get; set; }
    public BattleType AbilityType { get; set; }
    public DamageType DamageType { get; set; }
    public bool MakesContact { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public int Energy { get; set; }

    public int Priority { get; set; }
    public bool Delayed { get; set; }
    public bool Recharge { get; set; }

    public int ConsecutiveUses { get; set; }
    public bool Blocked { get; set; }
    public int Score { get; set; }

    public abstract void UseAbility();

    public bool CheckAccuracy(BattleChar user, BattleChar target)
    {
        //always true if accuracy = 0
        if (Accuracy == 0) { return true; }
        
        //if random 0-99 less than Accuracy modified by flat 10% per user AccMod or target DodMod
        if (Random.Range(0, 100) < (Accuracy + (user.AccMod * 10) - (target.DodMod * 10)))
        {
            return true;
        }
        return false;
    }
    public int EnergyCost(BattleChar battleChar)
    {
        return (battleChar.Cursed > 0) ? Energy * 2 : Energy;
    }
    public virtual bool IsUsable(BattleChar battleChar)
    {
        ///Base method returns only whether user has enough Energy to use Ability and that
        /// it is not Blocked. This can be overridden for Abilities with special requirements.

        return (EnergyCost(battleChar) <= battleChar.Energy && !Blocked);
    }

    public virtual void Reset()
    {
        ///Resets all tracking data corresponding to this Ability. Base method only resets
        /// properties shared by all Abilities. Can be overridden to reset any Ability-specific
        /// tracking data.

        ConsecutiveUses = 0;
        Blocked = false;
    }
}
