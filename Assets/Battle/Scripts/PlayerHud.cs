using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text hpText;
    public Slider hpBackSlider;
    public Slider hpMainSlider;
    public TMP_Text enText;
    public Slider enBackSlider;
    public Slider enMainSlider;

    bool animating;
    int oldHP;
    int currHP;
    int maxHP;
    int oldEN;
    int currEN;
    int maxEN;

    float delay;

    public void SetHUD(BattleChar battleChar)
    {
        ///sets player HUD without any animations

        //set basic string data
        nameText.text = battleChar.Name;
        levelText.text = ("Level " + battleChar.Level);
        enText.text = (battleChar.Energy + " / " + battleChar.MaxEnergy);
        hpText.text = (battleChar.HP + " / " + battleChar.MaxHP);

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
        
        ///IMPORTANT: MUST ALSO UPDATE STATUS EFFECT ICON

        //store previous data in variables and update HP and EN text right away
        oldHP = currHP;
        currHP = battleChar.HP;
        //oldEN = currEN;
        currEN = battleChar.Energy;

        hpText.text = (currHP + " / " + maxHP);
        enText.text = (currEN + " / " + maxEN);

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

        if (delay < 0.2) { delay += Time.deltaTime; return; }

        //move back slider down by difference in old and current HP, will take 1/2 of a second
        hpBackSlider.value -= ((oldHP - currHP) * (Time.deltaTime * 2f)) / maxHP;

        //if taking damage, set animating to false once less than or equal to mainSlider
        if (oldHP - currHP >= 0)
        {
            if (hpBackSlider.value <= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
                delay = 0f;
            }
        }
        //else if healing, set animating to false once greater than or equal to mainSlider
        else
        {
            if (hpBackSlider.value >= hpMainSlider.value)
            {
                hpBackSlider.value = hpMainSlider.value;
                animating = false;
                delay = 0f;
            }
        }
    }
}
