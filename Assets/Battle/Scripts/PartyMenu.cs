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

    [Header("SelectOptions")]
    [SerializeField] PartySelectOptions inBattleOptions;
    [SerializeField] PartySelectOptions inBattleNoSwapOptions;
    [SerializeField] PartySelectOptions outOfBattleOptions;

    [Header("Details")]
    [SerializeField] GameObject detailsPanel;
    [SerializeField] GameObject detailsOverlay;
    [SerializeField] Button[] detailsCharButtons;
    [SerializeField] Button detailsBackButton;
    [SerializeField] TMP_Text detailsName;
    [SerializeField] TMP_Text specUp;
    [SerializeField] TMP_Text specDown;

    [Header("Abilities")]
    [SerializeField] Button[] abilityButtons;
    [SerializeField] TMP_Text[] abilityTexts;
    [SerializeField] Button abilityBackButton;
    [SerializeField] GameObject abilityInfoOverlay;
    [SerializeField] TMP_Text abilityName;
    [SerializeField] TMP_Text abilityType;
    [SerializeField] TMP_Text abilityCategory;
    [SerializeField] TMP_Text abilityContact;
    [SerializeField] TMP_Text abilityEnergy;
    [SerializeField] TMP_Text abilityPower;
    [SerializeField] TMP_Text abilityAccuracy;


    //MAKE BOTH OF THESE METHODS, FUCK PUBLIC PROPERTIES HERE
    public Button[] CharButtons { get { return charButtons; } }
    public Button BackButton { get { return backButton; } }


    public bool AbilitiesFocused { get; private set; }
    public bool DetailsFocused { get; private set; }
    public int CurrButtonIndex { get; set; }
    public bool BackButtonJumped { get; private set; }

    BattleChar[] playerChars;
    DialogBox dialogBox;
    BattleSystem battleSystem;
    int currPlayerIndex = 0;
    int lastAbilityIndex = 0;



    /// <summary>
    /// Sets the local reference to the playerChars array
    /// </summary>
    /// <param name="battleChars">Reference to array of BattleChars</param>
    public void SetPlayerCharsReference(BattleChar[] battleChars)
    {
        playerChars = battleChars;
    }

    /// <summary>
    /// Sets reference to DialogBox, used in battle when hiding PartyMenu
    /// </summary>
    /// <param name="dialogBox">Reference to DialogBox object</param>
    public void SetDialogBoxReference(DialogBox dialogBox)
    {
        this.dialogBox = dialogBox;
    }

    /// <summary>
    /// Sets local reference to active BattleSystem
    /// </summary>
    /// <param name="battleSystem"></param>
    public void SetBattleSystemReference(BattleSystem battleSystem)
    {
        this.battleSystem = battleSystem;
    }




    /// <summary>
    /// Loads BattleChar info into party buttons and sets currPlayerIndex from argument
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
                detailsCharButtons[i].interactable = false;
            }
        }
        this.currPlayerIndex = currPlayerIndex;
    }

    /// <summary>
    /// Hides PartyMenu when back button is pressed
    /// </summary>
    public void HidePartyMenu()
    {
        CurrButtonIndex = 0;
        BackButtonJumped = false;

        //if dialogBox is not null, call its function to auto-select main Party button
        if (dialogBox != null)
        {
            dialogBox.ShowPartyMenu(false);
        }
    }




    /// <summary>
    /// Shows menu options when selecting a PartyButton
    /// </summary>
    public void ShowSelectOptions()
    {
        //if valid (not -1), then is in battle; else is out of battle
        if (currPlayerIndex != -1)
        {
            if (currPlayerIndex == CurrButtonIndex)
            {
                inBattleNoSwapOptions.gameObject.SetActive(true);
                inBattleNoSwapOptions.DetailsButton.Select();
            }
            else
            {
                inBattleOptions.gameObject.SetActive(true);
                inBattleOptions.SwapInButton.Select();
            }
        }
        else
        {
            outOfBattleOptions.gameObject.SetActive(true);
            outOfBattleOptions.DetailsButton.Select();
        }
    }

    /// <summary>
    /// Hides menu options after selecting a PartyButton
    /// </summary>
    public void HideSelectOptions()
    {
        //hide all select options and reselect original party button
        inBattleOptions.gameObject.SetActive(false);
        inBattleNoSwapOptions.gameObject.SetActive(false);
        outOfBattleOptions.gameObject.SetActive(false);
        charButtons[CurrButtonIndex].Select();
    }




    /// <summary>
    /// Focuses details panel, setting active and auto-selecing first DetailsCharButton
    /// </summary>
    public void FocusDetailsPanel()
    {
        DetailsFocused = true;
        detailsPanel.SetActive(true);
        detailsCharButtons[CurrButtonIndex].Select();
    }

    /// <summary>
    /// Shows details of the selected party button's corresponding BattleChar
    /// </summary>
    /// <param name="index">Index of selected BattleChar</param>
    public void ShowDetails()
    {
        BattleChar player = playerChars[CurrButtonIndex];

        //SHOW DATA
        detailsName.text = player.Name;
        specUp.text = "Spec up: " + player.SpecialtyUp.ToString();
        specDown.text = "Spec down: " + player.SpecialtyDown.ToString();

        for (int i = 0; i < player.Abilities.Length; i++)
        {
            abilityTexts[i].text = player.Abilities[i].Name;
            if (player.Abilities[i].Name == "EMPTY")
            {
                //MISSING should remain interactable
                abilityButtons[i].interactable = false;
            }
        }

        //find and store last Ability button index for quick access
        for (int i = 0; i < player.Abilities.Length; i++)
        {
            if (player.Abilities[i].Name == "EMPTY")
            {
                //set to index just before first found EMPTY Ability
                lastAbilityIndex = i - 1;
                break;
            }
        }

        //set active and select current detailsCharButton
        detailsOverlay.SetActive(false);
        DetailsFocused = true;
    }

    /// <summary>
    /// Temporarily covers up details pane while back button is selected
    /// </summary>
    public void CoverDetails()
    {
        detailsOverlay.SetActive(true);
    }

    /// <summary>
    /// Hides details panel and re-focuses calling Button
    /// </summary>
    public void HideDetails()
    {
        HideSelectOptions();

        //select button from current details panel character
        charButtons[CurrButtonIndex].Select();
        DetailsFocused = false;
        BackButtonJumped = false;

        detailsPanel.SetActive(false);
    }




    /// <summary>
    /// Focuses Ability panel, auto-selecting first Ability
    /// </summary>
    public void FocusAbilityPanel()
    {
        AbilitiesFocused = true;
        abilityButtons[0].Select();
    }

    /// <summary>
    /// Shows detailed Ability info at provided index (when hovering button)
    /// </summary>
    public void ShowAbilityInfo(int index)
    {
        Ability ability = playerChars[CurrButtonIndex].Abilities[index];

        abilityName.text = ability.Name;
        abilityType.text = "Type: " + ability.AbilityType.ToString();
        abilityCategory.text = "Category: " + ability.Category.ToString();
        abilityContact.text = (ability.MakesContact) ? "Contact: Yes" : "Contact: No";
        abilityEnergy.text = "Energy: " + ability.Energy.ToString();
        abilityPower.text = "Power: " + ability.Power.ToString();
        abilityAccuracy.text = "Accuracy: " + ability.Accuracy.ToString();

        abilityInfoOverlay.SetActive(true);
        AbilitiesFocused = true;
    }

    /// <summary>
    /// Temporarily covers Ability info overlay while back button is hovered
    /// </summary>
    public void CoverAbilityInfo()
    {
        abilityInfoOverlay.SetActive(false);
    }

    /// <summary>
    /// Hides Ability info overlay and auto-selects current DetailsCharButton
    /// </summary>
    public void HideAbilityInfo()
    {
        abilityInfoOverlay.SetActive(false);
        AbilitiesFocused = false;
        BackButtonJumped = false;
        detailsCharButtons[CurrButtonIndex].Select();
    }




    /// <summary>
    /// Takes BattleSystem reference and calls its OnSwapButtonPress method
    /// </summary>
    /// <param name="battleSystem">Reference to active BattleSystem (in-battle only)</param>
    public void OnSwapButtonPressed()
    {
        if (battleSystem != null)
        {
            HideSelectOptions();

            //call battleSystem's OnSwapButtonPressed method with currentIndex
            battleSystem.OnSwapButtonPress(CurrButtonIndex);
        }
    }




    /// <summary>
    /// Selects last available CharButton or AbilityButton, based on context
    /// </summary>
    public void SelectCorrectNormalButton()
    {
        if (AbilitiesFocused)
        {
            abilityButtons[lastAbilityIndex].Select();
        }
        else if (DetailsFocused)
        {
            detailsCharButtons[playerChars.Length - 1].Select();
            CurrButtonIndex = playerChars.Length - 1;
        }
        else
        {
            charButtons[playerChars.Length - 1].Select();
            CurrButtonIndex = playerChars.Length - 1;
        }
        BackButtonJumped = false;
    }

    /// <summary>
    /// Selects relevant back button, based on context
    /// </summary>
    public void SelectCorrectBackButton()
    {
        if (AbilitiesFocused)
        {
            abilityBackButton.Select();
        }
        else if (DetailsFocused)
        {
            detailsBackButton.Select();
        }
        else
        {
            backButton.Select();
        }
        BackButtonJumped = true;
    }




    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //if DetailsFocused (ability info is focused)
            if (AbilitiesFocused)
            {
                HideAbilityInfo();
            }
            //else if details panel is focused, and abilities not
            else if (DetailsFocused)
            {
                HideDetails();
            }
            //else if select options are shown
            else if (inBattleOptions.gameObject.activeSelf || inBattleNoSwapOptions.gameObject.activeSelf
                || outOfBattleOptions.gameObject.activeSelf)
            {
                HideSelectOptions();
            }
            //else back out of entire PartyMenu IF BUTTON IS VALID
            else if (backButton.gameObject.activeSelf)
            {
                HidePartyMenu();
            }
        }
    }
}
