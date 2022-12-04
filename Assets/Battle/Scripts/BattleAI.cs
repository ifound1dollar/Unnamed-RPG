using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum AIDifficulty { Easy, Medium, Hard, Boss, Wild }
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
    public bool CheckShouldSwap(BattleChar currEnemy, BattleChar currPlayer, int maxScore)
    {
        ///Does calculation to determine if swapping is more preferable than attacking this
        /// turn. Calculates preferability, and if it passes a threshold, rolls a chance
        /// proportional to the preferability to swap instead of attack this turn.
        ///The greater the number of non-preferable interactions (bad type interaction,
        /// active negative status, negative modifiers (net), low energy), the more likely
        /// the enemy is to swap. There is a strict threshold that must be passed, but
        /// once it is passed, the chance is directly proportional to the number of non-
        /// preferable interactions.
        ///Preferability is hierarchical, most important to least important:
        /// 1) Type interaction, 2) status effects & modifiers, 3) remaining Energy

        //if only one remaining, only active for 1-2 turns, or Easy difficulty, do not swap
        if (BattleSystemRef.GetRemaining(playerTeam: false) <= 1 || currEnemy.TurnsActive <= 2
            || Difficulty == AIDifficulty.Easy)
        {
            //MUST ALSO CHECK FOR ATTRIBUTE THAT PREVENTS SWAPPING OUT OF BATTLE
            return false;
        }


        //get effectiveness of player vs enemy
        float mult1 = Effectiveness.GetMultiplier(currPlayer.SpeciesData.Type1, currEnemy.SpeciesData.Type1, currEnemy.SpeciesData.Type2);
        float mult2 = Effectiveness.GetMultiplier(currPlayer.SpeciesData.Type2, currEnemy.SpeciesData.Type1, currEnemy.SpeciesData.Type2);
        float typeMultiplier = mult1 * mult2;

        int score = 0;

        //+2 points if active status that is NOT Berserk
        if (currEnemy.HasActiveStatus() && currEnemy.Berserk == 0)
        {
            score += 2;
        }

        //+-1 point per modifier (total magnitude, change by inverse)
        score -= currEnemy.CountModifierTotal();

        //+1 point if less than 50% Energy remaining, +2 if less than 25%
        score += (currEnemy.MaxEnergy / currEnemy.Energy) / 2;

        //multiplier calculation
        score = (int)Mathf.Round(score * typeMultiplier);


        //if maxScore >= 200 (likely lethal hit) OR score < 5 (not enough reason to swap)
        if (maxScore >= 200 || score < 5)
        {
            return false;
        }

        //increase scope to be comparable with maxScore (ex. score now 30, maxScore 70)
        score *= 5;

        //Medium difficulty has only half chance to swap, while Hard and Boss have normal
        if (Difficulty == AIDifficulty.Medium)
        {
            if (UnityEngine.Random.Range(0, score + maxScore) < score / 2)
            {
                return true;
            }
        }
        else
        {
            if (UnityEngine.Random.Range(0, score + maxScore) < score)
            {
                return true;
            }
        }

        //if should not swap, return false
        return false;
    }
    public int ChooseSwapChar(BattleChar[] enemyChars, BattleChar currPlayer)
    {
        ///Finds the most preferable character to swap to and chooses it, weighted
        /// by difficulty.

        //calculate best option and add to pool (Easy is equal score for each)
        List<int> scoresPool = new();
        for (int i = 0; i < enemyChars.Length; i++)
        {
            //if currEnemy or at 0HP, add 0 to pool and continue
            if (BattleSystemRef.GetCurrBattleCharIndex(playerTeam: false) == i || enemyChars[i].HP == 0)
            {
                scoresPool.Add(0);
                continue;
            }

            //get effectiveness of player vs enemy
            float mult1 = Effectiveness.GetMultiplier(currPlayer.SpeciesData.Type1, enemyChars[i].SpeciesData.Type1, enemyChars[i].SpeciesData.Type2);
            float mult2 = Effectiveness.GetMultiplier(currPlayer.SpeciesData.Type2, enemyChars[i].SpeciesData.Type1, enemyChars[i].SpeciesData.Type2);
            float typeMultiplier = mult1 * mult2;

            //make prelim score and modify slightly based on HP and Level difference
            int score = 10;
            if ((float)enemyChars[i].HP / enemyChars[i].MaxHP < 0.5f)
            {
                score -= 3;
            }
            if (enemyChars[i].Level - currPlayer.Level >= 3)
            {
                score += 3;
            }

            //Easy: 10 for all | Medium: multiplier*2 | Hard: multiplier^2 | Boss: multiplier^3
            if (Difficulty == AIDifficulty.Easy || currPlayer.HP == 0)
            {
                //IF CURRPLAYER HP IS 0, DO NOT KNOW WHICH COMES OUT NEXT, SO PICK RANDOM
                score = 10;
            }
            else if (Difficulty == AIDifficulty.Medium)
            {
                //decent likelihood of selecting optimal character
                if (typeMultiplier >= 1.0f)
                {
                    score = (int)Mathf.Round(score * (typeMultiplier * 2));
                }
                else
                {
                    score = (int)Mathf.Round(score * (typeMultiplier / 2));
                }
            }
            else if (Difficulty == AIDifficulty.Hard)
            {
                //2x effective is same as Medium, but 3x effective is much more likely
                score = (int)Mathf.Round(score * Mathf.Pow(typeMultiplier, 2));
            }
            else
            {
                //extremely high likelihood of selecting optimal character
                score = (int)Mathf.Round(score * Mathf.Pow(typeMultiplier, 3));
            }

            //add score to pool
            scoresPool.Add(score);
        }

        //get choice and find what index it landed on
        int choice = UnityEngine.Random.Range(0, scoresPool.Sum());
        for (int i = 0; i < scoresPool.Count; i++)
        {
            //subtract item added; if now NEGATIVE, then this is where choice landed
            choice -= scoresPool[i];
            if (choice < 0)
            {
                return i;
            }
        }


        //it is possible that every one added to pool has 0, so will never return above
        for (int i = 0; i < enemyChars.Length; i++)
        {
            //return first character at > 0HP
            if (enemyChars[i].HP > 0)
            {
                return i;
            }
        }

        //needed for compilation
        return 0;
    }

    void CalcAbilityScores(BattleChar currEnemy, BattleChar currPlayer)
    {
        ///Calculates Score of each of enemy's Abilities

        //calculate Score for each ability
        foreach (Ability ability in currEnemy.Abilities)
        {
            ability.CalcScore(currEnemy, currPlayer);
        }

        //if Easy, modify by 25%; if Medium, modify by 10%
        if (Difficulty == AIDifficulty.Easy)
        {
            MakePreferDamaging(currEnemy.Abilities);
        }
        else if (Difficulty == AIDifficulty.Medium)
        {
            MakePreferDamaging(currEnemy.Abilities, modifier: 1.1f);
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

        //TEMP
        Debug.Log("Scores: " + currEnemy.Abilities[0].Score + " " + currEnemy.Abilities[1].Score + " "
            + currEnemy.Abilities[2].Score + " " + currEnemy.Abilities[3].Score);
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
            //don't count Abilities that cost too much Energy even at max
            if (ability.EnergyCost(currEnemy) > currEnemy.Energy
                && ability.EnergyCost(currEnemy) <= currEnemy.MaxEnergy)
            {
                numUnusable++;
            }
            if (ability.Name != "EMPTY" && ability.Name != "MISSING"
                && ability.EnergyCost(currEnemy) <= currEnemy.MaxEnergy)
            {
                totalValid++;
            }
        }

        //allow pass roll only if at least one Ability unusable and maxScore < 200
        if ((float)numUnusable / totalValid > 0.0f && maxScore < 200)
        {
            //passScore is proportional to missing energy (one minus)
            int passScore = Mathf.RoundToInt((1.0f - (float)currEnemy.Energy / currEnemy.MaxEnergy) * 75);

            //adjust passScore based on % of Abilities that are unusable (can't be 0% or 100%)
            passScore = Mathf.RoundToInt(passScore * ((float)numUnusable / totalValid));
            
            //TEMP
            Debug.Log("Pass score: " + passScore);
            //TEMP

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

    void MakePreferDamaging(Ability[] abilities, float modifier = 1.25f)
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
