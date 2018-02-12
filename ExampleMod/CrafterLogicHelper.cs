﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExampleMod
{
    public static class CrafterLogicHelper
    {
        readonly static float MaxDistance = 100f;
        readonly static float MaxDistanceSq = MaxDistance * MaxDistance;

        static double lastContainerCheckTime = 0.0;

        static ItemsContainer[] nearbyItemContainers;

        public static ItemsContainer[] FindAllItemsContainersInRange()
        {
            if (DayNightCycle.main.timePassed > 1.0 + lastContainerCheckTime || nearbyItemContainers == null)
            {
                StorageContainer[] array = UnityEngine.Object.FindObjectsOfType<StorageContainer>();
                List<ItemsContainer> list = new List<ItemsContainer>();
                foreach (StorageContainer storageContainer in array)
                {
                    if (storageContainer.container != null && (Player.main.transform.position - storageContainer.transform.position).sqrMagnitude < MaxDistanceSq)
                    {
                        list.Add(storageContainer.container);
                    }
                }
                list.Add(Inventory.main.container);
                nearbyItemContainers = list.ToArray();
                lastContainerCheckTime = DayNightCycle.main.timePassed;
            }
            return nearbyItemContainers;
        }

        public static int GetTotalPickupCount(TechType techType, ItemsContainer[] itemContainers)
        {
            int num = 0;
            foreach (ItemsContainer itemsContainer in itemContainers)
            {
                num += itemsContainer.GetCount(techType);
            }
            return num;
        }

        public static bool DestroyItemInContainers(TechType techType, ItemsContainer[] itemsContainers)
        {
            for (int i = 0; i < itemsContainers.Length; i++)
            {
                if (itemsContainers[i].DestroyItem(techType))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool DestroyItemInLocalContainers(TechType techType)
        {
            var itemsContainers = FindAllItemsContainersInRange();

            return DestroyItemInContainers(techType, itemsContainers);
        }

        public static bool IsCraftRecipeFulfilled(TechType techType)
        {
            if (Inventory.main == null)
            {
                return false;
            }
            if (!GameModeUtils.RequiresIngredients())
            {
                return true;
            }

            var itemContainers = FindAllItemsContainersInRange();
            ITechData techData = CraftData.Get(techType, false);
            if (techData != null)
            {
                int i = 0;
                int ingredientCount = techData.ingredientCount;
                while (i < ingredientCount)
                {
                    IIngredient ingredient = techData.GetIngredient(i);
                    if (GetTotalPickupCount(ingredient.techType, itemContainers) < ingredient.amount)
                    {
                        return false;
                    }
                    i++;
                }
                return true;
            }
            return false;
        }

        public static bool ConsumeResources(TechType techType)
        {
            if (!IsCraftRecipeFulfilled(techType))
            {
                ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
                return false;
            }

            var itemsContainers = FindAllItemsContainersInRange();
            ITechData techData = CraftData.Get(techType, false);
            if (techData == null)
            {
                return false;
            }
            int i = 0;
            int ingredientCount = techData.ingredientCount;
            while (i < ingredientCount)
            {
                IIngredient ingredient = techData.GetIngredient(i);
                TechType techType2 = ingredient.techType;
                int j = 0;
                int amount = ingredient.amount;
                while (j < amount)
                {
                    DestroyItemInContainers(techType2, itemsContainers);
                    uGUI_IconNotifier.main.Play(techType2, uGUI_IconNotifier.AnimationType.To, null);
                    j++;
                }
                i++;
            }
            return true;
        }

        public static void WriteIngredients(ITechData data, List<TooltipIcon> icons)
        {
            int ingredientCount = data.ingredientCount;
            ItemsContainer[] itemContainers = FindAllItemsContainersInRange();
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < ingredientCount; i++)
            {
                stringBuilder.Length = 0;
                IIngredient ingredient = data.GetIngredient(i);
                TechType techType = ingredient.techType;
                int totalPickupCount = GetTotalPickupCount(techType, itemContainers);
                int amount = ingredient.amount;
                bool flag = totalPickupCount >= amount || !GameModeUtils.RequiresIngredients();
                Atlas.Sprite sprite = SpriteManager.Get(techType);
                if (flag)
                {
                    stringBuilder.Append("<color=#94DE00FF>");
                }
                else
                {
                    stringBuilder.Append("<color=#DF4026FF>");
                }
                string orFallback = Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType);
                stringBuilder.Append(orFallback);
                if (amount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(amount);
                }
                if (totalPickupCount > 0 && totalPickupCount < amount)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(totalPickupCount);
                    stringBuilder.Append(")");
                }
                stringBuilder.Append("</color>");
                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }
        }
    }
}