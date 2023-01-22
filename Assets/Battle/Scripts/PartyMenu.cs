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
    [SerializeField] GameObject detailsButtonsContainer;
    [SerializeField] Button[] detailsCharButtons;
    [SerializeField] Button detailsBackButton;
    [SerializeField] TMP_Text detailsName;
    [SerializeField] TMP_Text specUp;
    [SerializeField] TMP_Text specDown;

    [Header("Abilities")]
    [SerializeField] Button[] abilityButtons;
    [SerializeField] TMP_Text[] abilityTexts;
    [SerializeField] Button abilityBackButton;
    [SerializeField] AbilitySelectOptions abilityReorderOption;
    [SerializeField] AbilitySelectOptions abilityRemoveOption;
    [Space()]
    [SerializeField] GameObject abilityInfoOverlay;
    [SerializeField] TMP_Text abilityName;
    [SerializeField] TMP_Text abilityType;
    [SerializeField] TMP_Text abilityCategory;
    [SerializeField] TMP_Text abilityContact;
    [SerializeField] TMP_Text abilityEnergy;
    [SerializeField] TMP_Text abilityPower;
    [SerializeField] TMP_Text abilityAccuracy;

    [Header("Teach Ability")]
    [SerializeField] GameObject teachAbilityOverlay;
    [SerializeField] GameObject teachWarningOverlay;
    [SerializeField] TMP_Text teachAbilityName;
    [SerializeField] TMP_Text teachAbilityType;
    [SerializeField] TMP_Text teachAbilityCategory;
    [SerializeField] TMP_Text teachAbilityContact;
    [SerializeField] TMP_Text teachAbilityEnergy;
    [SerializeField] TMP_Text teachAbilityPower;
    [SerializeField] TMP_Text teachAbilityAccuracy;


    public bool AbilitiesFocused    { get; private set; }
    public bool DetailsFocused      { get; private set; }
    public int CurrCharIndex        { get; set; }
    public int CharReorderIndex     { get; set; } = -1;
    public int CurrAbilityIndex     { get; set; }
    public int AbilityReorderIndex  { get; set; } = -1;
    public Ability TeachAbility     { get; set; }
    public bool BackButtonJumped    { get; private set; }

    BattleChar[] playerChars;
    BattleSystem battleSystem;
    int currPlayerIndex = 0;
    int lastAbilityIndex = 0;




    /// <summary>
    /// Sets local references to playerChars array and BattleSystem instance
    /// </summary>
    /// <param name="playerChars">Reference to playerChars array</param>
    /// <param name="battleSystem">Reference to active BattleSystem instance</param>
    public void Setup(BattleChar[] playerChars, BattleSystem battleSystem = null)
    {
        this.playerChars = playerChars;
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
    /// Shows party menu and auto-reloads party chars, optionally hiding back button
    /// </summary>
    public void ShowPartyMenu(bool showBackButton = true)
    {
        gameObject.SetActive(true);
        backButton.gameObject.SetActive(showBackButton);
    }

    /// <summary>
    /// Focuses party menu, selecting the first char button
    /// </summary>
    public void FocusPartyMenu()
    {
        charButtons[0].Select();
    }

    /// <summary>
    /// Hides PartyMenu when back button is pressed
    /// </summary>
    public void HidePartyMenu()
    {
        CurrCharIndex = 0;
        BackButtonJumped = false;
        gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);

        //if battleSystem is not null, call its function to auto-select main Party button
        if (battleSystem != null)
        {
            battleSystem.HidePartyMenuInBattle();
        }
        //else if is null, should deselect all
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }




    /// <summary>
    /// Shows menu options when selecting a PartyButton
    /// </summary>
    public void ShowSelectOptions()
    {
        //if CharReorderIndex is NOT -1, then is in Reorder mode
        if (CharReorderIndex != -1)
        {
            HandleCharReorder();
            return;
        }

        //if valid (not -1), then is in battle; else is out of battle
        if (currPlayerIndex != -1)
        {
            //if is currPlayer or currPlayer cannot swap
            if (currPlayerIndex == CurrCharIndex || !playerChars[currPlayerIndex].CheckCanSwap())
            {
                inBattleNoSwapOptions.gameObject.SetActive(true);
                inBattleNoSwapOptions.DetailsButton.Select();
            }
            //else can swap so show normal options
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
        charButtons[CurrCharIndex].Select();
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
            battleSystem.OnSwapButtonPress(CurrCharIndex);
        }
    }

    /// <summary>
    /// Starts reordering process, hiding select options and storing initial reorder button
    /// </summary>
    public void OnCharReorderButtonPressed()
    {
        CharReorderIndex = CurrCharIndex;
        HideSelectOptions();
        backButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Does reordering operation, swapping BattleChars, reloading, then ending reorder mode
    /// </summary>
    void HandleCharReorder()
    {
        //reorder actual playerChars
        (playerChars[CurrCharIndex], playerChars[CharReorderIndex])
            = (playerChars[CharReorderIndex], playerChars[CurrCharIndex]);

        LoadPartyChars(currPlayerIndex);    //avoid resetting currPlayerIndex to -1
        CancelCharReorder();
    }

    /// <summary>
    /// Cancel reorder mode, resetting CharReorderIndex and showing BackButton again
    /// </summary>
    void CancelCharReorder()
    {
        CharReorderIndex = -1;
        backButton.gameObject.SetActive(true);
    }




    /// <summary>
    /// Focuses details panel, setting active and auto-selecing first DetailsCharButton
    /// </summary>
    public void FocusDetailsPanel()
    {
        DetailsFocused = true;
        detailsPanel.SetActive(true);
        detailsCharButtons[CurrCharIndex].Select();
    }

    /// <summary>
    /// Shows details of the selected party button's corresponding BattleChar
    /// </summary>
    /// <param name="index">Index of selected BattleChar</param>
    public void ShowDetails()
    {
        BattleChar player = playerChars[CurrCharIndex];

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
            else
            {
                abilityButtons[i].interactable = true;
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
        charButtons[CurrCharIndex].Select();
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
    public void ShowAbilityInfo()
    {
        Ability ability = playerChars[CurrCharIndex].Abilities[CurrAbilityIndex];

        abilityName.text = ability.Name;
        abilityType.text = "Type: " + ability.AbilityType.ToString();
        abilityCategory.text = "Category: " + ability.Category.ToString();
        abilityContact.text = (ability.MakesContact) ? "Contact: Yes" : "Contact: No";
        abilityEnergy.text = "Energy: " + ability.Energy.ToString();
        abilityPower.text = "Power: " + ability.Power.ToString();
        abilityAccuracy.text = "Accuracy: " + ability.Accuracy.ToString();

        teachWarningOverlay.SetActive(false);
        abilityInfoOverlay.SetActive(true);
        AbilitiesFocused = true;
    }

    /// <summary>
    /// Temporarily covers Ability info overlay while back button is hovered
    /// </summary>
    public void CoverAbilityInfo()
    {
        abilityInfoOverlay.SetActive(false);

        //if TeachAbility not null, then is teaching, so show warning
        if (TeachAbility != null)
        {
            teachWarningOverlay.SetActive(true);
        }
    }

    /// <summary>
    /// Hides Ability info overlay and auto-selects current DetailsCharButton
    /// </summary>
    public void HideAbilityInfo()
    {
        //if TeachAbility not null, then cancelling must CancelAbilityRemove
        if (TeachAbility != null)
        {
            CancelAbilityRemove();
            return;
        }

        teachWarningOverlay.SetActive(false);
        abilityInfoOverlay.SetActive(false);
        AbilitiesFocused = false;
        BackButtonJumped = false;
        detailsCharButtons[CurrCharIndex].Select();

        CurrAbilityIndex = -1;
    }




    /// <summary>
    /// Shows correct AbilityOption panel and auto-selects based on context
    /// </summary>
    public void ShowAbilityOption()
    {
        //if AbilityReorderIndex is NOT -1, then is in Reorder mode
        if (AbilityReorderIndex != -1)
        {
            HandleAbilityReorder();
            return;
        }

        //if learning, only allow remove option
        if (TeachAbility != null)
        {
            abilityRemoveOption.gameObject.SetActive(true);
            abilityRemoveOption.RemoveButton.Select();
        }
        else
        {
            //only show reorder option if out of battle
            if (currPlayerIndex != -1)
            {
                abilityReorderOption.gameObject.SetActive(true);
                abilityReorderOption.ReorderButton.Select();
            }
        }
    }

    /// <summary>
    /// Hides AbilityOption window with Reorder or Remove button
    /// </summary>
    public void HideAbilityOption()
    {
        abilityReorderOption.gameObject.SetActive(false);
        abilityRemoveOption.gameObject.SetActive(false);
        abilityButtons[CurrAbilityIndex].Select();
    }

    /// <summary>
    /// Sets TeachAbility reference and display info, then loads entire PartyMenu interface
    /// </summary>
    /// <param name="ability">Ability to teach</param>
    /// <param name="charIndex">Index of player character being taught</param>
    public void BeginTeachAbility(Ability ability, int charIndex)
    {
        TeachAbility = ability;

        teachAbilityName.text = ability.Name;
        teachAbilityType.text = "Type: " + ability.AbilityType.ToString();
        teachAbilityCategory.text = "Category: " + ability.Category.ToString();
        teachAbilityContact.text = (ability.MakesContact) ? "Contact: Yes" : "Contact: No";
        teachAbilityEnergy.text = "Energy: " + ability.Energy.ToString();
        teachAbilityPower.text = "Power: " + ability.Power.ToString();
        teachAbilityAccuracy.text = "Accuracy: " + ability.Accuracy.ToString();

        //reload chars then set this entire gameobject active
        LoadPartyChars(currPlayerIndex);
        ShowPartyMenu();

        //load details of this character, then hide other character buttons
        CurrCharIndex = charIndex;
        ShowDetails();
        detailsPanel.SetActive(true);
        detailsButtonsContainer.SetActive(false);

        //focus Ability panel and show TeachAbility info
        FocusAbilityPanel();
        teachAbilityOverlay.SetActive(true);
    }

    /// <summary>
    /// Called when button pressed, handling Ability removal (or replacement)
    /// </summary>
    public void OnAbilityRemoveButtonPressed()
    {
        abilityRemoveOption.gameObject.SetActive(false);
        HandleAbilityRemove();
    }

    /// <summary>
    /// Removes Ability at CurrAbilityIndex and possibly replaces with TeachAbility
    /// </summary>
    void HandleAbilityRemove()
    {
        BattleChar player = playerChars[CurrCharIndex];
        if (TeachAbility != null)
        {
            //replace ability at selected index with TeachAbility
            player.Abilities[CurrAbilityIndex] = TeachAbility;
        }
        else
        {
            //replace with EmptyAbility (remove), then move it to end
            player.Abilities[CurrAbilityIndex] = new EmptyAbility();
            for (int i = CurrAbilityIndex; i < 3; i++)
            {
                //if next Ability is legitimate, swap locations
                if (player.Abilities[i + 1].Name != "EMPTY")
                {
                    (player.Abilities[i], player.Abilities[i + 1])
                        = (player.Abilities[i + 1], player.Abilities[i]);
                }
            }
        }

        CancelAbilityRemove();
    }

    /// <summary>
    /// Resets TeachAbility, hides its data, and completely hides PartyMenu
    /// </summary>
    void CancelAbilityRemove()
    {
        TeachAbility = null;
        teachAbilityOverlay.SetActive(false);
        HideAbilityInfo();
        HideDetails();
        detailsButtonsContainer.SetActive(true);
        HidePartyMenu();
    }




    /// <summary>
    /// Begins reordering process, setting AbilityReorderIndex and hiding back button
    /// </summary>
    public void OnAbilityReorderButtonPressed()
    {
        AbilityReorderIndex = CurrAbilityIndex;
        abilityReorderOption.gameObject.SetActive(false);
        abilityBackButton.gameObject.SetActive(false);
        abilityButtons[CurrAbilityIndex].Select();
    }

    /// <summary>
    /// Performs Ability swap, reloads details panel, then resets reorder data
    /// </summary>
    void HandleAbilityReorder()
    {
        //swap Abilities in array
        BattleChar player = playerChars[CurrCharIndex];
        (player.Abilities[AbilityReorderIndex], player.Abilities[CurrAbilityIndex])
            = (player.Abilities[CurrAbilityIndex], player.Abilities[AbilityReorderIndex]);

        ShowDetails();
        CancelAbilityReorder();
    }

    /// <summary>
    /// Resets AbilityReorderIndex and shows back button again
    /// </summary>
    void CancelAbilityReorder()
    {
        AbilityReorderIndex = -1;
        abilityBackButton.gameObject.SetActive(true);
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
            CurrCharIndex = playerChars.Length - 1;
        }
        else
        {
            charButtons[playerChars.Length - 1].Select();
            CurrCharIndex = playerChars.Length - 1;
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
            if (abilityBackButton.gameObject.activeSelf)
            {
                abilityBackButton.Select();
            }
            else
            {
                //if ability back button not visible, reselect last CharButton
                SelectCorrectNormalButton();
            }
        }
        else if (DetailsFocused)
        {
            detailsBackButton.Select();
        }
        else
        {
            if (backButton.gameObject.activeSelf)
            {
                backButton.Select();
            }
            else
            {
                //if back button not visible, reselect last CharButton
                SelectCorrectNormalButton();
            }
        }
        BackButtonJumped = true;
    }




    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //if ability info is focused
            if (AbilitiesFocused)
            {
                //in reorder mode
                if (AbilityReorderIndex != -1)
                {
                    CancelAbilityReorder();
                }
                //else if either AbilityOption is active
                else if (abilityReorderOption.gameObject.activeSelf || abilityRemoveOption.gameObject.activeSelf)
                {
                    HideAbilityOption();
                }
                //if not null, then is teaching
                else if (TeachAbility != null)
                {
                    if (CurrAbilityIndex == -1)
                    {
                        //if already selected, will Cancel ability removal/replacement
                        CancelAbilityRemove();
                    }
                    else
                    {
                        //pressing Escape should hover but not auto-select back button if not already selected
                        abilityBackButton.Select();
                    }
                }
                //else back out of Ability panel focus like normal
                else
                {
                    HideAbilityInfo();
                }
            }
            //else if details panel is focused
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
            //else if not -1, then is in reorder mode
            else if (CharReorderIndex != -1)
            {
                CancelCharReorder();
            }
        }
    }
}
