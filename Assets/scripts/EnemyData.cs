using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy")]
public class EnemyData : ScriptableObject
{
    public enum ElementType { Normal, Fire, Ice }

    [Header("Info")]
    public string enemyName;
    public Sprite sprite;

    [Header("Stats")]
    public int maxHealth;
    public int attack;
    public int defense;
    public int xpYield = 10;
    public int currencyYield = 10;
    public bool isBoss = false;

    public ElementType element;
}