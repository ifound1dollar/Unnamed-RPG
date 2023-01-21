using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum AIDifficulty { Easy, Normal, Hard, Boss, Wild }
public class BattleAI
{
    public struct AIContextObject
    {
        public AIDifficulty Difficulty { get; private set; }
        public BattleChar[] EnemyChars { get; private set; }
        public BattleChar Enemy { get; private set; }
        public BattleChar Player { get; private set; }

        public AIContextObject(AIDifficulty difficulty, BattleChar[] enemyChars, BattleChar enemy, BattleChar player)
        {
            Difficulty = difficulty;
            EnemyChars = enemyChars;
            Enemy = enemy;
            Player = player;
        }

        public int CountEnemyRemaining()
        {
            int count = 0;
            foreach (BattleChar enemy in EnemyChars)
            {
                if (enemy.HP > 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
    



    /// <summary>
    /// Calculates Ability Scores and chooses Ability to use or to Pass this turn
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <returns>Ability to use, or null to Pass</returns>
    public Ability ChooseAbility(AIContextObject aiContext)
    {
        ///Calculate Ability scores and dynamically choose an Ability to use

        //if Wild, pick random valid Ability (only passing if none valid)
        if (aiContext.Difficulty == AIDifficulty.Wild)
        {
            return GetWildAbilityChoice(aiContext.Enemy);
        }

        //calculate Ability scores and assign values to each corresponding Ability
        CalcAbilityScores(aiContext);

        //create Ability pool
        List<Ability> abilityPool = CreateAbilityPool(aiContext);

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

    /// <summary>
    /// Determines whether to Swap instead of using one of the candidate Abilities
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <param name="maxScore">Maximum score of all candidate Abilities</param>
    /// <returns>Whether to swap, true if should</returns>
    public bool CheckShouldSwap(AIContextObject aiContext, int maxScore)
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

        BattleChar enemy = aiContext.Enemy;

        //if one remaining, active for 1-2 turns, Easy difficulty, or cannot swap
        if (aiContext.CountEnemyRemaining() <= 1 || enemy.TurnsActive <= 2
            || aiContext.Difficulty == AIDifficulty.Easy || !enemy.CheckCanSwap())
        {
            return false;
        }


        //get effectiveness of player vs enemy
        float mult1 = Effectiveness.GetMultiplier(aiContext.Player.SpeciesData.Type1, enemy.SpeciesData.Type1, enemy.SpeciesData.Type2);
        float mult2 = Effectiveness.GetMultiplier(aiContext.Player.SpeciesData.Type2, enemy.SpeciesData.Type1, enemy.SpeciesData.Type2);
        float typeMultiplier = mult1 * mult2;

        int score = 0;

        //+2 points if active status that is NOT Berserk
        if (enemy.HasActiveStatus() && enemy.StatusActive != StatusEffect.Berserk)
        {
            score += 2;
        }

        //+-1 point per modifier (total magnitude, change by inverse)
        score -= enemy.CountModifierTotal();

        //+1 point if less than 50% Energy remaining, +2 if less than 25%
        score += (enemy.MaxEnergy / enemy.Energy) / 2;

        //multiplier calculation
        score = Mathf.RoundToInt(score * typeMultiplier);


        //if maxScore >= 150 (likely lethal hit) OR score < 5 (not enough reason to swap)
        if (maxScore >= 150 || score < 5)
        {
            return false;
        }

        //increase score to be comparable with maxScore (ex. score now 30, maxScore 70)
        score *= 5;

        //Medium difficulty has only half chance to swap, while Hard and Boss have normal
        if (aiContext.Difficulty == AIDifficulty.Normal)
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

    /// <summary>
    /// Determines most preferable character to swap to and returns its index
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <param name="currEnemyIndex">Index of BattleSystem's currEnemy in enemyChars</param>
    /// <returns>Index of character to swap to</returns>
    public int ChooseSwapChar(AIContextObject aiContext, int currEnemyIndex)
    {
        ///Finds the most preferable character to swap to and chooses it, weighted
        /// by difficulty.

        BattleChar[] enemies = aiContext.EnemyChars;

        //calculate best option and add to pool (Easy is equal score for each)
        List<int> scoresPool = new();
        for (int i = 0; i < enemies.Length; i++)
        {
            //if currEnemy or at 0HP, add 0 to pool and continue
            if (currEnemyIndex == i || enemies[i].HP == 0)
            {
                scoresPool.Add(0);
                continue;
            }

            //get effectiveness of player vs enemy
            float mult1 = Effectiveness.GetMultiplier(aiContext.Player.SpeciesData.Type1, enemies[i].SpeciesData.Type1, enemies[i].SpeciesData.Type2);
            float mult2 = Effectiveness.GetMultiplier(aiContext.Player.SpeciesData.Type2, enemies[i].SpeciesData.Type1, enemies[i].SpeciesData.Type2);
            float typeMultiplier = mult1 * mult2;

            //make prelim score and modify slightly based on HP and Level difference
            int score = 10;
            if ((float)enemies[i].HP / enemies[i].MaxHP < 0.5f)
            {
                score -= 3;
            }
            if (enemies[i].Level - aiContext.Player.Level >= 3)
            {
                score += 3;
            }

            //Easy: 10 for all | Medium: multiplier*2 | Hard: multiplier^2 | Boss: multiplier^3
            if (aiContext.Difficulty == AIDifficulty.Easy || aiContext.Player.HP == 0)
            {
                //IF CURRPLAYER HP IS 0, DO NOT KNOW WHICH COMES OUT NEXT, SO PICK RANDOM
                score = 10;
            }
            else if (aiContext.Difficulty == AIDifficulty.Normal)
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
            else if (aiContext.Difficulty == AIDifficulty.Hard)
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
        for (int i = 0; i < enemies.Length; i++)
        {
            //return first character at > 0HP that is not currEnemy
            if (enemies[i].HP > 0 && i != currEnemyIndex)
            {
                return i;
            }
        }

        //needed for compilation
        return 0;
    }




    /// <summary>
    /// Dynamically calculates Score of each Ability
    /// </summary>
    /// <param name="aiContext">Contains all context required for AI decision</param>
    void CalcAbilityScores(AIContextObject aiContext)
    {
        ///Calculates Score of each of enemy's Abilities

        BattleChar enemy = aiContext.Enemy;
        BattleChar player = aiContext.Player;

        //calculate Score for each ability
        foreach (Ability ability in enemy.Abilities)
        {
            ability.CalcScore(aiContext);
        }

        //if Easy, modify by 25%; if Medium, modify by 10%
        if (aiContext.Difficulty == AIDifficulty.Easy)
        {
            MakePreferDamaging(enemy.Abilities);
        }
        else if (aiContext.Difficulty == AIDifficulty.Normal)
        {
            MakePreferDamaging(enemy.Abilities, modifier: 1.1f);
        }

        //if only one enemy remains, prefer damaging
        //if (aiContext.CountEnemyRemaining() == 1)
        //{
        //    MakePreferDamaging(enemy.Abilities);
        //}

        //if HP of player or enemy is below 50%, prefer damaging (integer division)
        if (enemy.HP <= enemy.MaxHP / 2 || player.HP <= player.MaxHP / 2)
        {
            MakePreferDamaging(enemy.Abilities);
        }

        //if enemy is 5+ levels higher than player, prefer damaging
        if (enemy.Level - player.Level >= 5)
        {
            MakePreferDamaging(enemy.Abilities);
        }

        //TEMP
        Debug.Log("Scores: " + enemy.Abilities[0].Score + " " + enemy.Abilities[1].Score + " "
            + enemy.Abilities[2].Score + " " + enemy.Abilities[3].Score);
    }

    /// <summary>
    /// Creates and returns pool of candidate Abilities based on calculated Scores
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <returns>List of candidate Abilities</returns>
    List<Ability> CreateAbilityPool(AIContextObject aiContext)
    {
        ///Places every Ability within 75% of the highest Score in a pool

        Ability[] abilities = aiContext.Enemy.Abilities;

        //find maxScore and cutoff score, then create new List
        int maxScore = abilities.Max(x => x.Score);
        int cutoff = (int)Mathf.Ceil(maxScore * 0.75f);
        List<Ability> abilityPool = new();

        //check if passing is preferable/forced this turn, returning empty if so
        if (CheckShouldPass(aiContext, maxScore))
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

    /// <summary>
    /// Selects Ability from pool of candidate Abilities via weighted rolling
    /// </summary>
    /// <param name="abilityPool">Pool of Abilities to choose from</param>
    /// <returns>Chosen Ability</returns>
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

    /// <summary>
    /// Determines whether to Pass this turn or use one of the candidate Abilities
    /// </summary>
    /// <param name="aiContext">Contains difficulty, all enemies, current enemy, and current player</param>
    /// <param name="maxScore">Maximum score of all candidate Abilities</param>
    /// <returns>Whether to pass, true if should</returns>
    bool CheckShouldPass(AIContextObject aiContext, int maxScore)
    {
        ///Determine whether passing this turn is preferable or forced

        BattleChar enemy = aiContext.Enemy;

        //if maxScore is 0, none are usable, so must pass
        if (maxScore == 0)
        {
            return true;
        }

        //if HP less than 25% or at 100% Energy, do not pass
        if (enemy.HP <= (enemy.MaxHP / 4.0f) || enemy.Energy == enemy.MaxEnergy)
        {
            return false;
        }

        ///IF LESS THAN 25% ENERGY REMAINING or GREATER THAN 66% ABILITIES ARE UNUSABLE,
        /// SHOULD PASS
        ///SHOULD MAKE BEST ESTIMATE IF THERE IS A LETHAL ABILITY (>50 MAXSCORE? OR JUST
        /// DO POOL, MAKES PASS BASICALLY GUARANTEED IF ONLY AVAILABLE ABILITY HAS SCORE 1)
        /// 
        ///IMPORTANT: IF MAXSCORE IS VERY HIGH, THEN PASSING IS NATURALLY NOT PREFERABLE
        /// (HIGH MAXSCORE IMPLIES EXTREMELY FAVORABLE ATTACK CHOICE)

        //find number of Abilities without enough Energy to use AND total valid Abilities
        int numUnusable = 0;
        int totalValid = 0;
        foreach (Ability ability in enemy.Abilities)
        {
            //don't count Abilities that cost too much Energy even at max
            if (ability.EnergyCost(enemy) > enemy.Energy
                && ability.EnergyCost(enemy) <= enemy.MaxEnergy)
            {
                numUnusable++;
            }
            if (ability.Name != "EMPTY" && ability.Name != "MISSING"
                && ability.EnergyCost(enemy) <= enemy.MaxEnergy)
            {
                totalValid++;
            }
        }

        //if totalValid is 0, return true forcing pass (divide by 0)
        if (totalValid == 0)
        {
            return true;
        }

        //allow pass roll only if at least one Ability unusable and maxScore < 150
        if ((float)numUnusable / totalValid > 0.0f && maxScore < 150)
        {
            //passScore is proportional to missing energy (one minus)
            int passScore = Mathf.RoundToInt((1.0f - (float)enemy.Energy / enemy.MaxEnergy) * 100);

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




    /// <summary>
    /// Adjusts Score of all Abilities, increasing if damaging / reducing if non-damaging
    /// </summary>
    /// <param name="abilities">Enemy Abilities array</param>
    /// <param name="modifier">Ratio to adjust by, defaults to 1.25</param>
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

    /// <summary>
    /// Adds the second-highest Scoring Ability to the candidate pool, which was not within 75% of max
    /// </summary>
    /// <param name="abilities">Enemy Abilities array</param>
    /// <param name="maxScore">Maximum Score of all candidate Abilities</param>
    /// <param name="abilityPool">Existing pool of candiate Abilities</param>
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

    /// <summary>
    /// Choose random (validated) Ability from all Enemy Abilities
    /// </summary>
    /// <param name="enemy">Wild Enemy reference</param>
    /// <returns>Ability to use, null if must Pass</returns>
    Ability GetWildAbilityChoice(BattleChar enemy)
    {
        ///Make completely random, unbiased choice of Ability for Wild enemies

        //add all valid Abilities to abilityPool ONCE
        List<Ability> abilityPool = new();
        foreach (Ability ability in enemy.Abilities)
        {
            if (ability.IsUsable(enemy))
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
            return enemy.Abilities[UnityEngine.Random.Range(0, abilityPool.Count)];
        }
    }
}
