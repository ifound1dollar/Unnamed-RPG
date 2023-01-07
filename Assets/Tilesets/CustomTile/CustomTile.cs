using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Custom Tile")]
public class CustomTile : Tile
{
    [Header("Custom Tile Attributes")]
    [SerializeField] int testInt;

    public int TestInt { get => testInt; }
}
