using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class XPTable
{
    public static int GetXpToNextLevel(int level, float xpRatio)
    {
        ///Calculates XP threshold for the next level and returns the rounded value
        ///Level^3 XP threshold per level, so calculate from next level and
        /// modify by XP ratio
        
        if (level == 100)
        {
            return int.MaxValue;    //can never reach this
        }

        return Mathf.RoundToInt(Mathf.Pow(level + 1, 3) * xpRatio);
    }
}
