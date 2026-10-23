using UnityEngine;

public class CharacterSkillInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryContainer;
    [SerializeField] GameObject _inventoryCharacterSkillPrefab;

    // A button using the skill of the slot
    public UseCharacterSkillButton Create(CharacterSkillSlot slot, Character character)
    {
        GameObject characterSkillButton = Instantiate(_inventoryCharacterSkillPrefab, _inventoryContainer.transform);
        UseCharacterSkillButton skillButton = characterSkillButton.GetComponent<UseCharacterSkillButton>();
        skillButton.Init(slot, character);
        return skillButton;
    }

    public void Show(bool show)
    {
        _inventoryContainer.SetActive(show);
    }
}
