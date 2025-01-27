﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TbsCore.Helpers;
using TbsCore.Models.AccModels;
using TbsCore.Models.MapModels;
using TbsCore.Models.ResourceModels;
using TbsCore.Models.VillageModels;
using TbsCore.Parsers;
using TbsCore.Tasks.LowLevel;
using TbsCore.TravianData;
using static TbsCore.Helpers.Classificator;

namespace TbsCore.Helpers
{
    public static class HeroHelper
    {
        /// <summary>
        /// Calculates if there is any adventure in the range of the home village.
        /// </summary>
        /// <param name="acc">Account</param>
        /// <returns>Whether there are adventures in range</returns>
        public static bool AdventureInRange(Account acc)
        {
            var heroHome = GetHeroHomeVillage(acc);
            if (heroHome == null) return false;

            return acc.Hero.Adventures.Any(x =>
                heroHome.Coordinates.CalculateDistance(acc, x.Coordinates) <= acc.Hero.Settings.MaxDistance
            );
        }

        /// <summary>
        /// Gets the hero home village
        /// </summary>
        /// <param name="acc">Account</param>
        /// <returns>Hero home village</returns>
        public static Village GetHeroHomeVillage(Account acc) =>
            acc.Villages.FirstOrDefault(x => x.Id == acc.Hero.HomeVillageId);

        /// <summary>
        /// Auto equip hero if there is better equipment available
        /// </summary>
        /// <param name="acc">Account</param>
        public static void AutoEquipHero(Account acc)
        {
            foreach (HeroItemCategory category
                in (HeroItemCategory[])Enum.GetValues(typeof(HeroItemCategory)))
            {
                if (category == HeroItemCategory.Others) continue; // Don't equip into hero bag
                int currentTier = 0;
                if (acc.Hero.Equipt.TryGetValue(category, out var item))
                {
                    // Hero already has an equipt item for this category
                    currentTier = GetHeroItemTier(item);
                }

                var equipWith = acc.Hero.Items
                    .Where(x => GetHeroItemCategory(x.Item) == category)
                    .OrderBy(x => GetHeroItemTier(x.Item))
                    .LastOrDefault();

                if (equipWith != null &&
                    GetHeroItemTier(equipWith.Item) > currentTier &&
                    acc.Hero.Status == Hero.StatusEnum.Home)
                {
                    acc.Tasks.Add(new HeroEquip()
                    {
                        ExecuteAt = DateTime.Now,
                        Items = new List<(HeroItemEnum, int)>()
                        {
                            (equipWith.Item, 0)
                        }
                    }, true);
                }
            }
        }

        /// <summary>
        /// Will parse HeroItemEnum into category, name and tier
        /// </summary>
        /// <param name="item">Hero item enum</param>
        /// <returns>Hero item (category, name, tier)</returns>
        public static (HeroItemCategory, string, int) ParseHeroItem(HeroItemEnum item)
        {
            var attr = item.ToString().Split('_');

            Enum.TryParse(attr[0], out HeroItemCategory category);
            string name = attr[1];
            int tier = int.Parse(attr[2]);

            return (category, name, tier);
        }

        /// <summary>
        /// Parses hero weapon
        /// </summary>
        /// <param name="item">Hero item</param>
        /// <returns>(Troop boost, boost)</returns>
        public static (TroopsEnum, int) ParseWeapon(HeroItemEnum item)
        {
            var (_, name, tier) = ParseHeroItem(item);
            if(Enum.TryParse(name, out TroopsEnum troop))
            {
                return (troop, GetWeaponBoost(troop, tier));
            }
            return (TroopsEnum.None, 0);
        }

        public static int GetArmorStrength(string name)
        {
            switch (name)
            {
                case "Breastplate": return 500;
                case "Segmented": return 250;
                default: return 0;
            }
        }

        public static int GetArmorDmgReduce(string name, int tier)
        {
            switch (name)
            {
                case "Scale": return 2 + 2 * tier;
                case "Segmented": return 2 + tier;
                default: return 0;
            }
        }

        private static int GetWeaponBoost(TroopsEnum troop, int tier)
        {
            var upkeep = TroopsData.GetTroopUpkeep(troop);
            return (tier + 2) * upkeep;
        }

        /// <summary>
        /// Gets the tier of the hero item
        /// </summary>
        /// <param name="item">HeroItem</param>
        /// <returns>Tier</returns>
        public static int GetHeroItemTier(HeroItemEnum item)
        {
            var (_, _, itemTier) = ParseHeroItem(item);
            return itemTier;
        }
        public static string GetHeroItemName(HeroItemEnum item)
        {
            var (_, name, _) = ParseHeroItem(item);
            return name;
        }
        public static HeroItemCategory GetHeroItemCategory(HeroItemEnum item)
        {
            var (category, _, _) = ParseHeroItem(item);
            return category;
        }

