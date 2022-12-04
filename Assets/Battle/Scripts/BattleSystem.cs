using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BattleSystem : MonoBehaviour
{
    enum BattleState { Start, WaitingChoice, Preparing, Attacking, EndingTurn, NewTurn, BattleEnded, Busy }
    enum BattleChoice { Attack, Pass, Swap, FleeForfeit }

    [SerializeField] PlayerHud playerHud;
    [SerializeField] EnemyHud enemyHud;
    [SerializeField] DialogBox dialogBox;
    [SerializeField] Image playerImage;
    [SerializeField] Image enemyImage;
    [SerializeField] SpeciesData playerSpecies;
    [SerializeField] SpeciesData enemySpecies;

    [SerializeField] BattleParty playerParty;
    [SerializeField] BattleParty enemyParty;

    BattleChar[] playerChars;
    BattleChar[] enemyChars;
    BattleChar currPlayer;
    BattleChar currEnemy;
    BattleAI battleAI;
    AIDifficulty difficulty;
    float textDelay = 1.5f;

    int turnNumber;
    BattleState state;
    BattleChoice playerChoice;
    BattleChoice enemyChoice;
    int playerSwapIndex;
    int enemySwapIndex;

    //getter for playerChars
    public BattleChar[] PlayerChars { get { return playerChars; } }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Setup());
    }
    IEnumerator Setup()
    {
        InitPlayerParty();
        InitEnemyParty();
        currPlayer = playerChars[0];
        currEnemy = enemyChars[0];

        //instantiate BattleAI object (WILL ACCESS DIFFICULTY SETTING LATER)
        difficulty = AIDifficulty.Easy;
        battleAI = new(this, difficulty);

        //load player and enemy sprites from BattleChar's species data
        playerImage.sprite = currPlayer.SpeciesData.BackSprite;
        enemyImage.sprite = currEnemy.SpeciesData.FrontSprite;

        //set hud for each
        playerHud.SetHUD(currPlayer);
        enemyHud.SetHUD(currEnemy);
        dialogBox.Setup(this);
        dialogBox.SetAbilityButtons(currPlayer);

        //TEMP
        yield return dialogBox.DialogSet("TEST TEXT TEST TEXT TEST TEXT TEST TEXT TEST TEXT ");
        yield return new WaitForSeconds(textDelay);
        yield return dialogBox.DialogAppend("Test append.");
        yield return new WaitForSeconds(textDelay);

        currEnemy.Energy = 0;

        //TEMP

        StartCoroutine(Loop());
    }
    void InitPlayerParty()
    {
        playerChars = new BattleChar[playerParty.Team.Length];
        for (int i = 0; i < playerParty.Team.Length; i++)
        {
            playerChars[i] = new BattleChar(playerParty.Team[i], difficulty, playerTeam: true);
        }

        //find first BattleChar in array with >0HP to make currPlayer
        foreach (BattleChar battleChar in playerChars)
        {
            if (battleChar.HP > 0) { currPlayer = battleChar; }
        }
    }
    void InitEnemyParty()
    {
        enemyChars = new BattleChar[enemyParty.Team.Length];
        for (int i = 0; i < enemyParty.Team.Length; i++)
        {
            enemyChars[i] = new BattleChar(enemyParty.Team[i], difficulty, playerTeam: false);
        }

        //find first BattleChar in array with >0HP to make currPlayer
        foreach (BattleChar battleChar in enemyChars)
        {
            if (battleChar.HP > 0) { currEnemy = battleChar; }
        }
    }


    IEnumerator Loop()
    {
        while (state != BattleState.BattleEnded)
        {
            //enable player input and wait until input gotten
            yield return EnablePlayerAction();
            yield return new WaitUntil(() => state != BattleState.WaitingChoice);
            dialogBox.ShowMainButtons(false);
            yield return dialogBox.DialogSet("Preparing...");
            yield return new WaitForSeconds(textDelay);

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

            //check if player flee/forfeit
            if (playerChoice == BattleChoice.FleeForfeit)
            {
                //CALL END BATTLE METHOD
            }

            //check swaps, performing any chosen swaps by player or enemy and resetting swapIndex for either/both
            yield return CheckSwaps();

            //check status effect of either currPlayer or currEnemy ending early
            yield return CheckStatusEndEarly();

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
    IEnumerator EnablePlayerAction()
    {
        yield return dialogBox.DialogSet("Player choice...");
        state = BattleState.WaitingChoice;
        dialogBox.ShowMainButtons(true);
        dialogBox.SelectAttackButton();
    }
    void GetEnemyChoice()
    {
        //get chosen Ability and assign to temp variable, then find max Score of all
        Ability tempAbility = battleAI.ChooseAbility(currEnemy, currPlayer);
        int maxScore = currEnemy.Abilities.Max(x => x.Score);

        //if null, then no Ability was chosen (must Swap or Pass)
        if (tempAbility == null)
        {
            //if should swap, set swap index and choose Swap; else Pass this turn
            if (battleAI.CheckShouldSwap(currEnemy, currPlayer, maxScore))
            {
                enemySwapIndex = battleAI.ChooseSwapChar(enemyChars, currPlayer);
                enemyChoice = BattleChoice.Swap;
            }
            else
            {
                enemyChoice = BattleChoice.Pass;
            }
        }
        else
        {
            //if should swap, set swap index and choose Swap; else use Ability
            if (battleAI.CheckShouldSwap(currEnemy, currPlayer, maxScore))
            {
                enemySwapIndex = battleAI.ChooseSwapChar(enemyChars, currPlayer);
                enemyChoice = BattleChoice.Swap;
            }
            else
            {
                currEnemy.UsedAbility = tempAbility;
                enemyChoice = BattleChoice.Attack;
            }
        }
    }

    IEnumerator CheckSwaps()
    {
        if (playerChoice == BattleChoice.Swap)
        {
            yield return PerformSwap(playerTeam: true);
        }
        if (enemyChoice == BattleChoice.Swap)
        {
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
        if (user.Stunned > 0)
        {
            user.Delaying = false;
            user.Recharging = false;
            yield return dialogBox.DialogSet(user.Name + " is Stunned!");
            yield return new WaitForSeconds(textDelay);
            yield break;
        }

        //if user Frozen, 33% chance to be unable to attack this turn (does same as Stunned)
        if (user.Frozen > 0)
        {
            if (UnityEngine.Random.Range(0, 100) < 33)
            {
                user.Delaying = false;
                user.Recharging = false;
                yield return dialogBox.DialogSet(user.Name + " is too cold to move!");
                yield return new WaitForSeconds(textDelay);
                yield break;
            }
        }

        //if user currently Recharging, do not attack
        if (user.Recharging)
        {
            user.Recharging = false;
            yield return dialogBox.DialogSet(user.Name + " is recharging!");
            yield return new WaitForSeconds(textDelay);
            yield break;
        }

        //if user's UsedAbility is Delayed, check if currently waiting or starting delay
        if (user.UsedAbility.Delayed)
        {
            //if user is currently waiting, use Ability this turn; else begin actual delay
            if (user.Delaying)
            {
                user.Delaying = false;
            }
            else
            {
                user.Delaying = true;
                yield return dialogBox.DialogSet(user.Name + " is preparing to use "
                    + user.UsedAbility.Name + " next turn!");
                yield return new WaitForSeconds(textDelay);
                yield break;
            }
        }

        //if user not currently Recharging and is using Recharge move, recharge next turn
        if (user.UsedAbility.Recharge)
        {
            user.Recharging = true;
        }
        #endregion

        //create AbilityData object to pass to Ability's UseAbility method
        AbilityData data = new(user, target, turnNumber, dialogBox);

        //update dialog with use text, then consume energy and update HUD for both
        yield return dialogBox.DialogSet(user.Name + " used " + user.UsedAbility.Name + "!");
        user.Energy -= user.UsedAbility.EnergyCost(user);
        playerHud.SetHUD(currPlayer);
        enemyHud.SetHUD(currEnemy);

        //if hit target, do attack and after effects; else acknowledge miss
        if (user.UsedAbility.CheckAccuracy(user, target))
        {
            //PLAY HIT ANIMATION

            yield return user.UsedAbility.UseAbility(data);
            yield return new WaitForSeconds(textDelay);

            yield return DoAfterEffects(data);
        }
        else
        {
            //PLAY MISS ANIMATION

            yield return dialogBox.DialogSet("The attack missed!");
            yield return new WaitForSeconds(textDelay);
        }
    }
    IEnumerator DoAfterEffects(AbilityData data)
    {


        yield break;
    }

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
        if (currPlayer.Burned > 0)
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
        if (currEnemy.Burned > 0)
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


        //player field effect decrements, then enemy (ONLY PLAYER PRINTS DIALOG)
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
            if (GetRemaining(playerTeam: false) > 0)
            {
                enemySwapIndex = battleAI.ChooseSwapChar(enemyChars, currPlayer);
                enemySwap = true;
            }
            else
            {
                //END BATTLE
                Debug.Log("Player win.");
                StopAllCoroutines();
            }
        }

        if (currPlayer.HP == 0)
        {
            if (GetRemaining(playerTeam: true) > 0)
            {
                state = BattleState.WaitingChoice;
                dialogBox.ShowPartyMenu(enabled, hideBackButton: true);
                yield return new WaitUntil(() => state != BattleState.WaitingChoice);
                playerSwap = true;
            }
            else
            {
                //END BATTLE
                Debug.Log("Player lose.");
                StopAllCoroutines();
            }
        }

        //perform actual swaps after choices are made
        if (enemySwap)
        {
            yield return PerformSwap(playerTeam: false);
        }
        if (playerSwap)
        {
            yield return PerformSwap(playerTeam: true);
        }
    }
    
    IEnumerator NewTurnOperations()
    {
        RegenerateEnergy();
        playerHud.UpdateHUD(currPlayer);
        enemyHud.UpdateHUD(currEnemy);

        yield return dialogBox.DialogSet("Starting new turn...");
        yield return new WaitForSeconds(2f);
    }
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

        dialogBox.ShowPartyMenu(false);

        state = BattleState.Preparing;
    }
    public void OnFleeForfeitButtonPress()
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }
        playerChoice = BattleChoice.FleeForfeit;

        state = BattleState.Preparing;
    }

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
