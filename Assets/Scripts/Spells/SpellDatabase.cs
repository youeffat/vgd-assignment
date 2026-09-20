using System.Collections.Generic;
using UnityEngine;

public enum SpellType
{
    AntiGravity,
    SwapPositions
}

[System.Serializable]
public struct SpellMapping
{
    public string symbolName;
    public SpellType spellType;
}

public class SpellDatabase : MonoBehaviour
{
    [Header("Spell Mapping Database")]
    public List<SpellMapping> spellDatabase;

    public SpellType? GetSpellType(string symbolName)
    {
        foreach (var spell in spellDatabase)
        {
            if (spell.symbolName == symbolName)
            {
                return spell.spellType;
            }
        }
        return null;
    }
}