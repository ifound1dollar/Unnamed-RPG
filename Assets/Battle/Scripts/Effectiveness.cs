using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleType
{
    None,
    Nature,
    Toxic,
    Water,
    Fire,
    Earth,
    Air,
    Electric,
    Ice,
    Metal,
    Vital,
    Light,
    Dark,
    Mystic,
    Void
}


public static class Effectiveness
{
    static readonly Dictionary<BattleType, Dictionary<BattleType, float>> pairs = new()
    {
        {
            BattleType.None, new Dictionary<BattleType, float>() { }
        },
        {
            BattleType.Nature, new Dictionary<BattleType, float>()
            {
                { BattleType.Water, 2.0f }, { BattleType.Earth, 2.0f }, { BattleType.Dark, 2.0f },
                { BattleType.Fire, 0.5f }, { BattleType.Air, 0.5f }, { BattleType.Void, 0.5f }
            }
        },
        {
            BattleType.Toxic, new Dictionary<BattleType, float>()
            {
                { BattleType.Nature, 2.0f }, { BattleType.Vital, 2.0f }, { BattleType.Void, 2.0f },
                { BattleType.Earth, 0.5f }, { BattleType.Metal, 0.5f }, { BattleType.Light, 0.5f }
            }
        },
        {
            BattleType.Water, new Dictionary<BattleType, float>()
            {
                { BattleType.Fire, 2.0f }, { BattleType.Earth, 2.0f }, { BattleType.Metal, 2.0f },
                { BattleType.Nature, 0.5f }, { BattleType.Electric, 0.5f }, { BattleType.Ice, 0.5f }
            }
        },
        {
            BattleType.Fire, new Dictionary<BattleType, float>()
            {
                { BattleType.Nature, 2.0f }, { BattleType.Ice, 2.0f }, { BattleType.Metal, 2.0f },
                { BattleType.Water, 0.5f }, { BattleType.Earth, 0.5f }, { BattleType.Void, 0.5f }
            }
        },
        {
            BattleType.Earth, new Dictionary<BattleType, float>()
            {
                { BattleType.Toxic, 2.0f }, { BattleType.Fire, 2.0f }, { BattleType.Electric, 2.0f },
                { BattleType.Nature, 0.5f }, { BattleType.Water, 0.5f }, { BattleType.Air, 0.5f }
            }
        },
        {
            BattleType.Air, new Dictionary<BattleType, float>()
            {
                { BattleType.Nature, 2.0f }, { BattleType.Dark, 2.0f },
                { BattleType.Earth, 0.5f }, { BattleType.Metal, 0.5f },
            }
        },
        {
            BattleType.Electric, new Dictionary<BattleType, float>()
            {
                { BattleType.Water, 2.0f }, { BattleType.Metal, 2.0f }, { BattleType.Air, 2.0f },
                { BattleType.Nature, 0.5f }, { BattleType.Earth, 0.5f }, { BattleType.Ice, 0.5f }
            }
        },
        {
            BattleType.Ice, new Dictionary<BattleType, float>()
            {
                { BattleType.Air, 2.0f }, { BattleType.Vital, 2.0f }, { BattleType.Mystic, 2.0f },
                { BattleType.Void, 2.0f },
                { BattleType.Water, 0.5f }, { BattleType.Fire, 0.5f }, { BattleType.Metal, 0.5f }
            }
        },
        {
            BattleType.Metal, new Dictionary<BattleType, float>()
            {
                { BattleType.Ice, 2.0f }, { BattleType.Vital, 2.0f },
                { BattleType.Toxic, 0.5f }, { BattleType.Fire, 0.5f }, { BattleType.Electric, 0.5f }
            }
        },
        {
            BattleType.Vital, new Dictionary<BattleType, float>()
            {
                { BattleType.Earth, 2.0f }, { BattleType.Ice, 2.0f }, { BattleType.Mystic, 2.0f },
                { BattleType.Metal, 0.5f }, { BattleType.Light, 0.5f }, { BattleType.Dark, 0.5f }
            }
        },
        {
            BattleType.Light, new Dictionary<BattleType, float>()
            {
                { BattleType.Toxic, 2.0f }, { BattleType.Dark, 2.0f },
                { BattleType.Vital, 0.5f }, { BattleType.Mystic, 0.5f }, { BattleType.Void, 0.5f },
                { BattleType.Light, 0f }
            }
        },
        {
            BattleType.Dark, new Dictionary<BattleType, float>()
            {
                { BattleType.Light, 2.0f }, { BattleType.Mystic, 2.0f },
                { BattleType.Air, 0.5f }, { BattleType.Vital, 0.5f },
                { BattleType.Dark, 0f }
            }
        },
        {
            BattleType.Mystic, new Dictionary<BattleType, float>()
            {
                { BattleType.Fire, 2.0f }, { BattleType.Electric, 2.0f }, { BattleType.Void, 2.0f },
                { BattleType.Vital, 0.5f }, { BattleType.Dark, 0.5f }
            }
        },
        {
            BattleType.Void, new Dictionary<BattleType, float>()
            {
                { BattleType.Nature, 2.0f }, { BattleType.Light, 2.0f },
                { BattleType.Toxic, 0.5f }, { BattleType.Metal, 0.5f }, { BattleType.Mystic, 0.5f }
            }
        },
    };

    public static float GetMultiplier(BattleType userType, BattleType targetType1, BattleType targetType2)
    {
        //get each prelim value, access outer with user type and try inner with one target type
        if (!pairs[userType].TryGetValue(targetType1, out float prelimA))
        {
            //if failed to find, then is base effectiveness (change from default of 0.0f)
            prelimA = 1.0f;
        }
        if (!pairs[userType].TryGetValue(targetType2, out float prelimB))
        {
            prelimB = 1.0f;
        }

        //multiply prelim values for initial combined value
        float value = prelimA * prelimB;

        //eliminate any floating-point errors and clamp between 1/3x and 3x (except 0)
        if (Mathf.Approximately(value, 0.25f))
        {
            value = 0.33f;
        }
        else if (Mathf.Approximately(value, 0.5f))
        {
            value = 0.5f;
        }
        else if (Mathf.Approximately(value, 1.0f))
        {
            value = 1.0f;
        }
        else if (Mathf.Approximately(value, 2.0f))
        {
            value = 2.0f;
        }
        else if (Mathf.Approximately(value, 4.0f))
        {
            value = 3.0f;
        }

        //finally, return multiplier value
        return value;
    }

    public static float GetCharMultiplier(BattleChar user, BattleChar target)
    {
        float prelimA = GetMultiplier(user.SpeciesData.Type1, target.SpeciesData.Type1, target.SpeciesData.Type2);
        float prelimB = GetMultiplier(user.SpeciesData.Type2, target.SpeciesData.Type1, target.SpeciesData.Type2);

        return prelimA * prelimB;
    }
}
