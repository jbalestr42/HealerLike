using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSkillInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryContainer;
    [SerializeField] GameObject _inventoryCharacterSkillPrefab;

    List<UseCharacterSkillButton> _characterSkillButtons = new List<UseCharacterSkillButton>();

	public UseCharacterSkillButton Create()
    {
        var characterSkillButton = Instantiate(_inventoryCharacterSkillPrefab, _inventoryContainer.transform);
        UseCharacterSkillButton skillButton = characterSkillButton.GetComponent<UseCharacterSkillButton>();
        _characterSkillButtons.Add(skillButton);

        return skillButton;
	}

    public void Show(bool show)
    {
        _inventoryContainer.SetActive(show);
    }
}
