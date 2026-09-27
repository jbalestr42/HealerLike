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
            _characterSelectPanel = CharacterSelectPanel.Create(canvas.transform, _gameData.characters, OnCharacterChosen, null);
        }
        _characterSelectPanel.gameObject.SetActive(true);
    }

    void OnCharacterChosen(CharacterData character)
    {
        CharacterSelection.selected = character;
        SceneManager.LoadScene("Main");
    }
}