        /// <summary>
        /// Will parse all the useful data from the hero page (/hero.php)
        /// </summary>
        /// <param name="acc">Account</param>
        public static void ParseHeroPage(Account acc)
        {
            acc.Hero.HeroInfo = HeroParser.GetHeroInfo(acc.Wb.Html);
            acc.Hero.Items = HeroParser.GetHeroItems(acc.Wb.Html);
            acc.Hero.Equipt = HeroParser.GetHeroEquipment(acc.Wb.Html);
            acc.Hero.HeroArrival = DateTime.Now + HeroParser.GetHeroArrivalInfo(acc.Wb.Html);

            UpdateHeroVillage(acc);

            if (acc.Hero.Settings.AutoEquip)
            {
                AutoEquipHero(acc);
            }
        }

        public static void UpdateHeroVillage(Account acc)
        {
            var hrefId = HeroParser.GetHeroVillageHref(acc.Wb.Html);
            if (hrefId == null) return;

            switch (acc.AccInfo.ServerVersion)
            {
                case Classificator.ServerVersionEnum.TTwars:
                    acc.Hero.HomeVillageId = hrefId ?? 0;
                    return;
                case ServerVersionEnum.T4_5:
                    // Convert from coordinates id -> coordinates -> villageId
                    var coordinates = new Coordinates(acc, hrefId ?? 0);
                    var vill = acc.Villages.FirstOrDefault(x => x.Coordinates.Equals(coordinates));
                    if (vill == null) return;
                    acc.Hero.HomeVillageId = vill.Id;
                    return;
            }
        }

        public static long[] GetHeroResources(Account acc)
        {
            var heroItems = acc.Hero.Items;
            return new long[]
            {
                heroItems.FirstOrDefault(x => x.Item == HeroItemEnum.Others_Wood_0)?.Count ?? 0,
                heroItems.FirstOrDefault(x => x.Item == HeroItemEnum.Others_Clay_0)?.Count ?? 0,
                heroItems.FirstOrDefault(x => x.Item == HeroItemEnum.Others_Iron_0)?.Count ?? 0,
                heroItems.FirstOrDefault(x => x.Item == HeroItemEnum.Others_Crop_0)?.Count ?? 0
            };
        }

        /// <summary>
        /// Checks if bot should first switch hero helmets
        /// </summary>
        /// <param name="acc">Account</param>
        /// <param name="troop">Troop to train</param>
        /// <returns>Whether to switch helmets first</returns>
        public static bool SwitchHelmet(Account acc, Village trainVill, BuildingEnum building, TrainTroops task)
        {
            if (!acc.Hero.Settings.AutoSwitchHelmets) return false;

            // In T4.5, helmet will only have effect in hero home village
            // In TTWars, helmets have acc-wide effect
            // TODO: for T4.5, add auto-move hero feature (for helmet effect purposes)
            if (GetHeroHomeVillage(acc) != trainVill &&
                acc.AccInfo.ServerVersion != Classificator.ServerVersionEnum.TTwars) return false;

            string type = "";
            if (building == BuildingEnum.Barracks ||
                building == BuildingEnum.GreatBarracks) type = "Infantry";
            if (building == BuildingEnum.Stable ||
                building == BuildingEnum.GreatStable) type = "Cavalry";

            // No helmet helps us for training in workshop
            if (string.IsNullOrEmpty(type)) return false;

            var equipWith = acc.Hero.Items
                .Where(x => GetHeroItemName(x.Item) == type)
                .OrderBy(x => GetHeroItemTier(x.Item))
                .LastOrDefault();
            if (equipWith == null) return false; // No appropriate helmet to equip

            var (equipCategory, equipName, equipTier) = ParseHeroItem(equipWith.Item);

            if (acc.Hero.Equipt.TryGetValue(HeroItemCategory.Helmet, out var equiped))
            {
                var (category, name, tier) = ParseHeroItem(equiped);
                if (name == type &&
                    equipTier <= tier) return false; // We already have the correct helmet
            }

            acc.Tasks.Add(new HeroEquip()
            {
                ExecuteAt = acc.Hero.Status == Hero.StatusEnum.Home ? DateTime.Now : acc.Hero.HeroArrival,
                Items = new List<(Classificator.HeroItemEnum, int)>()
                {
                    (equipWith.Item, 0)
                },
                NextTask = task
            }, true);
            return true;
        }
    }
}