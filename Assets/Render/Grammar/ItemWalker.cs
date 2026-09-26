using System.Collections.Generic;

namespace HealerLike.Render.Grammar
{
    // Walks the items of an EntityData: the handlers they put on the unit and the behaviours they give its shots
    public static class ItemWalker
    {
        // The handlers the unit's own items put on it at Init, its passives
        public static List<ABuffHandlerFactory> Buffs(EntityData data)
        {
            List<ABuffHandlerFactory> handlers = new List<ABuffHandlerFactory>();
            foreach (ItemData item in Items(data))
            {
                AddHandlers(handlers, item.buffs);
            }
            return handlers;
        }

        // The handlers the unit's own items apply on every hit it deals
        public static List<ABuffHandlerFactory> OnHitEffects(EntityData data)
        {
            List<ABuffHandlerFactory> handlers = new List<ABuffHandlerFactory>();
            foreach (ItemData item in Items(data))
            {
                AddHandlers(handlers, item.onHitEffects);
            }
            return handlers;
        }

        // The data of every item the unit carries in its EntityData, in the order the data lists them
        static List<ItemData> Items(EntityData data)
        {
            List<ItemData> items = new List<ItemData>();
            if (data == null || data.items == null)
            {
                return items;
            }

            foreach (AItemFactory itemFactory in data.items)
            {
                ItemFactory item = itemFactory as ItemFactory;
                if (item != null && item.data != null)
                {
                    items.Add(item.data);
                }
            }
            return items;
        }

        static void AddHandlers(List<ABuffHandlerFactory> handlers, List<ABuffHandlerFactory> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (ABuffHandlerFactory handler in source)
            {
                if (handler != null)
                {
                    handlers.Add(handler);
                }
            }
        }

        // Bounces installed by the unit's own items
        public static int Bounces(EntityData data)
        {
            int bounces = 0;
            foreach (AProjectileBehaviourFactory behaviour in Behaviours(data))
            {
                if (behaviour is BounceProjectileBehaviourFactory bounce && bounce.data != null)
                {
                    bounces += bounce.data.bounce;
                }
            }
            return bounces;
        }

        // Behaviours the unit's own items add to its projectiles, through a buff or their projectile behaviour list
        public static List<AProjectileBehaviourFactory> Behaviours(EntityData data)
        {
            List<ABuffHandlerFactory> handlers = Buffs(data);
            foreach (ItemData item in Items(data))
            {
                AddHandlers(handlers, item.projectileBehaviours);
            }

            List<AProjectileBehaviourFactory> behaviours = new List<AProjectileBehaviourFactory>();
            foreach (ABuffHandlerFactory handler in handlers)
            {
                foreach (ABuffFactory buff in EffectDerivation.Buffs(handler))
                {
                    if (buff is ProjectileBehaviourBuffFactory projectileBuff
                        && projectileBuff.data != null
                        && projectileBuff.data.projectileBehaviour != null)
                    {
                        behaviours.Add(projectileBuff.data.projectileBehaviour);
                    }
                }
            }
            return behaviours;
        }
    }
}
