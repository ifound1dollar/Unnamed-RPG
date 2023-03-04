using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] PersistentData persistentData;
    [SerializeField] DialogManager dialogManager;
    [SerializeField] PartyMenu partyMenu;
    [SerializeField] BattleSystem battleSystem;

    public PersistentData PersistentData { get { return persistentData; } }
    public DialogManager DialogManager { get { return dialogManager; } }
    public PartyMenu PartyMenu { get { return partyMenu; } }
    public BattleSystem BattleSystem { get { return battleSystem; } }


    public bool InBattle { get; set; } = false;
    public bool InMenu { get; set; } = false;
    public bool InDialog { get; set; } = false;


    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        Instance = this;
        PersistentData.InitPlayerParty();
        BattleSystem.SetPersistentData(PersistentData);
        PartyMenu.Setup(PersistentData.PlayerChars, BattleSystem);
        DialogManager.Setup(PersistentData.DialogSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public bool GetCanMove()
    {
        return !InBattle && !InMenu && !InDialog;
    }

    public void BeginBattle(BattleParty enemyParty, string battleFlag)
    {
        InBattle = true;
        BattleSystem.BeginBattle(enemyParty, battleFlag);
    }
}
