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

    [SerializeField] GameObject detailsPanel;
    [SerializeField] Button detailsSwapButton;
    [SerializeField] Button detailsBackButton;
    [SerializeField] TMP_Text detailsName;
    

    public Button[] CharButtons { get { return charButtons; } }
    public Button BackButton { get { return backButton; } }

    public bool DetailsFocused { get; private set; }
    public int CurrentIndex { get; private set; }

    BattleChar[] playerChars;

    /// <summary>
    /// Sets the local reference to the playerChars array
    /// </summary>
    /// <param name="battleChars">Reference to array of BattleChars</param>
    public void SetBattleCharsReference(BattleChar[] battleChars)
    {
        playerChars = battleChars;
    }

    /// <summary>
    /// Loads BattleChar info into party buttons on right side
    /// </summary>
    public void LoadPartyChars()
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
    /// Focuses on the details panel, showing local swap and back buttons
    /// </summary>
    public void FocusDetails()
    {
        DetailsFocused = true;

        detailsSwapButton.gameObject.SetActive(true);
        detailsBackButton.gameObject.SetActive(true);

        detailsSwapButton.Select();
    }

    /// <summary>
    /// Takes BattleSystem reference and calls its OnSwapButtonPress method
    /// </summary>
    /// <param name="battleSystem">Reference to active BattleSystem (in-battle only)</param>
    public void OnSwapButtonPressed(BattleSystem battleSystem)
    {
        //call battleSystem's OnSwapButtonPressed method with currentIndex
    }

    /// <summary>
    /// De-focuses details panel and hides local swap and back buttons
    /// </summary>
    public void DetailsBackButtonPress()
    {
        //select button from current details panel character
        charButtons[CurrentIndex].Select();
        DetailsFocused = false;

        detailsSwapButton.gameObject.SetActive(false);
        detailsBackButton.gameObject.SetActive(false);
    }
}
