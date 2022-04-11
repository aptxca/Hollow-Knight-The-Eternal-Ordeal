using UnityEditor;
using UnityEngine;


namespace Demo2D
{
    [CreateAssetMenu(fileName = "GameSettings",menuName = "GameSettings")]
    public class GameSettings :  ScriptableObject
    {
        public int frameRate;
    }
}