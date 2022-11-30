using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleSystem : MonoBehaviour
{
    enum BattleState { Start, WaitingChoice, PlayerAction, EnemyAction, EndingTurn, NewTurn, Busy }
    enum BattleChoice { Attack, Pass, Swap, FleeForfeit }

    [SerializeField] PlayerHud playerHud;
    [SerializeField] EnemyHud enemyHud;
    [SerializeField] DialogBox dialogBox;
    [SerializeField] Image playerImage;
    [SerializeField] Image enemyImage;
    [SerializeField] SpeciesData playerSpecies;
    [SerializeField] SpeciesData enemySpecies;

    BattleChar[] playerChars;
    BattleChar[] enemyChars;
    BattleChar currPlayer;
    BattleChar currEnemy;

    BattleState state;
    BattleChoice playerChoice;
    BattleChoice enemyChoice;

    //getter for playerChars
    public BattleChar[] PlayerChars { get { return playerChars; } }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Setup());
    }
    IEnumerator Setup()
    {
        //TEMP add a single BattleChar to playerChars and enemyChars
        playerChars = new BattleChar[2] { new BattleChar(playerSpecies), new BattleChar(playerSpecies, "SECOND ONE") };
        enemyChars = new BattleChar[1] { new BattleChar(enemySpecies) };
        currPlayer = playerChars[0];
        currEnemy = enemyChars[0];

        //load player and enemy sprites from BattleChar's species data
        playerImage.sprite = currPlayer.SpeciesData.BackSprite;
        enemyImage.sprite = currEnemy.SpeciesData.BackSprite;

        //set hud for each
        playerHud.SetHUD(currPlayer);
        enemyHud.SetHUD(currEnemy);
        dialogBox.Setup(this);
        dialogBox.SetAbilityInfo(currPlayer);

        //TEMP
        yield return dialogBox.DialogSet("TEST TEXT TEST TEXT TEST TEXT TEST TEXT TEST TEXT ");
        yield return new WaitForSeconds(1f);
        yield return dialogBox.DialogAppend("Test append.");
        yield return new WaitForSeconds(1f);
        //TEMP

        StartCoroutine(Loop());
    }


    IEnumerator Loop()
    {
        WaitPlayerChoice();

        yield return new WaitUntil(() => state == BattleState.NewTurn);

        //enemy AI make choice and set enemyChoice variable

        //check if player flee/forfeit

        //check swaps, performing any chosen swaps by player or enemy

        //check status effect of either currPlayer or currEnemy ending early

        //perform action, checking playerChoice and enemyChoice
        //  if both attack, check HP of both characters after FIRST ATTACK completes
        //      if either at 0HP, end attacking and handle 0HP (force swap of either/both
        //      or end battle)

        //perform end-of-turn operations like status effects, team effects, etc.

        //perform new turn operations (increment turn, take snapshot, etc.)

        yield break;
    }
    void WaitPlayerChoice()
    {
        StartCoroutine(dialogBox.DialogSet("Player choice..."));
        state = BattleState.WaitingChoice;
        dialogBox.ShowMainButtons(true);
    }


    //button presses
    public void OnAbilityButtonPress(int abilityIndex)
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }
        playerChoice = BattleChoice.Attack;
    }
    public void OnPassButtonPress()
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }
        playerChoice = BattleChoice.Pass;

    }
    public void OnSwapButtonPress(int swapIndex)
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }
        playerChoice = BattleChoice.Swap;

    }
    public void OnFleeForfeitButtonPress()
    {
        if (state != BattleState.WaitingChoice)
        {
            return;
        }
        playerChoice = BattleChoice.FleeForfeit;

    }

}
