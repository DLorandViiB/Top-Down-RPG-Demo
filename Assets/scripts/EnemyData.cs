using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName;
    public Sprite sprite;

    [Header("Stats")]
    public int maxHealth;
    public int attack;
    public int defense;
}