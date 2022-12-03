using System;
using System.Collections;
using System.Collections.Generic;
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
        yield return new WaitForSeconds(1f);
        yield return dialogBox.DialogAppend("Test append.");
        yield return new WaitForSeconds(1f);
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
            yield return new WaitForSeconds(1f);

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

            //perform action, checking playerChoice and enemyChoice
            //  if both attack, check HP of both characters after FIRST ATTACK completes
            //      if either at 0HP, end attacking and handle 0HP (force swap of either/both
            //      or end battle)
            currPlayer.TakeDamage(5);
            playerHud.UpdateHUD(currPlayer);
            yield return new WaitForSeconds(1f);

            currEnemy.TakeDamage(5);
            enemyHud.UpdateHUD(currEnemy);
            yield return new WaitForSeconds(1f);

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
        //TEMP
        currEnemy.UsedAbility = currEnemy.Abilities[0];
        enemyChoice = BattleChoice.Attack;
        //TEMP
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
            yield return new WaitForSeconds(1f);

            //if any traps hit, update HUD and print dialog returned from hitting traps
            List<string> trapsHit = currPlayer.DoTrapsHit();
            if (trapsHit.Count > 0)
            {
                playerHud.UpdateHUD(currPlayer);
                foreach (string text in trapsHit)
                {
                    yield return dialogBox.DialogSet(text);
                    yield return new WaitForSeconds(1f);
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
            yield return new WaitForSeconds(1f);

            List<string> trapsHit = currEnemy.DoTrapsHit();
            if (trapsHit.Count > 0)
            {
                enemyHud.UpdateHUD(currEnemy);
                foreach (string text in trapsHit)
                {
                    yield return dialogBox.DialogSet(text);
                    yield return new WaitForSeconds(1f);
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
            yield return new WaitForSeconds(1f);
        }

        temp = currEnemy.CheckEffectEndEarly();
        if (temp != "")
        {
            enemyHud.SetHUD(currEnemy);
            yield return dialogBox.DialogSet(temp);
            yield return new WaitForSeconds(1f);
        }
    }

    //determine attack order method
    //actual attack method
    //attack after-effects method

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
            yield return new WaitForSeconds(1f);
        }

        //if currPlayer now at 0HP, don't check Burned status effect ending
        if (currPlayer.HP != 0)
        {
            string temp = currPlayer.DecrementStatusEffect();
            if (temp != "")
            {
                yield return dialogBox.DialogSet(temp);
                yield return new WaitForSeconds(1f);
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


        //active enemy BattleChar will print to dialog
        if (currEnemy.Burned > 0)
        {
            yield return dialogBox.DialogSet(currEnemy.DoBurnedDamage());
            yield return new WaitForSeconds(1f);
        }

        //if currEnemy now at 0HP, don't check Burned status effect ending
        if (currEnemy.HP != 0)
        {
            string temp = currEnemy.DecrementStatusEffect();
            if (temp != "")
            {
                yield return dialogBox.DialogSet(temp);
                yield return new WaitForSeconds(1f);
            }
        }

        //all inactive enemy BattleChars will silently decrement (no Burned damage)
        foreach (BattleChar battleChar in enemyChars)
        {
            if (battleChar.HP == 0 || battleChar == currEnemy) { continue; }
            battleChar.DecrementStatusEffect();
        }

        //CHECK HP HERE AND DO SWAP IF NECESSARY
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
                yield return new WaitForSeconds(1f);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT

        //player team effect decrements
        teamEffectStrings = currPlayer.DecrementTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(1f);
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
                yield return new WaitForSeconds(1f);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT

        //enemy team effect decrements
        teamEffectStrings = currEnemy.DecrementTeamEffects();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(1f);
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
                yield return new WaitForSeconds(1f);
            }
        }


        //enemy trap decrements
        teamEffectStrings = currEnemy.DecrementTraps();
        if (teamEffectStrings.Count > 0)
        {
            foreach (string text in teamEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(1f);
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
                yield return new WaitForSeconds(1f);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT


        //enemy field effect actions
        fieldEffectStrings = currEnemy.DoFieldEffects();
        if (fieldEffectStrings.Count > 0)
        {
            enemyHud.UpdateHUD(currEnemy);
            foreach (string text in fieldEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(1f);
            }
        }

        //CHECK HP FOR ANY DAMAGE DEALT BEFORE DECREMENT


        //player field effect decrements, then enemy (ONLY PLAYER PRINTS DIALOG)
        fieldEffectStrings = currPlayer.DecrementFieldEffects();
        if (fieldEffectStrings.Count > 0)
        {
            foreach (string text in fieldEffectStrings)
            {
                yield return dialogBox.DialogSet(text);
                yield return new WaitForSeconds(1f);
            }
        }
        currEnemy.DecrementFieldEffects();
    }

    IEnumerator CheckHP()
    {
        yield break;
    }
    
    IEnumerator NewTurnOperations()
    {
        yield return dialogBox.DialogSet("Starting new turn...");
        yield return new WaitForSeconds(2f);
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
