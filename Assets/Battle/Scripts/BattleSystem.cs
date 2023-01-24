using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleSystem : MonoBehaviour
{
    enum BattleState { Start, WaitingChoice, Preparing, Attacking, EndingTurn, NewTurn, BattleEnded, Busy }
    enum BattleChoice { None, Attack, Pass, Swap, FleeForfeit }

    [SerializeField] PlayerHud playerHud;
    [SerializeField] EnemyHud enemyHud;
    [SerializeField] DialogBox dialogBox;
    [SerializeField] PartyMenu partyMenu;
    [SerializeField] Image playerImage;
    [SerializeField] Image enemyImage;

    BattleChar[] playerChars;
    BattleChar[] enemyChars;
    BattleChar currPlayer;
    BattleChar currEnemy;

    readonly BattleAI battleAI = new();
    BattleAI.AIDifficulty difficulty;
    float textDelay = 1.5f;

    int turnNumber;
    BattleState state;
    BattleChoice playerChoice;
    BattleChoice enemyChoice;
    int playerSwapIndex;
    int enemySwapIndex;




    //setup
    public void SetPersistentData(PersistentData data)
    {
        difficulty = data.Difficulty;
        dialogBox.DialogSpeed = data.DialogSpeed;
        textDelay = data.TextDelay;

        playerChars = data.PlayerChars;
    }
    public void BeginBattle(BattleParty enemyParty)
    {
        //find first BattleChar in array with >0HP to make currPlayer
        foreach (BattleChar battleChar in playerChars)
        {
            if (battleChar.HP > 0)
            {
                currPlayer = battleChar;
                currPlayer.WasActive = true;
                break;
            }
        }

        //InitPlayerParty();
        InitEnemyParty(enemyParty);

        //load player and enemy sprites from BattleChar's species data
        playerImage.sprite = currPlayer.SpeciesData.BackSprite;
        enemyImage.sprite = currEnemy.SpeciesData.FrontSprite;

        //set hud for each
        playerHud.SetHUD(currPlayer);
        enemyHud.SetHUD(currEnemy);
        dialogBox.SetAbilityButtons(currPlayer);

        StartCoroutine(Loop());
    }
    void InitEnemyParty(BattleParty enemyParty)
    {
        int targetLevel = CalcEnemyLevel();

        enemyChars = new BattleChar[enemyParty.Team.Length];
        for (int i = 0; i < enemyParty.Team.Length; i++)
        {
            //calculate new level within 5% deviation range of targetLevel, then initialize
            int level = Mathf.RoundToInt(UnityEngine.Random.Range(0.95f, 1.05f) * targetLevel);
            enemyChars[i] = new BattleChar(enemyParty.Team[i], difficulty, playerTeam: false, argLevel: level);
        }

        foreach (BattleChar battleChar in enemyChars)
        {
            if (battleChar.HP > 0)
            {
                currEnemy = battleChar;
                currEnemy.WasActive = true;
                break;
            }
        }
    }
    int CalcEnemyLevel()
    {
        //TEMP
        int minLevel = 1;
        int maxLevel = 10;
        //TEMP

        //calculate average and max levels of player chars, then set prelim targetLevel
        int playerAvg = Mathf.RoundToInt(playerChars.Sum(x => x.Level) / (float)playerChars.Length);
        int playerMax = playerChars.Max(x => x.Level);
        int targetLevel = playerAvg;

        //different behavior for wild/easy, normal, and hard/boss
        if (difficulty == BattleAI.AIDifficulty.Easy || difficulty == BattleAI.AIDifficulty.Wild)
        {
            //if easy or wild, set to 4% below player average
            targetLevel = Mathf.RoundToInt(playerAvg * 0.96f);

            //force targetLevel to be at minimum 8% below player max (TOTAL 4-8% BELOW)
            targetLevel = Mathf.Max(targetLevel, Mathf.RoundToInt(playerMax * 0.92f));
        }
        else if (difficulty == BattleAI.AIDifficulty.Normal)
        {
            //if normal, keep equal to player average

            //force targetLevel to be at minimum 4% below player max (TOTAL 0-4% BELOW)
            targetLevel = Mathf.Max(targetLevel, Mathf.RoundToInt(playerMax * 0.96f));
        }
        else if (difficulty == BattleAI.AIDifficulty.Hard || difficulty == BattleAI.AIDifficulty.Boss)
        {
            //if hard or boss, set to 4% above player average
            targetLevel = Mathf.RoundToInt(playerAvg * 1.04f);

            //force targetLevel to be at minimum equal to player max (TOTAL 0-4% ABOVE)
            targetLevel = Mathf.Max(targetLevel, playerMax);
        }

        //finally, verify that targetLevel is within min and max range ONLY IF NOT BOSS
        if (difficulty != BattleAI.AIDifficulty.Boss)
        {
            targetLevel = Mathf.Clamp(targetLevel, minLevel, maxLevel);
        }

        return targetLevel;
    }




    IEnumerator Loop()
    {
        //TEMP
        yield return dialogBox.DialogSet("TEST TEXT TEST TEXT TEST TEXT TEST TEXT TEST TEXT ");
        yield return new WaitForSeconds(textDelay);
        yield return dialogBox.DialogAppend("Test append.");
        yield return new WaitForSeconds(textDelay);
        //TEMP

        while (state != BattleState.BattleEnded)
        {
            //enable player input and wait until input gotten
            yield return WaitPlayerAction();

            //check if player flee/forfeit
            if (playerChoice == BattleChoice.FleeForfeit)
            {
                yield return EndBattle(playerWin: false);
            }

            //enemy AI make choice and set enemyChoice
            GetEnemyChoice();

            //set player and enemy UsedAbility to null if they did not Attack this turn (reset ConsecutiveUses first)
            if (playerChoice != BattleChoice.Attack && currPlayer.UsedAbility != null)
            {
                currPlayer.UsedAbility.ConsecutiveUses = 0;
                currPlayer.UsedAbility = null;
            }
            if (enemyChoice != BattleChoice.Attack && currEnemy.UsedAbility != null)
            {
                currEnemy.UsedAbility.ConsecutiveUses = 0;
                currEnemy.UsedAbility = null;
            }

            //short dialog and delay before actions start
            yield return dialogBox.DialogSet("Preparing...");
            yield return new WaitForSeconds(textDelay);

            //check status effect of either currPlayer or currEnemy ending early
            yield return CheckStatusEndEarly();

            //check swaps, performing any chosen swaps by player or enemy and resetting swapIndex for either/both
            yield return CheckSwaps();

            //perform attacks
            yield return PerformAttacks();

            //check HP immediately after attacks, before end-of-turn operations
            yield return CheckHP();

            //perform end-of-turn operations like status effects, team effects, etc.
            yield return EndOfTurnOperations();

            //perform new turn operations (increment turn, take snapshot, etc.)
            yield return NewTurnOperations();
        }
    }
    



    //WaitingChoice
    IEnumerator WaitPlayerAction()
    {
        //skip action if Delaying, Recharging, or MultiTurnAbility
        if (currPlayer.IsDelaying || currPlayer.IsRecharging || currPlayer.MultiTurnAbility > 0)
        {
            playerChoice = BattleChoice.Attack; //ensure remains set to Attack
            yield break;
        }

        yield return dialogBox.DialogSet("Player choice...");
        dialogBox.ShowMainButtons(true);
        dialogBox.SelectAttackButton();

        //WaitingChoice will be un-set when player presses a button
        state = BattleState.WaitingChoice;
        yield return new WaitUntil(() => state != BattleState.WaitingChoice);
        dialogBox.ShowMainButtons(false);
    }
    void GetEnemyChoice()
    {
        //skip action if Delaying, Recharging, or MultiTurnAbility
        if (currEnemy.IsDelaying || currEnemy.IsRecharging || currEnemy.MultiTurnAbility > 0)
        {
            enemyChoice = BattleChoice.Attack;  //ensure remains set to attack
            return;
        }

        BattleAI.AIContextObject aiContext = new(difficulty, enemyChars, currEnemy, currPlayer);

        //get chosen Ability and assign to temp variable, then find max Score of all
        Ability tempAbility = battleAI.ChooseAbility(aiContext);
        int maxScore = currEnemy.Abilities.Max(x => x.Score);

        //if should swap, set swap index and choose Swap; else Pass or Attack
        if (battleAI.CheckShouldSwap(aiContext, maxScore))
        {
            enemySwapIndex = battleAI.ChooseSwapChar(aiContext, GetCurrBattleCharIndex(playerTeam: false));
            enemyChoice = BattleChoice.Swap;
        }
        else
        {
            //if null, then no Ability was chosen, so must Pass; else attack
            if (tempAbility == null)
            {
                enemyChoice = BattleChoice.Pass;
            }
            else
            {
                currEnemy.UsedAbility = tempAbility;
                enemyChoice = BattleChoice.Attack;
            }
        }
    }




    //Preparing (swaps, status end early)
    IEnumerator CheckSwaps()
    {
        if (playerChoice == BattleChoice.Swap)
        {
            //interrupt swap if enemy used Ability that interrupts swapping
            if (enemyChoice == BattleChoice.Attack && currEnemy.UsedAbility.Interrupts)
            {
                yield return DoAttack(currEnemy, currPlayer);
                enemyChoice = BattleChoice.None;

                //if now Trapped, then Interrupt ability stopped swaps and cannot swap; else swap as usual
                if (currPlayer.Trapped > 0)
                {
                    yield return dialogBox.DialogSet(currPlayer.Name + " could not swap out!");
                    yield return new WaitForSeconds(textDelay);

                    playerChoice = BattleChoice.None;
                    playerSwapIndex = -1;
                    yield break;
                }
            }

            yield return PerformSwap(playerTeam: true);
        }
        if (enemyChoice == BattleChoice.Swap)
        {
            if (playerChoice == BattleChoice.Attack && currPlayer.UsedAbility.Interrupts)
            {
                yield return DoAttack(currPlayer, currEnemy);
                playerChoice = BattleChoice.None;

                if (currEnemy.Trapped > 0)
                {
                    yield return dialogBox.DialogSet(currEnemy.Name + " could not swap out!");
                    yield return new WaitForSeconds(textDelay);

                    enemyChoice = BattleChoice.None;
                    enemySwapIndex = -1;
                    yield break;
                }
            }

            yield return PerformSwap(playerTeam: false);
        }

        yield break;
    }
    IEnumerator PerformSwap(bool playerTeam)
    {
        if (playerTeam)
        {
            //reset temporary data and transfer data, then set currPlayer to new BattleChar
            currPlayer.ResetAll();
            currPlayer.TransferAllToNew(playerChars[playerSwapIndex]);
            currPlayer = playerChars[playerSwapIndex];
            currPlayer.WasActive = true;

            //update sprite, hud, and button data
            playerImage.sprite = currPlayer.SpeciesData.BackSprite;
            playerHud.SetHUD(currPlayer);
            dialogBox.SetAbilityButtons(currPlayer);

            //update dialog text
            yield return dialogBox.DialogSet("Player swapped to " + playerChars[playerSwapIndex].Name + ".");
            yield return new WaitForSeconds(textDelay);

            //if any traps hit, update HUD and print dialog returned from hitting traps
            List<string> trapsHit = currPlayer.DoTrapsHit();
            if (trapsHit.Count > 0)
            {
                playerHud.UpdateHUD(currPlayer);
                foreach (string text in trapsHit)
                {
                    yield return dialogBox.DialogSet(text);
                    yield return new WaitForSeconds(textDelay);
                }
            }
        }
        else
        {
            currEnemy.ResetAll();
            currEnemy.TransferAllToNew(enemyChars[enemySwapIndex]);
            currEnemy = enemyChars[enemySwapIndex];
            currEnemy.WasActive = true;

            enemyImage.sprite = currEnemy.SpeciesData.FrontSprite;
            enemyHud.SetHUD(currEnemy);

            yield return dialogBox.DialogSet("Enemy swapped to " + enemyChars[enemySwapIndex].Name + ".");
            yield return new WaitForSeconds(textDelay);

            List<string> trapsHit = currEnemy.DoTrapsHit();
            if (trapsHit.Count > 0)
            {
                enemyHud.UpdateHUD(currEnemy);
                foreach (string text in trapsHit)
                {
                    yield return dialogBox.DialogSet(text);
                    yield return new WaitForSeconds(textDelay);
                }
            }
        }
    }
    IEnumerator CheckStatusEndEarly()
    {
        string temp = currPlayer.CheckEffectEndEarly();
        if (temp != "")
        {
            playerHud.SetHUD(currPlayer);
            yield return dialogBox.DialogSet(temp);
            yield return new WaitForSeconds(textDelay);
        }

        temp = currEnemy.CheckEffectEndEarly();
        if (temp != "")
        {
            enemyHud.SetHUD(currEnemy);
            yield return dialogBox.DialogSet(temp);
            yield return new WaitForSeconds(textDelay);
        }

        //after checking status, time to begin attacking
        state = BattleState.Attacking;
    }




    //Attacking
    IEnumerator PerformAttacks()
    {
        ///Determine attack order and do attacks

        //if both attacked this turn
        if (playerChoice == BattleChoice.Attack && enemyChoice == BattleChoice.Attack)
        {
            //BattleChar with higher Agility + Priority modification attacks first
            if ((currPlayer.Agility + currPlayer.UsedAbility.Priority * 1000)
                >= (currEnemy.Agility + currEnemy.UsedAbility.Priority * 1000))
            {
                yield return DoAttack(currPlayer, currEnemy);
                yield return DoAttack(currEnemy, currPlayer);
            }
            else
            {
                yield return DoAttack(currEnemy, currPlayer);
                yield return DoAttack(currPlayer, currEnemy);
            }
        }
        //else if only player attacked, enemy passed/swapped
        else if (playerChoice == BattleChoice.Attack)
        {
            if (enemyChoice == BattleChoice.Pass)
            {
                yield return dialogBox.DialogSet(currEnemy.Name + " passed this turn.");
                yield return new WaitForSeconds(textDelay);
            }
            yield return DoAttack(currPlayer, currEnemy);
        }
        //else if only enemy attacked, player passed/swapped
        else if (enemyChoice == BattleChoice.Attack)
        {
            if (playerChoice == BattleChoice.Pass)
            {
                yield return dialogBox.DialogSet(currPlayer.Name + " passed this turn.");
                yield return new WaitForSeconds(textDelay);
            }
            yield return DoAttack(currEnemy, currPlayer);
        }
        //else if both passed, so just acknowledge
        else if (playerChoice == BattleChoice.Pass && enemyChoice == BattleChoice.Pass)
        {
            yield return dialogBox.DialogSet(currPlayer.Name + " passed this turn.");
            yield return new WaitForSeconds(textDelay);
            yield return dialogBox.DialogSet(currEnemy.Name + " passed this turn.");
            yield return new WaitForSeconds(textDelay);
        }

        //after attacks, set state to EndingTurn
        state = BattleState.EndingTurn;
    }
    IEnumerator DoAttack(BattleChar user, BattleChar target)
    {
        ///Carry out attacks for this turn, handling all attack-related operations

        #region Prelim checks (0HP, Stunned, Delaying, etc.)
        //if either at 0HP, break and do not do attack
        if (user.HP == 0 || target.HP == 0)
        {
            yield break;
        }

        //if user Stunned, acknowledge and do not attack (interrupt Delaying and Recharging)
        if (user.StatusActive == StatusEffect.Stunned)
        {
            user.IsDelaying = false;
            user.IsRecharging = false;
            yield return dialogBox.DialogSet(user.Name + " is Stunned!");
            yield return new WaitForSeconds(textDelay);
            yield break;
        }

        //if user Frozen, 33% chance to be unable to attack this turn (does same as Stunned)
        if (user.StatusActive == StatusEffect.Frozen)
        {
            if (UnityEngine.Random.Range(0, 100) < 33)
            {
                user.IsDelaying = false;
                user.IsRecharging = false;
                yield return dialogBox.DialogSet(user.Name + " is too cold to move!");
                yield return new WaitForSeconds(textDelay);
                yield break;
            }
        }

        //if user currently Recharging, do not attack
        if (user.IsRecharging)
        {
            user.IsRecharging = false;
            yield return dialogBox.DialogSet(user.Name + " is recharging!");
            yield return new WaitForSeconds(textDelay);
            yield break;
        }

        //if user's UsedAbility is Delayed, check if currently waiting or starting delay
        if (user.UsedAbility.Delayed)
        {
            //if user is currently waiting, use Ability this turn; else begin actual delay
            if (user.IsDelaying)
            {
                user.IsDelaying = false;
            }
            else
            {
                user.IsDelaying = true;
                yield return dialogBox.DialogSet(user.Name + " is preparing to use "
                    + user.UsedAbility.Name + " next turn!");
                yield return new WaitForSeconds(textDelay);
                yield break;
            }
        }

        //if user not currently Recharging and is using Recharge move, recharge next turn
        if (user.UsedAbility.Recharge)
        {
            user.IsRecharging = true;
        }

        //if user is Trapped, cannot use movement Abilities
        if (user.Trapped > 0 && user.UsedAbility.IsMovement)
        {
            yield return dialogBox.DialogSet(user.Name + " is immobilized and cannot use "
                + user.UsedAbility.Name + "!");
            yield return new WaitForSeconds(textDelay);
            yield break;
        }
        #endregion

        //create AbilityData object to pass to Ability's UseAbility method
        AbilityData data = new(user, target, turnNumber, dialogBox, textDelay, playerHud, enemyHud);

        //update dialog with use text and wait
        yield return dialogBox.DialogSet(user.Name + " used " + user.UsedAbility.Name + "!");
        yield return new WaitForSeconds(textDelay);

        //do not consume Energy if subsequent uses of a multi-turn Ability
        if (user.MultiTurnAbility == 0)
        {
            //consume energy and update HUD, clear dialog, then progress to animation (seamlessly)
            user.Energy -= user.UsedAbility.EnergyCost(user);
            playerHud.UpdateHUD(currPlayer);
            enemyHud.UpdateHUD(currEnemy);
        }
        yield return dialogBox.DialogSet("TEMP ANIMATION TEXT");

        //if hit target, do attack and after effects; else acknowledge miss and continue
        if (user.UsedAbility.CheckAccuracy(user, target))
        {
            //PLAY HIT ANIMATION, DELAYING UNTIL COMPLETION
            yield return new WaitForSeconds(1.5f);  //TEMP ANIMATION DELAY

            //use Ability, then do after-effects like Thorns (dialog and delays handled in-method)
            yield return user.UsedAbility.UseAbility(data);
            yield return DoAfterEffects(data);

            //BOTH PLAYER AND ENEMY SNAPSHOT UPDATES HERE
        }
        else
        {
            //PLAY MISS ANIMATION, DELAYING UNTIL COMPLETION
            yield return new WaitForSeconds(1.5f);  //TEMP ANIMATION DELAY

            yield return dialogBox.DialogSet("The attack missed!");
            yield return new WaitForSeconds(textDelay);
        }
    }
    IEnumerator DoAfterEffects(AbilityData data)
    {
        ///Handles any attack after-effects (Thorns recoil, reflect Status Effect, etc).
        ///Updates dialog and HUD immediately after each operation, if any.

        //Thorns team effect
        if (data.Target.Thorns > 0)
        {
            //deal 10% max HP damage if user's Attack made contact
            if (data.User.UsedAbility.MakesContact)
            {
                data.User.TakeDamage(Mathf.RoundToInt(data.User.MaxHP / 10.0f));
                playerHud.UpdateHUD(currPlayer);
                enemyHud.UpdateHUD(currEnemy);

                yield return dialogBox.DialogSet(data.User.Name + " took damage from " +
                    data.Target.Name + "'s Thorns!");
                yield return new WaitForSeconds(textDelay);
            }
        }

        //ReflectDamage turn effect
        if (data.Target.ReflectDamage > 0)
        {
            //if damaging attack used on target, deal ReflectDamage (as percent) to user
            if (data.User.UsedAbility.Category == Category.Physical
                || data.User.UsedAbility.Category == Category.Magic)
            {
                data.User.TakeDamage(Mathf.RoundToInt(data.Damage * (data.Target.ReflectDamage / 100.0f)));
                playerHud.UpdateHUD(currPlayer);
                enemyHud.UpdateHUD(currEnemy);

                yield return dialogBox.DialogSet(data.Target.Name + " reflected damage back to "
                    + data.User.Name + "!");
                yield return new WaitForSeconds(textDelay);
            }
        }

        //ReflectStatus turn effect
        if (data.Target.ReflectStatus != null && data.Target.ReflectStatus != StatusEffect.None)
        {
            //if null, is not active; if None, is ready but not actually used

            //set status to user for 5 turns, status stored in ReflectStatus (CANNOT BE NULL)
            if (data.User.SetStatusEffect((StatusEffect)data.Target.ReflectStatus, 5))
            {
                playerHud.UpdateHUD(currPlayer);
                enemyHud.UpdateHUD(currEnemy);

                yield return dialogBox.DialogSet(data.User.Name + "'s attempt to apply "
                        + data.Target.ReflectStatus + " to " + data.Target.Name + " rebounded!");
                yield return new WaitForSeconds(textDelay);
            }
        }

        //Toxic Skin
        if (data.Target.PassiveAbility.Name == "Toxic Skin")
        {
            if (UnityEngine.Random.Range(0, 100) < 10)
            {
                //10% chance to Poison attacker
                if (data.User.SetStatusEffect(StatusEffect.Poisoned, 5))
                {
                    playerHud.UpdateHUD(currPlayer);
                    enemyHud.UpdateHUD(currEnemy);

                    yield return dialogBox.DialogSet($"{data.Target.Name}'s Toxic Skin poisoned {data.User.Name}!");
                    yield return new WaitForSeconds(textDelay);
                }
            }
        }

        yield break;
    }




    //EndingTurn
    IEnumerator EndOfTurnOperations()
    {
        //NOTE: this method will only ever be called when both current BattleChars are > 0HP

        //IMPORTANT: CheckHP() is called whenever necessary in the below methods

        yield return HandleStatusEffectOperations();
        yield return HandleTeamEffectOperations();
        yield return HandleTrapOperations();
        yield return HandleFieldEffectOperations();
    }
    IEnumerator HandleStatusEffectOperations()
    {
        //active player BattleChar will print to dialog
        if (currPlayer.StatusActive == StatusEffect.Burned)
        {
            yield return dialogBox.DialogSet(currPlayer.DoBurnedDamage());
            yield return new WaitForSeconds(textDelay);
        }

        //if currPlayer now at 0HP, don't check Burned status effect ending
        if (currPlayer.HP != 0)
        {
            string temp = currPlayer.DecrementStatusEffect();
            if (temp != "")
            {
                yield return dialogBox.DialogSet(temp);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //all inactive player BattleChars will silently decrement (no Burned damage)
        foreach (BattleChar battleChar in playerChars)
        {
            //skip if 0HP or currPlayer (currPlayer already handled above)
            if (battleChar.HP == 0 || battleChar == currPlayer) { continue; }
            battleChar.DecrementStatusEffect();
        }

        //CHECK HP HERE AND DO SWAP IF NECESSARY
        yield return CheckHP();


        //active enemy BattleChar will print to dialog
        if (currEnemy.StatusActive == StatusEffect.Burned)
        {
            yield return dialogBox.DialogSet(currEnemy.DoBurnedDamage());
            yield return new WaitForSeconds(textDelay);
        }

        //if currEnemy now at 0HP, don't check Burned status effect ending
        if (currEnemy.HP != 0)
        {
            string temp = currEnemy.DecrementStatusEffect();
            if (temp != "")
            {
                yield return dialogBox.DialogSet(temp);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //all inactive enemy BattleChars will silently decrement (no Burned damage)
        foreach (BattleChar battleChar in enemyChars)
        {
            if (battleChar.HP == 0 || battleChar == currEnemy) { continue; }
            battleChar.DecrementStatusEffect();
        }

        //CHECK HP HERE AND DO SWAP IF NECESSARY
        yield return CheckHP();
    }
    IEnumerator HandleTeamEffectOperations()
    {
        List<string> teamEffectStrings;

        //player team effect actions
        teamEffectStrings = currPlayer.DoTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            playerHud.UpdateHUD(currPlayer);
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT
        yield return CheckHP();

        //player team effect decrements
        teamEffectStrings = currPlayer.DecrementTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }


        //enemy team effect actions
        teamEffectStrings = currEnemy.DoTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            enemyHud.UpdateHUD(currEnemy);
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT
        yield return CheckHP();

        //enemy team effect decrements
        teamEffectStrings = currEnemy.DecrementTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }
    }
    IEnumerator HandleTrapOperations()
    {
        List<string> teamEffectStrings;

        //player trap decrements
        teamEffectStrings = currPlayer.DecrementTraps();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }


        //enemy trap decrements
        teamEffectStrings = currEnemy.DecrementTraps();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }
    }
    IEnumerator HandleFieldEffectOperations()
    {
        List<string> fieldEffectStrings;

        //player field effect actions
        fieldEffectStrings = currPlayer.DoFieldEffects();
        if (fieldEffectStrings.Count > 0)
        {
            playerHud.UpdateHUD(currPlayer);
            foreach (string text in fieldEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT
        yield return CheckHP();


        //enemy field effect actions
        fieldEffectStrings = currEnemy.DoFieldEffects();
        if (fieldEffectStrings.Count > 0)
        {
            enemyHud.UpdateHUD(currEnemy);
            foreach (string text in fieldEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT
        yield return CheckHP();


        //player field effect decrements, then enemy (ONLY PRINT DIALOG ONCE, FROM PLAYER)
        fieldEffectStrings = currPlayer.DecrementFieldEffects();
        if (fieldEffectStrings.Count > 0)
        {
            foreach (string text in fieldEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(textDelay);
            }
        }
        currEnemy.DecrementFieldEffects();
    }
    IEnumerator CheckHP()
    {
        ///Checks HP of both currPlayer and currEnemy, forcing swap if at 0HP and
        /// other teammates are > 0HP

        //track who needs to swap, then swap at same time (no bias)
        bool enemySwap = false;
        bool playerSwap = false;

        //check enemy first so if player and enemy both lose at same time, player still wins
        if (currEnemy.HP == 0)
        {
            yield return dialogBox.DialogSet($"Enemy's {currEnemy.Name} was slain!");
            yield return new WaitForSeconds(textDelay);

            currEnemy.WasSlain = true;
            yield return HandleSlainOperations(currEnemy, currPlayer);

            if (GetRemaining(playerTeam: false) > 0)
            {
                BattleAI.AIContextObject aiContext = new(difficulty, enemyChars, currEnemy, currPlayer);
                enemySwapIndex = battleAI.ChooseSwapChar(aiContext, GetCurrBattleCharIndex(playerTeam: false));
                enemySwap = true;
            }
            else
            {
                //END BATTLE
                yield return dialogBox.DialogSet("Player win.");
                yield return new WaitForSeconds(textDelay);
                yield return EndBattle(playerWin: true);
            }
        }

        if (currPlayer.HP == 0)
        {
            yield return dialogBox.DialogSet($"Player's {currPlayer.Name} was slain!");
            yield return new WaitForSeconds(textDelay);

            currPlayer.WasSlain = true;
            yield return HandleSlainOperations(currPlayer, currEnemy);

            if (GetRemaining(playerTeam: true) > 0)
            {
                state = BattleState.WaitingChoice;
                partyMenu.ShowPartyMenu(showBackButton: false);
                yield return new WaitUntil(() => state != BattleState.WaitingChoice);
                playerSwap = true;
            }
            else
            {
                //END BATTLE
                yield return dialogBox.DialogSet("Player lose.");
                yield return new WaitForSeconds(textDelay);
                yield return EndBattle(playerWin: false);
            }
        }

        //perform actual swaps after choices are made
        if (enemySwap)
        {
            yield return PerformSwap(playerTeam: false);
            //ENEMY SNAPSHOT UPDATE HERE
        }
        if (playerSwap)
        {
            yield return PerformSwap(playerTeam: true);
            //PLAYER SNAPSHOT UPDATE HERE
        }
    }
    IEnumerator HandleSlainOperations(BattleChar slain, BattleChar alive)
    {
        //GraspAbility used by slain or alive
        if (slain.UsedAbility is GraspAbility || alive.UsedAbility is GraspAbility)
        {
            //IF slain was user; ELSE alive was user
            if (slain.UsedAbility is GraspAbility && slain.MultiTurnAbility > 0)
            {
                //reset only if Trapped duration was not initially longer than when set
                if (alive.Trapped <= slain.MultiTurnAbility)
                {
                    alive.Trapped = 0;
                }
            }
            else if (alive.UsedAbility is GraspAbility && alive.MultiTurnAbility > 0)
            {
                //must end MultiTurnAbility because there is no enemy to 'grasp' anymore
                alive.MultiTurnAbility = 0;
            }
        }

        yield break;
    }
    



    //NewTurn
    /// <summary>
    /// Does final operations just before new turn like Energy regeneration, turn effect operations, etc.
    /// </summary>
    /// <returns></returns>
    IEnumerator NewTurnOperations()
    {
        RegenerateEnergy();
        HandleTurnEffects();

        if (currPlayer.MultiTurnAbility > 0)
        {
            currPlayer.MultiTurnAbility--;
        }
        if (currEnemy.MultiTurnAbility > 0)
        {
            currEnemy.MultiTurnAbility--;
        }

        playerHud.UpdateHUD(currPlayer);
        enemyHud.UpdateHUD(currEnemy);
        dialogBox.SetAbilityButtons(currPlayer);

        //BOTH SNAPSHOTS FINAL UPDATES HERE

        yield return dialogBox.DialogSet("Starting new turn...");
        yield return new WaitForSeconds(2f);

        turnNumber++;
    }

    /// <summary>
    /// Regenerate Energy of all chars at > 0HP, less for active vs inactive
    /// </summary>
    void RegenerateEnergy()
    {
        ///Regenerate Energy of all BattleChars on each team that are > 0HP

        foreach (BattleChar battleChar in playerChars)
        {
            if (battleChar == currPlayer)
            {
                //regenerate 25% if Pass, else 10% (Mathf.Max so can't regenerate 0)
                if (playerChoice == BattleChoice.Pass)
                {
                    battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 3.99f), 1);
                }
                else
                {
                    battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 10.0f), 1);
                }

                //verify not greater than max
                if (battleChar.Energy > battleChar.MaxEnergy)
                {
                    battleChar.Energy = battleChar.MaxEnergy;
                }
            }
            else
            {
                battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 3.0f), 1);

                //if now one less than max or greater than max, set to max (1/3 calculation errors)
                if (battleChar.MaxEnergy - battleChar.Energy == 1 || battleChar.Energy > battleChar.MaxEnergy)
                {
                    battleChar.Energy = battleChar.MaxEnergy;
                }
            }
        }

        foreach (BattleChar battleChar in enemyChars)
        {
            if (battleChar == currEnemy)
            {
                if (enemyChoice == BattleChoice.Pass)
                {
                    battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 4.0f), 1);
                }
                else
                {
                    battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 10.0f), 1);
                }

                if (battleChar.Energy > battleChar.MaxEnergy)
                {
                    battleChar.Energy = battleChar.MaxEnergy;
                }
            }
            else
            {
                battleChar.Energy += Mathf.Max((int)Mathf.Round(battleChar.MaxEnergy / 3.0f), 1);

                //if now one less than max or greater than max, set to max (1/3 calculation errors)
                if (battleChar.MaxEnergy - battleChar.Energy == 1 || battleChar.Energy > battleChar.MaxEnergy)
                {
                    battleChar.Energy = battleChar.MaxEnergy;
                }
            }
        }
    }

    /// <summary>
    /// Reset single-turn effects and decrement multi-turn effects
    /// </summary>
    void HandleTurnEffects()
    {
        currPlayer.ResetSingleTurnEffects();
        currEnemy.ResetSingleTurnEffects();

        currPlayer.DecrementMultiTurnEffects();
        currEnemy.DecrementMultiTurnEffects();
    }
    



    //BattleEnded
    IEnumerator EndBattle(bool playerWin)
    {
        yield return HandleXPOperations();
        HandleEvolutionChecks();

        yield return dialogBox.DialogSet("Ending battle...");
        yield return new WaitForSeconds(textDelay);

        //hide this gameObject (entire BattleSystem) then stop all coroutines
        gameObject.SetActive(false);
        ResetAllBattleData();
        GameState.InBattle = false;
        StopAllCoroutines();
    }
    IEnumerator HandleXPOperations()
    {
        int[] playerTeamXP = new int[playerChars.Length];
        int participants = 0;

        //count number of player chars that participated and < level 100
        foreach (BattleChar player in playerChars)
        {
            participants += (player.WasActive && player.Level < 100) ? 1 : 0;
        }

        //calculate XP yield of any slain enemy character and add to array
        foreach (BattleChar enemy in enemyChars)
        {
            if (enemy.HP == 0)
            {
                //calculate specific XP yield for this playerchar
                for (int i = 0; i < playerChars.Length; i++)
                {
                    playerTeamXP[i] = CalculateXPYield(enemy, playerChars[i], participants);
                }
            }
        }

        //add XP to each player that was active, handling any level ups and learning Abilities
        for (int i = 0; i < playerChars.Length; i++)
        {
            if (!playerChars[i].WasActive)
            {
                continue;
            }

            BattleChar player = playerChars[i];
            player.AddXP(playerTeamXP[i]);

            while (player.XP >= XPTable.GetXpToNextLevel(player.Level, player.SpeciesData.XPRatio))
            {
                yield return HandleLevelUp(player);
            }
        }
    }
    int CalculateXPYield(BattleChar enemy, BattleChar player, int participants)
    {
        //calculate xp ratio, is 5% per level difference (absolute value, then add 1)
        float ratio = Mathf.Abs((enemy.Level - player.Level) / 20.0f) + 1;

        //if enemy is >= player, multiply by ratio, else divide (base yield 10 per level)
        float xp = (enemy.Level >= player.Level)
            ? (enemy.Level * 10) * ratio : (enemy.Level * 10) / ratio;

        //increase yield by flat 25% per evolution stage above 1
        if (enemy.SpeciesData.EvolutionStage > 1)
        {
            xp *= (enemy.SpeciesData.EvolutionStage == 2) ? 1.25f : 1.5f;
        }

        //increase by another 50% if NOT Wild, and 100% if Boss
        if (difficulty != BattleAI.AIDifficulty.Wild)
        {
            if (difficulty != BattleAI.AIDifficulty.Boss)
            {
                xp *= 1.50f;
            }
            else
            {
                xp *= 2.00f;
            }
        }

        //return xp value, divided by number of participants then rounded to int
        return Mathf.RoundToInt(xp / participants);
    }
    IEnumerator HandleLevelUp(BattleChar player)
    {
        yield return dialogBox.DialogSet($"{player.Name} leveled up to {player.Level + 1}!");
        yield return new WaitForSeconds(textDelay);

        //if LevelUp returns not empty, then an Ability is trying to be learned
        string abilityName = player.LevelUp();
        if (abilityName != "")
        {
            Ability newAbility;
            try
            {
                newAbility = Activator.CreateInstance(Type.GetType(abilityName)) as Ability;

                //if successful, add newAbility's class name to player's PastAbilities list
                if (!player.PastAbilities.Contains(newAbility.GetType().Name))
                {
                    player.PastAbilities.Add(newAbility.GetType().Name);
                }
            }
            catch
            {
                newAbility = new BadAbility();
            }

            //search for first available index, if any
            int availableIndex = -1;
            for (int i = 0; i < player.Abilities.Length; i++)
            {
                if (player.Abilities[i] is EmptyAbility)
                {
                    availableIndex = i;
                    break;
                }
            }

            //if valid index, can insert new Ability in new location
            if (availableIndex != -1)
            {
                player.Abilities[availableIndex] = newAbility;
                yield return dialogBox.DialogSet($"{player.Name} learned {player.Abilities[availableIndex].Name}!");
                yield return new WaitForSeconds(textDelay);
                yield break;
            }

            //handle teaching ability, waiting until PartyMenu hidden to move on
            yield return dialogBox.DialogSet($"{player.Name} is trying to learn an Ability...");
            yield return new WaitForSeconds(textDelay);

            partyMenu.BeginTeachAbility(newAbility, Array.IndexOf(playerChars, player));
            yield return new WaitUntil(() => !partyMenu.gameObject.activeSelf);
        }
    }
    void HandleEvolutionChecks()
    {
        foreach (BattleChar player in playerChars)
        {
            player.CheckEvolutionConditions(enemyChars);
        }
    }
    void ResetAllBattleData()
    {
        turnNumber = 0;
        playerSwapIndex = -1;
        enemySwapIndex = -1;
        state = BattleState.Start;
        playerChoice = BattleChoice.Attack;
        enemyChoice = BattleChoice.Attack;

        //reset all player BattleChar data and nullify enemyChars to free memory
        currPlayer = null;
        currEnemy = null;
        foreach (BattleChar player in playerChars)
        {
            player.ResetEndBattle();
        }
        enemyChars = null;

        //deselect all buttons so player cannot interact with BattleSystem when inactive
        EventSystem.current.SetSelectedGameObject(null);
    }




    //button presses
    public void OnAbilityButtonPress(int abilityIndex)
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }

        playerChoice = BattleChoice.Attack;
        currPlayer.UsedAbility = currPlayer.Abilities[abilityIndex];

        dialogBox.ShowAbilityButtons(false);

        state = BattleState.Preparing;
    }
    public void OnPassButtonPress()
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }

        playerChoice = BattleChoice.Pass;

        state = BattleState.Preparing;
    }
    public void OnSwapButtonPress(int swapIndex)
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }

        playerChoice = BattleChoice.Swap;
        playerSwapIndex = swapIndex;

        partyMenu.HidePartyMenu();

        state = BattleState.Preparing;
    }
    public void OnFleeForfeitButtonPress()
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }

        dialogBox.ShowFleeForfeitOverlay(false);
        playerChoice = BattleChoice.FleeForfeit;
        state = BattleState.Preparing;
    }
    public void OnPartyButtonPress()
    {
        partyMenu.LoadPartyChars(GetCurrBattleCharIndex(playerTeam: true));
        partyMenu.ShowPartyMenu();
        partyMenu.FocusPartyMenu();
    }
    public void HidePartyMenuInBattle()
    {
        dialogBox.FocusMainPartyButton();
    }




    //simple getters
    public int GetCurrBattleCharIndex(bool playerTeam)
    {
        if (playerTeam)
        {
            return Array.IndexOf(playerChars, currPlayer);
        }
        else
        {
            return Array.IndexOf(enemyChars, currEnemy);
        }
    }
    public int GetRemaining(bool playerTeam)
    {
        BattleChar[] team = (playerTeam) ? playerChars : enemyChars;

        int charsAlive = 0;
        foreach (BattleChar battleChar in team)
        {
            if (battleChar.HP > 0)
            {
                charsAlive++;
            }
        }
        return charsAlive;
    }
}
