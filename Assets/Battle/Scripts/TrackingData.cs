using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingData
{
    public void UpdateAfterSwap(bool forced = false)
    {
        ///Updates data relevant to swapping

        //no need to check team, will be called manually in PerformSwap() method

        //UPDATE:
        //  1) set Swapped bool
        //  2) set ForcedSwap bool to parameter 'forced'
    }
    public void UpdateAfterAttack(AbilityData data)
    {
        ///Updates data relevant to attacking

        //check if user is playerteam, else target is playerteam

        //UPDATE: (two different variables for user/target)
        //  1) total damage dealt/taken (depending on user or target)
        //  2) effectiveness of attack (dealt or taken)
        //  3) whether critical strike (dealt or taken)
    }
    public void UpdateFinal(BattleChar battleChar)
    {
        ///Updates general data and end of turn, as final update

        //no need to check team, will be passed currPlayer or currEnemy correctly

        //UPDATE/STORE:
        //  1) final HP and Energy
        //  2) final modifiers (net only?)
        //  3) active status effect (string, empty if none)
        //  4) 
    }
}
