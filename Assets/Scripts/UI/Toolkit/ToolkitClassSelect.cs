using System;
using System.Collections.Generic;
using UnityEngine;

// The menu's class choice: Start opens it, a class card (or the Random card) records the pick in
// CharacterSelection and continues to the expedition, Back returns to the menu. The classes are the game data's
// characters, the same list Main's DataManager plays, so the pick is honoured instead of replaced by a random one.
public class ToolkitClassSelect
{
    public static readonly string PanelName = "class-panel";
    public static readonly string ListName = "class-list";
    public static readonly string BackButtonName = "class-back-button";
    public static readonly string RandomTitle = "Random";

    // The Random card's icon: a procedural data icon labelled Random
    public class RandomChoice : IDataIconMetadata
    {
        public string label { get { return RandomTitle; } }
        public Sprite icon { get { return null; } }
    }

    readonly RandomChoice _random = new RandomChoice();
    readonly List<CharacterData> _characters = new List<CharacterData>();
    ToolkitGameView _view;
    Action<CharacterData> _chosen;
    bool _isOpen;

    public bool isOpen { get { return _isOpen; } }

    public IReadOnlyList<CharacterData> characters { get { return _characters; } }

    // Picks the index of the Random card, UnityEngine.Random unless a test supplies its own
    public Func<int, int> randomIndex = count => UnityEngine.Random.Range(0, count);

    // The classes a game data offers, in its order, without empty entries
    public static List<CharacterData> Offered(GameData data)
    {
        List<CharacterData> offered = new List<CharacterData>();
        if (data == null || data.characters == null)
        {
            return offered;
        }

        foreach (CharacterData character in data.characters)
        {
            if (character != null)
            {
                offered.Add(character);
            }
        }

        return offered;
    }

    // The whole card text as plain lines: Julien's select card content (description, skills, starting items,
    // deployable units)
    public static string Describe(CharacterData character)
    {
        List<string> lines = new List<string>();
        string role = Role(character);
        if (!string.IsNullOrEmpty(role))
        {
            lines.Add(role);
        }

        string details = Details(character);
        if (!string.IsNullOrEmpty(details))
        {
            lines.Add(details);
        }

        return string.Join("\n", lines);
    }

    // The card's lead line under the class name: what the class plays like, Julien's description
    public static string Role(CharacterData character)
    {
        return CharacterCardText.GetDescription(character);
    }

    // The kit in the card's lighter block, one line each: skills, starting items with their effect, units
    public static string Details(CharacterData character)
    {
        List<string> lines = new List<string>();
        string skills = Inline(CharacterCardText.GetSkills(character));
        if (!string.IsNullOrEmpty(skills))
        {
            lines.Add("Skills: " + skills);
        }

        List<string> items = new List<string>();
        if (character.items != null)
        {
            foreach (AItemFactory item in character.items)
            {
                if (item != null)
                {
                    string effect = item.GetItem().description;
                    items.Add(string.IsNullOrEmpty(effect) ? item.title : item.title + " (" + effect + ")");
                }
            }
        }

        if (items.Count > 0)
        {
            lines.Add("Starts with: " + string.Join(", ", items));
        }

        string units = CharacterCardText.GetUnits(character);
        if (!string.IsNullOrEmpty(units))
        {
            lines.Add("Units: " + units);
        }

        return string.Join("\n", lines);
    }

    // The kit's line labels, which the card tints so each line starts where the eye can find it
    public static readonly string[] DetailKeys = { "Skills:", "Starts with:", "Units:" };
    public static readonly string DetailKeyColor = "#BDDA9C";

    // The card's rich text of Details: each line's label in the theme's accent green
    public static string MarkDetails(string details)
    {
        if (string.IsNullOrEmpty(details))
        {
            return details;
        }

        List<string> lines = new List<string>();
        foreach (string line in details.Split('\n'))
        {
            string marked = line;
            foreach (string key in DetailKeys)
            {
                if (line.StartsWith(key, StringComparison.Ordinal))
                {
                    marked = "<color=" + DetailKeyColor + ">" + key + "</color>" + line.Substring(key.Length);
                    break;
                }
            }

            lines.Add(marked);
        }

        return string.Join("\n", lines);
    }

    // Julien's one-per-line list ("- A\n- B") on one line
    static string Inline(string lines)
    {
        if (string.IsNullOrEmpty(lines))
        {
            return lines;
        }

        List<string> names = new List<string>();
        foreach (string line in lines.Split('\n'))
        {
            string name = line.StartsWith("- ", StringComparison.Ordinal) ? line.Substring(2) : line;
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return string.Join(", ", names);
    }

    public void Init(ToolkitGameView view, Action<CharacterData> chosen)
    {
        _view = view;
        _chosen = chosen;
        _isOpen = false;
        if (_view != null)
        {
            _view.Show(PanelName, false);
        }
    }

    // Shows the classes of the data. False, and nothing shown, when the data offers none: the caller then keeps
    // its old direct start.
    public bool Open(GameData data)
    {
        _characters.Clear();
        _characters.AddRange(Offered(data));
        if (_characters.Count == 0)
        {
            Close();
            return false;
        }

        _isOpen = true;
        if (_view != null)
        {
            _view.SetCards(ListName, BuildModels());
            _view.Show(PanelName, true);
        }

        return true;
    }

    public void Close()
    {
        _isOpen = false;
        if (_view != null)
        {
            _view.Show(PanelName, false);
        }
    }

    public List<ToolkitCardModel> BuildModels()
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (CharacterData character in _characters)
        {
            models.Add(new ToolkitCardModel
            {
                key = character.name,
                source = character,
                iconSource = character,
                title = character.title,
                description = Role(character),
                details = MarkDetails(Details(character)),
                status = "Choose " + character.title,
                activate = OnCardActivated
            });
        }

        models.Add(new ToolkitCardModel
        {
            key = RandomTitle,
            source = _random,
            iconSource = _random,
            title = RandomTitle,
            description = "Let fate choose one of the " + _characters.Count + " classes.",
            status = "Choose at random",
            activate = OnCardActivated
        });
        return models;
    }

    // Records the pick and continues. A class outside the offered list is refused: Main would replace it anyway.
    public bool Choose(CharacterData character)
    {
        if (!_isOpen || character == null || !_characters.Contains(character))
        {
            return false;
        }

        CharacterSelection.selected = character;
        Close();
        if (_chosen != null)
        {
            _chosen.Invoke(character);
        }

        return true;
    }

    public bool ChooseRandom()
    {
        if (!_isOpen || _characters.Count == 0)
        {
            return false;
        }

        int index = Mathf.Clamp(randomIndex(_characters.Count), 0, _characters.Count - 1);
        return Choose(_characters[index]);
    }

    void OnCardActivated(ToolkitCardModel model)
    {
        if (model.source is RandomChoice)
        {
            ChooseRandom();
        }
        else
        {
            Choose(model.source as CharacterData);
        }
    }
}
