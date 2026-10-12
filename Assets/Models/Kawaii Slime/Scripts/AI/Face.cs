using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Face", order = 1)]
public class Face :ScriptableObject
{
    public Texture Idleface, WalkFace, jumpFace, attackFace,damageFace;
}
