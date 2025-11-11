using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for .ToDictionary

public class AssetManager : MonoBehaviour
{
    public static AssetManager instance;

    // Dictionaries for fast, string-based lookup
    public Dictionary<string, ItemData> itemDatabase;
    public Dictionary<string, SkillData> skillDatabase;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Load all assets and build the databases
            LoadItemData();
            LoadSkillData();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void LoadItemData()
    {
        // 1. Load all 'ItemData' assets from the "Resources/Items" folder
        var items = Resources.LoadAll<ItemData>("Items");

        // 2. Convert the list into a Dictionary using the 'itemName' as the key
        // This makes it super fast to find an item by its name.
        itemDatabase = items.ToDictionary(item => item.itemName, item => item);

        Debug.Log($"Loaded {itemDatabase.Count} items into the database.");
    }

    private void LoadSkillData()
    {
        // 1. Load all 'SkillData' assets from the "Resources/Skills" folder
        var skills = Resources.LoadAll<SkillData>("Skills");

        // 2. Convert to a Dictionary using 'skillName' as the key
        skillDatabase = skills.ToDictionary(skill => skill.skillName, skill => skill);

        Debug.Log($"Loaded {skillDatabase.Count} skills into the database.");
    }

    // --- Public Helper Functions ---

    public ItemData GetItemByName(string itemName)
    {
        if (itemDatabase.TryGetValue(itemName, out ItemData item))
        {
            return item;
        }
        Debug.LogError($"Item not found in database: {itemName}");
        return null;
    }

    public SkillData GetSkillByName(string skillName)
    {
        if (skillDatabase.TryGetValue(skillName, out SkillData skill))
        {
            return skill;
        }
        Debug.LogError($"Skill not found in database: {skillName}");
        return null;
    }
}