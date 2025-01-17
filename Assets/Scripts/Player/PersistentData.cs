using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PersistentData : MonoBehaviour
{
    [SerializeField] BattleAI.AIDifficulty difficulty;
    [Tooltip("Dialog print speed in characters per second, default 30")]
    [SerializeField] int dialogSpeed = 30;
    [Tooltip("Delay in seconds after dialog finishes printing, default 1.5")]
    [SerializeField] float textDelay = 1.5f;

    //TEMP
    [Header("TEMP")]
    [SerializeField] BattleParty playerParty;   //will pull from save file later
    //TEMP


    public BattleAI.AIDifficulty Difficulty { get { return difficulty; } }
    public int DialogSpeed { get { return dialogSpeed; } }
    public float TextDelay { get { return textDelay; } }


    //not visible in editor
    public BattleChar[] PlayerChars { get; private set; }
    public Dictionary<string, bool> Flags { get; private set; } = new();


    /// <summary>
    /// Initializes PlayerChars from save data
    /// </summary>
    public void InitPlayerParty()
    {
        ///WILL PULL DATA FROM SAVE FILE IN THE FUTURE

        PlayerChars = new BattleChar[playerParty.Team.Length];
        for (int i = 0; i < playerParty.Team.Length; i++)
        {
            PlayerChars[i] = new BattleChar(playerParty.Team[i], difficulty, playerTeam: true);
        }
    }

}
