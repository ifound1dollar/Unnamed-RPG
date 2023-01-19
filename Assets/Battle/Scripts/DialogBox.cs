using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogBox : MonoBehaviour
{
    [SerializeField] PartyMenu partyMenu;
    [SerializeField] GameObject abilityOverlay;
    [SerializeField] Button[] abilityButtons;
    [SerializeField] TMP_Text[] abilityNames;
    [SerializeField] TMP_Text[] abilityEnergies;
    [SerializeField] TMP_Text dialogText;

    [SerializeField] Button attackButton;
    [SerializeField] Button passButton;
    [SerializeField] Button partyButton;
    [SerializeField] Button fleeForfeitButton;
    [SerializeField] Button abilityBackButton;

    BattleSystem battleSystem;

    [Tooltip("Dialog print speed in characters per second.")]
    [SerializeField] int dialogSpeed = 30;  //default 30 characters per second

    public PartyMenu PartyMenu { get { return partyMenu; } }

    /// <summary>
    /// Sets BattleSystem reference, also sets playerChars reference in PartyMenu
    /// </summary>
    /// <param name="battleSystem">Reference to active BattleSystem</param>
    public void Setup(BattleSystem battleSystem)
    {
        this.battleSystem = battleSystem;
        partyMenu.SetPlayerCharsReference(battleSystem.PlayerChars);
        partyMenu.SetDialogBoxReference(this);
        partyMenu.SetBattleSystemReference(battleSystem);
    }

    /// <summary>
    /// Selects attack button, typically used for beginning of turn
    /// </summary>
    public void SelectAttackButton()
    {
        attackButton.Select();
    }

    /// <summary>
    /// Sets Ability button info for current player BattleChar
    /// </summary>
    /// <param name="battleChar">Reference to player's active BattleChar</param>
    public void SetAbilityButtons(BattleChar battleChar)
    {
        for (int i = 0; i < 4; i++)
        {
            abilityNames[i].text = battleChar.Abilities[i].Name;

            if (battleChar.Abilities[i].Name != "MISSING" && battleChar.Abilities[i].Name != "EMPTY"
                && battleChar.Abilities[i].EnergyCost(battleChar) <= battleChar.Energy)
            {
                abilityButtons[i].interactable = true;
                abilityEnergies[i].text = battleChar.Abilities[i].EnergyCost(battleChar).ToString();
            }
            else
            {
                abilityButtons[i].interactable = false;
                abilityEnergies[i].text = "";
            }
        }
    }

    /// <summary>
    /// Shows/hides four main buttons: Attack, Pass, Party, FleeForfeit
    /// </summary>
    /// <param name="enable">Bool denoting whether to enable or disable buttons</param>
    public void ShowMainButtons(bool enable)
    {
        attackButton.gameObject.SetActive(enable);
        passButton.gameObject.SetActive(enable);
        partyButton.gameObject.SetActive(enable);
        fleeForfeitButton.gameObject.SetActive(enable);
    }

    /// <summary>
    /// Shows/hides ability buttons overlay (Ability buttons and back button)
    /// </summary>
    /// <param name="enable">Bool for whether to enable or disable buttons</param>
    public void ShowAbilityButtons(bool enable)
    {
        //if showing buttons, auto-select first Ability
        if (enable)
        {
            //select first interactable button; if none, select back button
            bool validButton = false;
            foreach (Button button in abilityButtons)
            {
                if (button.interactable)
                {
                    validButton = true;
                    button.Select();
                    break;
                }
            }
            
            if (!validButton)
            {
                abilityBackButton.Select();
            }
        }
        //else back button pressed or Ability pressed, so auto select Attack button
        else
        {
            attackButton.Select();
        }

        //show overlay and back button, hide original 4 buttons (and vice versa when enable = false)
        abilityOverlay.SetActive(enable);
        abilityBackButton.gameObject.SetActive(enable);

        attackButton.gameObject.SetActive(!enable);
        passButton.gameObject.SetActive(!enable);
        partyButton.gameObject.SetActive(!enable);
        fleeForfeitButton.gameObject.SetActive(!enable);
    }

    /// <summary>
    /// Shows/hides PartyMenu, reloading party data if showing
    /// </summary>
    /// <param name="enable">Bool for whether to enable or disable menu</param>
    public void ShowPartyMenu(bool enable)
    {
        //if showing party menu, reload data then select first char show details
        if (enable)
        {
            partyMenu.LoadPartyChars(currPlayerIndex: battleSystem.GetCurrBattleCharIndex(playerTeam: true));
            partyMenu.SelectCurrPlayerButton();
            partyMenu.ShowPartyMenu();
        }
        //else if hiding, auto select Party button
        else
        {
            partyButton.Select();
            partyMenu.HidePartyMenu();
        }
    }
    public void HidePartyBackButton()
    {
        partyMenu.ShowPartyMenu(showBackButton: false);
    }

    /// <summary>
    /// Shows main buttons and auto-selects PartyButton
    /// </summary>
    public void FocusMainPartyButton()
    {
        ShowMainButtons(true);
        partyButton.Select();
    }



    /// <summary>
    /// Sets dialog text to the text argument
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IEnumerator DialogSet(string text)
    {
        //set text TRANSPARENT (scrolling text will move a character down a line in the middle
        //  of a word if it reaches text box border, which looks weird to the player; starting
        //  with the text already sized correctly will prevent this weird behavior)

        dialogText.text = "<color=#ffffff00>" + text + "</color>";  //17, 8

        yield return ScrollText(text);
    }

    /// <summary>
    /// Appends text argument to current dialog text with a single space in-between
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IEnumerator DialogAppend(string text)
    {
        int origLength = dialogText.text.Length;
        dialogText.text += " <color=#ffffff00>" + text + "</color>";

        //must start at origLength + 1 because a space is added before color definition
        yield return ScrollText(text, startIndex: origLength + 1);
    }

    /// <summary>
    /// Displays dialog text one character at a time
    /// </summary>
    /// <param name="text">Text to be displayed one character at a time</param>
    /// <param name="startIndex">Index to begin displaying text from, inclusive (append)</param>
    /// <returns></returns>
    IEnumerator ScrollText(string text, int startIndex = 0)
    {
        ///This method declares a StringBuilder with the recently set dialogText. It removes
        /// the first DISPLAYED character in the original dialogText (after the color
        /// definition, which uses 17 characters) then inserts the character at this index
        /// before the color definition (thus is the default, visible color).
        ///Parameter startIndex is optional and is for appending to existing text. If defined,
        /// all text before startIndex will already be visible, and only the characters after
        /// will be displayed one by one.
        ///This gives the illusion that the text is being printed one character at a time,
        /// but the text already exists transparently for text box sizing purposes.

        StringBuilder temp = new(dialogText.text);

        for (int i = 0; i < text.Length; i++)
        {
            temp.Remove(i + startIndex + 17, 1);    //remove AT index i, 1 character
            temp.Insert(i + startIndex, text[i]);   //insert AFTER index i
            dialogText.text = temp.ToString();
            yield return new WaitForSeconds(1f/dialogSpeed);
        }

        //remove the now useless color definition (17 for beginning, 8 for end)
        dialogText.text = dialogText.text[..^25];
    }


    /// <summary>
    /// Handles Escape key press, auto-invoking the relevant Back Button
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //invoke ability back button only if ability overlay is shown
            if (abilityOverlay.activeSelf)
            {
                abilityBackButton.onClick.Invoke();
            }
        }
    }
}
