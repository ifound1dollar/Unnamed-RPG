using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHud : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Slider hpBackSlider;
    public Slider hpMainSlider;
    public Slider enBackSlider;
    public Slider enMainSlider;

    bool animating;
    int oldHP;
    int currHP;
    int maxHP;
    int oldEN;
    int currEN;
    int maxEN;


    public void SetHUD(BattleChar battleChar)
    {
        ///sets player HUD without any animations

        //set basic string data
        nameText.text = battleChar.Name;
        levelText.text = ("Level " + battleChar.Level);

        //set HP slider values
        hpBackSlider.value = hpMainSlider.value = (float)battleChar.HP / battleChar.MaxHP;

        //set EN slider values
        enBackSlider.value = enMainSlider.value = (float)battleChar.Energy / battleChar.MaxEnergy;

        //assign values that are used for animations (below)
        currHP = battleChar.HP;
        maxHP = battleChar.MaxHP;
        currEN = battleChar.Energy;
        maxEN = battleChar.MaxEnergy;
    }
    public void UpdateHUD(BattleChar battleChar)
    {
        ///updates player HUD with animations

        //store previous data in variables
        oldHP = currHP;
        currHP = battleChar.HP;
        //oldEN = currEN;
        currEN = battleChar.Energy;


        //verify back sliders at old value, then set main sliders to real current value
        hpBackSlider.value = (float)oldHP / maxHP;
        hpMainSlider.value = (float)currHP / maxHP;
        //enBackSlider.value = (float)oldEN / maxEN;
        enMainSlider.value = (float)currEN / maxEN;
        enBackSlider.value = (float)currEN / maxEN; //TEMP? dont do energy bar animation

        //set animating bool, which tells Update() to animate bars until they are at correct values
        animating = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!animating) { return; }

        //move back slider down by difference in old and current HP
        hpBackSlider.value -= (oldHP - currHP) / Time.deltaTime;

        //if taking damage, set animating to false once less than or equal to mainSlider
        if (oldHP - currHP >= 0)
        {
            if (hpBackSlider.value <= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
            }
        }
        //else if healing, set animating to false once greater than or equal to mainSlider
        else
        {
            if (hpBackSlider.value >= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
            }
        }
    }
}
