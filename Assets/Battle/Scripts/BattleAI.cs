using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum AIDifficulty { Wild, Easy, Medium, Hard, Boss }
public class BattleAI
{
    AIDifficulty Difficulty { get; set; }
    BattleSystem BattleSystemRef { get; set; }

    public BattleAI(BattleSystem battleSystem, AIDifficulty difficulty)
    {
        BattleSystemRef = battleSystem;
        Difficulty = difficulty;
    }


    public Ability ChooseAbility(BattleChar currEnemy, BattleChar currPlayer)
    {
        ///Calculate Ability scores and dynamically choose an Ability to use

        //if Wild, pick random valid Ability (only passing if none valid)
        if (Difficulty == AIDifficulty.Wild)
        {
            return GetWildAbilityChoice(currEnemy);
        }

        //calculate Ability scores and assign to Ability objects
        CalcAbilityScores(currEnemy, currPlayer);

        //create Ability pool
        List<Ability> abilityPool = CreateAbilityPool(currEnemy, currPlayer, currEnemy.Abilities);

        //if Ability pool is not empty, choose Ability; else return null (will Pass)
        if (abilityPool.Count > 0)
        {
            return SelectAbilityFromPool(abilityPool);
        }
        else
        {
            return null;
        }
    }

    void CalcAbilityScores(BattleChar currEnemy, BattleChar currPlayer)
    {
        ///Calculate Score of each of enemy's Abilities

        //calculate Score for each ability
        foreach (Ability ability in currEnemy.Abilities)
        {
            ability.CalcScore(currEnemy, currPlayer);
        }

        //if Easy, modify by 50%; if Medium, modify by 20%
        if (Difficulty == AIDifficulty.Easy)
        {
            MakePreferDamaging(currEnemy.Abilities);
        }
        else if (Difficulty == AIDifficulty.Medium)
        {
            MakePreferDamaging(currEnemy.Abilities, modifier: 1.2f);
        }

        //if only one enemy remains, prefer damaging
        if (BattleSystemRef.GetRemaining(playerTeam: false) == 1)
        {
            MakePreferDamaging(currEnemy.Abilities);
        }

        //if HP of player or enemy is below 50%, prefer damaging (integer division)
        if (currEnemy.HP <= currEnemy.MaxHP / 2 || currPlayer.HP <= currPlayer.MaxHP / 2)
        {
            MakePreferDamaging(currEnemy.Abilities);
        }

        //if enemy is 5+ levels higher than player, prefer damaging
        if (currEnemy.Level - currPlayer.Level >= 5)
        {
            MakePreferDamaging(currEnemy.Abilities);
        }
    }
    List<Ability> CreateAbilityPool(BattleChar currEnemy, BattleChar currPlayer, Ability[] abilities)
    {
        ///Places every Ability within 75% of the highest Score in a pool

        //find maxScore and cutoff score, then create new List
        int maxScore = abilities.Max(x => x.Score);
        int cutoff = (int)Mathf.Ceil(maxScore * 0.75f);
        List<Ability> abilityPool = new();

        //check if passing is preferable/forced this turn, returning empty if so
        if (CheckShouldPass(currEnemy, currPlayer, maxScore))
        {
            return abilityPool;
        }

        //add all Abilities within cutoff range, and add top scoring Ability(s) twice
        foreach (Ability ability in abilities)
        {
            if (ability.Score >= cutoff)
            {
                abilityPool.Add(ability);
            }
            if (ability.Score == maxScore)
            {
                abilityPool.Add(ability);
            }
        }

        //if only 2 items, then only top scoring Ability, so add second highest
        if (abilityPool.Count == 2)
        {
            AddSecondHighestToPool(abilities, maxScore, abilityPool);
        }

        //FINALLY, return ability pool
        return abilityPool;
    }
    bool CheckShouldPass(BattleChar currEnemy, BattleChar currPlayer, int maxScore)
    {
        ///Determine whether passing this turn is preferable or forced

        //if maxScore is 0, none are usable, so must pass
        if (maxScore == 0)
        {
            return true;
        }

        //if HP less than 25% or at 100% Energy, do not pass
        if (currEnemy.HP <= currEnemy.MaxHP / 4.0f || currEnemy.Energy == currEnemy.MaxEnergy)
        {
            return false;
        }

        ///IF LESS THAN 25% ENERGY REMAINING or GREATER THAN 66% ABILITIES ARE UNUSABLE,
        /// SHOULD PASS
        ///SHOULD MAKE BEST ESTIMATE IF THERE IS A LETHAL ABILITY (>50 MAXSCORE? OR JUST
        /// DO POOL, MAKES PASS BASICALLY GUARANTEED IF ONLY AVAILABLE ABILITY HAS SCORE 1)
        /// 
        ///IMPORTANT: IF MAXSCORE IS VERY HIGH, THEN PASSING IS NATURALLY NOT PREFERABLE
        /// (HIGH MAXSCORE IMPLIES BALLS-TO-THE-WALL WITH ATTACKING)

        //find number of Abilities without enough Energy to use AND total valid Abilities
        int numUnusable = 0;
        int totalValid = 0;
        foreach (Ability ability in currEnemy.Abilities)
        {
            if (ability.EnergyCost(currEnemy) > currEnemy.Energy)
            {
                numUnusable++;
            }
            if (ability.Name != "EMPTY" && ability.Name != "MISSING")
            {
                totalValid++;
            }
        }

        //allow pass roll only if less than 25% Energy OR total unusable is at least 50% total
        if (currEnemy.Energy <= currEnemy.MaxEnergy / 4.0f || (float)numUnusable / totalValid >= 0.49f)
        {
            //if maxScore is greater than 200, should not pass
            if (maxScore > 200)
            {
                return false;
            }

            //if unusable is greater than 66%, should have slightly higher chance to pass
            int passScore = ((float)numUnusable / totalValid >= 0.66f) ? 75 : 50;

            //roll weighted chance to pass
            if (UnityEngine.Random.Range(0, passScore + maxScore) < passScore)
            {
                return true;
            }
        }

        return false;
    }
    Ability SelectAbilityFromPool(List<Ability> abilityPool)
    {
        ///Selects an Ability randomly from the provided Ability pool

        //calculate total score and select a random value in its range
        int choice = UnityEngine.Random.Range(0, abilityPool.Sum(x => x.Score));

        ///EXPLANATION
        ///Iterate through abilityPool, subtracting the current Ability's score from 'choice'
        /// until it goes negative.
        ///When it goes negative, break from the loop and return the Ability at this index.
        ///
        ///The total score can be thought of as a line segment, starting at 0 and
        /// ending at total score. The line segment is made up of the Ability Scores in
        /// abilityPool, added onto each other (ex. |100|125|125|, where 100 is the second
        /// option and both of the 125s are the first option).
        ///If 'choice' selects 230, then the item being pointed to by that integer would
        /// be the second 125 (third block). Subtracting the value of the first item in the
        /// line segment (in this example, 100) would leave 130 to go; the same operation
        /// would leave 5 to go.
        ///When the third subtraction occurs, 'choice' goes negative. This indicates that
        /// the value to which the variable pointed has just been passed, and thus THIS
        /// value is the index of the correct Move to use.
        foreach (Ability ability in abilityPool)
        {
            choice -= ability.Score;
            if (choice < 0)
            {
                return ability;
            }
        }

        //this will never be reached, but is required for compilation
        return null;
    }

