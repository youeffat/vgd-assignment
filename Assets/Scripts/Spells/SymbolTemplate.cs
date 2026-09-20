using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSymbolTemplate", menuName = "Gesture Recognition/Symbol Template")]
public class SymbolTemplate : ScriptableObject
{
    public string symbolIdName; // e.g., "Fireball", "Shield", "Cross"
    
    [HideInInspector]
    public List<Vector2> templatePoints = new List<Vector2>();
}