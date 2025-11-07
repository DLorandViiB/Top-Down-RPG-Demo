using UnityEngine;

[System.Serializable] // This lets us see it in the Inspector, which is nice
public class Buff
{
    public SkillData.SkillEffect effect;
    public int duration; // How many turns are left
}