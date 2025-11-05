using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButton : MonoBehaviour
{
    public TextMeshProUGUI skillText;

    // Set these colors in the Prefab's Inspector
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    // This sets up the text and default color
    public void Setup(SkillData skill)
    {
        skillText.text = $"{skill.skillName}   {skill.manaCost} MP";
        Deselect(); // Start as deselected
    }

    // New function that BattleManager will call
    public void Select()
    {
        skillText.color = highlightColor;
    }

    // New function that BattleManager will call
    public void Deselect()
    {
        skillText.color = normalColor;
    }
}