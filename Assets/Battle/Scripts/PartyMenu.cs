using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartyMenu : MonoBehaviour
{
    [SerializeField] PartyChar[] partyChars;
    [SerializeField] Button[] charButtons;
    [SerializeField] Button backButton;

    [Header("Details")]
    [SerializeField] GameObject detailsPanel;
    [SerializeField] Button detailsSwapButton;
    [SerializeField] Button detailsBackButton;
    [SerializeField] TMP_Text detailsName;
    [SerializeField] TMP_Text specUp;
    [SerializeField] TMP_Text specDown;
    

    public Button[] CharButtons { get { return charButtons; } }
    public Button BackButton { get { return backButton; } }

    public bool DetailsFocused { get; private set; }
    public int CurrentIndex { get; private set; }

    BattleChar[] playerChars;
    int currPlayerIndex;

    /// <summary>
    /// Sets the local reference to the playerChars array
    /// </summary>
    /// <param name="battleChars">Reference to array of BattleChars</param>
    public void SetPlayerCharsReference(BattleChar[] battleChars)
    {
        playerChars = battleChars;
    }

    /// <summary>
    /// Loads BattleChar info into party buttons on right side, also sets currPlayerIndex from argument
    /// </summary>
    /// <param name="currPlayerIndex">Index of currPlayer in playerChars[]</param>
    public void LoadPartyChars(int currPlayerIndex = -1)
    {
        for (int i = 0; i < partyChars.Length; i++)
        {
            //if index is valid in playerChars[]
            if (i < playerChars.Length)
            {
                partyChars[i].Name.text = playerChars[i].Name;
            }
            //else not valid BattleChar, so hide button data and make non-interactable
            else
            {
                partyChars[i].Name.text = "EMPTY";
                charButtons[i].interactable = false;
            }
        }
        this.currPlayerIndex = currPlayerIndex;
    }

    /// <summary>
    /// Shows details of the selected party button's corresponding BattleChar
    /// </summary>
    /// <param name="index">Index of selected BattleChar</param>
    public void ShowDetails(int index)
    {
        //store index of selected button for use when back button pressed
        CurrentIndex = index;

        //SHOW DATA
        detailsName.text = playerChars[index].Name;
        specUp.text = "Spec up: " + playerChars[index].SpecialtyUp.ToString();
        specDown.text = "Spec down: " + playerChars[index].SpecialtyDown.ToString();

        detailsPanel.SetActive(true);
    }

    /// <summary>
    /// Hides details panel, intended only for when PartyMenu's back button is selected
    /// </summary>
    public void HideDetails()
    {
        CurrentIndex = 0;
        detailsPanel.SetActive(false);
    }

    /// <summary>
    /// Focuses on the details panel, showing local swap button if valid BattleSystem
    /// </summary>
    public void FocusDetails()
    {
        DetailsFocused = true;
        detailsBackButton.gameObject.SetActive(true);

        EnableOtherButtons(false);

        //if currPlayerIndex is -1, then there is no active BattleSystem
        if (currPlayerIndex == -1)
        {
            //disable swap button and auto-select back button
            detailsSwapButton.gameObject.SetActive(false);
            detailsBackButton.Select();
        }
        else
        {
            detailsSwapButton.gameObject.SetActive(true);

            //make swap button non-interactable if is current BattleChar
            if (currPlayerIndex == CurrentIndex)
            {
                detailsSwapButton.interactable = false;
                detailsBackButton.Select();
            }
            else
            {
                detailsSwapButton.interactable = true;
                detailsSwapButton.Select();
            }
        }
        
    }

    /// <summary>
    /// Takes BattleSystem reference and calls its OnSwapButtonPress method
    /// </summary>
    /// <param name="battleSystem">Reference to active BattleSystem (in-battle only)</param>
    public void OnSwapButtonPressed(BattleSystem battleSystem)
    {
        //call battleSystem's OnSwapButtonPressed method with currentIndex
        battleSystem.OnSwapButtonPress(CurrentIndex);
    }

    /// <summary>
    /// De-focuses details panel and hides local swap and back buttons
    /// </summary>
    public void DetailsBackButtonPress()
    {
        EnableOtherButtons(true);

        //select button from current details panel character
        charButtons[CurrentIndex].Select();
        DetailsFocused = false;

        detailsSwapButton.gameObject.SetActive(false);
        detailsBackButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Enables or disables interaction with party buttons and main back button
    /// </summary>
    /// <param name="enable">Bool for whether to enable or disable button interaction</param>
    void EnableOtherButtons(bool enable)
    {
        //only enable charButtons that correspond to an actual playerChar
        for (int i = 0; i < playerChars.Length; i++)
        {
            charButtons[i].interactable = enable;
        }
        backButton.interactable = enable;
    }
}
