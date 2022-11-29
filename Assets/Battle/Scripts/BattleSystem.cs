using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleSystem : MonoBehaviour
{
    enum BattleState { Start, WaitingChoice, PlayerAction, EnemyAction, EndingTurn, NewTurn, Busy }

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

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Setup());
    }
    IEnumerator Setup()
    {
        //TEMP add a single BattleChar to playerChars and enemyChars
        playerChars = new BattleChar[1] { new BattleChar(playerSpecies) };
        enemyChars = new BattleChar[1] { new BattleChar(enemySpecies) };
        currPlayer = playerChars[0];
        currEnemy = enemyChars[0];

        //load player and enemy sprites from BattleChar's species data
        playerImage.sprite = currPlayer.SpeciesData.BackSprite;
        enemyImage.sprite = currEnemy.SpeciesData.BackSprite;

        //set hud for each
        playerHud.SetHUD(currPlayer);
        enemyHud.SetHUD(currEnemy);
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

        yield break;
    }
    void WaitPlayerChoice()
    {
        StartCoroutine(dialogBox.DialogSet("Player choice..."));
        state = BattleState.WaitingChoice;
        dialogBox.ShowMainButtons(true);
    }


}
