using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // The playable characters are the ones of the game data
    [SerializeField] GameData _gameData;

    CharacterSelectPanel _characterSelectPanel;

    // The character select screen is the first screen, the run starts once a character is picked
    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (_characterSelectPanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            // Read each time the menu opens, so a level unlocked by the last run can be picked
            AscensionProgress progress = AscensionProgress.Load(AscensionProgress.defaultPath);
            _characterSelectPanel = CharacterSelectPanel.Create(canvas.transform, _gameData.characters, OnCharacterChosen, null, _gameData.ascension,
                character => progress.GetUnlockedLevel(AscensionProgress.GetKey(character)));
        }
        _characterSelectPanel.gameObject.SetActive(true);
    }

    void OnCharacterChosen(CharacterData character, int ascensionLevel)
    {
        CharacterSelection.selected = character;
        CharacterSelection.ascensionLevel = ascensionLevel;
        SceneManager.LoadScene("Main");
    }
}
