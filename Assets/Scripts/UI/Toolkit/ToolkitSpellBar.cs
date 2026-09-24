using System.Collections.Generic;

// The healer's spell cards, cast through the legacy skill slots
public class ToolkitSpellBar
{
    ToolkitGameContext _context;
    ToolkitGameView _view;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        _context = context;
        _view = view;
    }

    public void Refresh(bool canCast)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        Character character = _context.player.character;
        if (character != null)
        {
            foreach (CharacterSkillSlot slot in character.skillSlots)
            {
                if (slot == null || slot.data == null)
                {
                    continue;
                }

                ToolkitCardModel model = new ToolkitCardModel();
                model.key = slot.GetEntityId().ToString();
                model.iconSource = slot.data;
                model.title = slot.data.name;
                model.description = TextConvertor.Convert(slot.data.description, character, slot.data);
                model.status = LegacyUiReader.SkillStatus(slot);
                model.isEnabled = canCast && LegacyUiReader.CanUse(slot);
                model.source = slot;
                model.activate = OnSpellActivated;
                models.Add(model);
            }
        }

        _view.SetCards("spell-list", models);
    }

    void OnSpellActivated(ToolkitCardModel model)
    {
        ((CharacterSkillSlot)model.source).UseSkill();
    }
}
