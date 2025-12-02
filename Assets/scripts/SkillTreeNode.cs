using UnityEngine;
using UnityEngine.UI;

public class SkillTreeNode : MonoBehaviour
{
    public enum SkillStatus { Locked, Available, Unlocked }

    [Header("Skill Data")]
    public SkillData skillData;

    [Header("Node Connections")]
    public SkillTreeNode nodeUp;
    public SkillTreeNode nodeDown;
    public SkillTreeNode nodeLeft;
    public SkillTreeNode nodeRight;

    [Header("Node State")]
    public bool isStarterSkill = false;

    [Header("UI")]
    public GameObject highlight;
    public Image iconImage;
    public GameObject lockedOverlay;
    public GameObject availableBorder;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnNodeClicked);
    }

    // This function checks the player's stats and returns the node's current state
    private SkillStatus GetStatus(PlayerStats player)
    {
        // 1. Is it already unlocked?
        if (player.unlockedSkills.Contains(skillData))
        {
            return SkillStatus.Unlocked;
        }

        // 2. Is it a starter skill? (It's available if not unlocked)
        if (isStarterSkill)
        {
            return SkillStatus.Available;
        }

        // 3. Is an adjacent, connected node unlocked?
        if (nodeUp != null && player.unlockedSkills.Contains(nodeUp.skillData))
        {
            return SkillStatus.Available;
        }
        if (nodeDown != null && player.unlockedSkills.Contains(nodeDown.skillData))
        {
            return SkillStatus.Available;
        }
        if (nodeLeft != null && player.unlockedSkills.Contains(nodeLeft.skillData))
        {
            return SkillStatus.Available;
        }
        if (nodeRight != null && player.unlockedSkills.Contains(nodeRight.skillData))
        {
            return SkillStatus.Available;
        }

        // 4. If none of the above, it's locked
        return SkillStatus.Locked;
    }

    // This is called by CharacterMenuUI to refresh the visuals
    public void UpdateNodeVisuals(PlayerStats player)
    {
        // Get the current status
        SkillStatus status = GetStatus(player);

        // Update visuals based on the 3 states
        switch (status)
        {
            case SkillStatus.Unlocked:
                iconImage.color = Color.white;
                lockedOverlay.SetActive(false);
                availableBorder.SetActive(false);
                break;

            case SkillStatus.Available:
                iconImage.color = Color.gray;
                lockedOverlay.SetActive(false);
                availableBorder.SetActive(true);
                break;

            case SkillStatus.Locked:
                iconImage.color = Color.gray;
                lockedOverlay.SetActive(true);
                availableBorder.SetActive(false);
                break;
        }
    }

    public void OnNodeClicked()
    {
        PlayerStats player = PlayerStats.instance;

        // Get the status
        SkillStatus status = GetStatus(player);

        if (status == SkillStatus.Available)
        {
            // Try to buy the skill
            if (player.UnlockSkill(skillData))
            {
                AudioManager.instance.PlaySFX("MenuConfirm");
                // OnStatsChanged will auto-refresh visuals
            }
            else
            {
                AudioManager.instance.PlaySFX("MenuDenied");
            }
        }
        else if (status == SkillStatus.Unlocked)
        {
            Debug.Log("Already unlocked: " + skillData.skillName);
        }
        else // (status == SkillStatus.Locked)
        {
            AudioManager.instance.PlaySFX("MenuDenied");
        }
    }

    public void SelectNode()
    {
        highlight.SetActive(true);
    }

    public void DeselectNode()
    {
        highlight.SetActive(false);
    }
}