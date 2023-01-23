using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraspAbility : Ability
{
    public GraspAbility()
    {
        Name = "Grasp Ability";
        Power = 30;
        Accuracy = 90;
        Energy = 5;
        Description = "A two-turn attack. The user violently grabs hold of the target on the first turn, " +
            "dealing damage and impairing its movement. The user then deals the same damage on the second " +
            "turn before releasing the target. The target is unable to swap out while grasped.";

        AbilityType = BattleType.Vital;
        Category = Category.Physical;
        MakesContact = true;

        Interrupts = true;
        Priority = 5;       //extremely high because should ALWAYS attack first
    }

    public override IEnumerator UseAbility(AbilityData data)
    {
        //deal normal damage first, then operations later
        CalcDamageToDeal(data);
        data.Target.TakeDamage(data.Damage);

        yield return UpdateHudAndDelay(data);
        yield return UpdateDialogUniversal(data);


        //if 0, then this is first turn
        if (data.User.MultiTurnAbility == 0)
        {
            data.User.MultiTurnAbility = 2;

            //only set trapped if it is not already active for longer than 2 turns
            if (data.Target.Trapped < 2)
            {
                data.Target.Trapped = 2;
            }
            yield return data.DialogBox.DialogSet($"{data.User.Name} grasped {data.Target.Name}!");
            yield return new WaitForSeconds(data.TextDelay);
        }
        //else must be 1, meaning second turn
        else
        {
            //only reset if was not already active for longer on first turn
            if (data.Target.Trapped == 1)
            {
                data.Target.Trapped = 0;
            }
            yield return data.DialogBox.DialogSet($"{data.User.Name} released {data.Target.Name} " +
                $"from its grasp.");
            yield return new WaitForSeconds(data.TextDelay);
        }
    }
    protected override void CalcSpecificScore(BattleAI.AIContextObject aiContext)
    {
        ///IN THE FUTURE, ON HARD MODE ONLY, AI SHOULD FLAT OUT CHEAT?
        ///SEE IF PLAYERCHOICE IS SWAP AND ROLL CHANCE TO INTERRUPT? HARD AI BUT NOT GUARANTEED INTERRUPT

        CalcDamagingScore(aiContext, EstimateDamage(aiContext.Enemy, aiContext.Player) * 2);

        //is multi-turn, so decrease by 25% (75%)
        Score = Mathf.RoundToInt(Score * 0.75f);

        //if player has reason to swap (guess), increase Score (bad type matchup, much lower level, etc.)
        int count = 0;
        float ratio = Effectiveness.GetCharMultiplier(aiContext.Enemy, aiContext.Player);
        if (ratio > 1.01f)
        {
            count++;
            if (ratio > 2.01f)
            {
                count++;
            }
        }
        if (aiContext.Enemy.Level - aiContext.Player.Level >= 5)
        {
            count++;
        }

        //increase Score by 10% per count
        Score = Mathf.RoundToInt(Score * (1 + (count * 0.10f)));
    }

    public override bool CheckAccuracy(BattleChar user, BattleChar target)
    {
        //IF 1 (second turn, guaranteed hit); ELSE normal check
        if (user.MultiTurnAbility == 1)
        {
            return true;
        }

        return base.CheckAccuracy(user, target);
    }
}