    void MakePreferDamaging(Ability[] abilities, float modifier = 1.5f)
    {
        ///Adjust Score of every Ability up or down by modifier value

        foreach (Ability ability in abilities)
        {
            //if damaging, increase score by modifier, else decrease by inverse
            if (ability.Category == Category.Physical || ability.Category == Category.Magic)
            {
                ability.Score = (int)Mathf.Round(ability.Score * modifier);
            }
            else
            {
                ability.Score = (int)Mathf.Round(ability.Score / modifier);
            }
        }
    }
    void AddSecondHighestToPool(Ability[] abilities, int maxScore, List<Ability> abilityPool)
    {
        ///Add second highest scoring Ability that is greater than 1 and add to pool

        //find second highest score
        int secondHighest = 0;
        foreach (Ability ability in abilities)
        {
            if (ability.Score < maxScore && ability.Score > secondHighest)
            {
                secondHighest = ability.Score;
            }
        }

        //don't add unless second highest is greater than 1
        if (secondHighest > 1)
        {
            foreach (Ability ability in abilities)
            {
                if (ability.Score == secondHighest)
                {
                    abilityPool.Add(ability);
                }
            }
        }
    }

    Ability GetWildAbilityChoice(BattleChar currEnemy)
    {
        ///Make completely random, unbiased choice of Ability for Wild enemies

        //add all valid Abilities to abilityPool ONCE
        List<Ability> abilityPool = new();
        foreach (Ability ability in currEnemy.Abilities)
        {
            if (ability.IsUsable(currEnemy))
            {
                abilityPool.Add(ability);
            }
        }

        //if none are usable, return null, else choose random and return it
        if (abilityPool.Count == 0)
        {
            return null;
        }
        else
        {
            return currEnemy.Abilities[UnityEngine.Random.Range(0, abilityPool.Count)];
        }
    }
}
