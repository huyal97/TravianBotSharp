﻿using System.Collections.Generic;
using System.Linq;
using TbsCore.Models.TroopsModels;
using static TbsCore.Helpers.Classificator;

namespace TbsCore.Parsers
{
    public static class FarmlistParser
    {
        public static List<FarmList> ParseFL(HtmlAgilityPack.HtmlDocument htmlDoc, ServerVersionEnum version)
        {
            var list = new List<FarmList>();
            var raidList = htmlDoc.GetElementbyId("raidList");
            if (raidList == null) return null;

            switch (version)
            {
                case ServerVersionEnum.TTwars: return ParseT4_4(raidList);
                case ServerVersionEnum.T4_5: return ParseT4_5(raidList);
                default: return null;
            }
        }

        public static List<FarmList> ParseT4_5(HtmlAgilityPack.HtmlNode raidList)
        {
            List<FarmList> list = new List<FarmList>();
            var vills = raidList.ChildNodes.Where(x => x.HasClass("villageWrapper"));
            foreach (var vill in vills)
            {
                string villName = vill.Descendants("a").First(x => x.HasClass("villageName")).InnerText;
                var fls = vill.Descendants("div").Where(x => x.HasClass("raidList"));

                foreach (var fl in fls)
                {
                    FarmList farmList = new FarmList();
                    farmList.Enabled = true;
                    string FlName = fl.Descendants("div").First(x => x.HasClass("listName")).InnerText.Trim();
                    farmList.Name = (villName + " - " + FlName).Replace("\t", "").Replace("\n", "").Replace("\r", "");
                    farmList.Id = (int)Parser.RemoveNonNumeric(fl.GetAttributeValue("data-listid", ""));
                    farmList.NumOfFarms = (int)Parser.RemoveNonNumeric(fl.Descendants("span").First(x => x.HasClass("slotsCount")).InnerText);
                    list.Add(farmList);
                }
            }
            return list;
        }

        public static List<FarmList> ParseT4_4(HtmlAgilityPack.HtmlNode raidList)
        {
            List<FarmList> list = new List<FarmList>();
            var fls = raidList.ChildNodes.Where(x => x.Id.StartsWith("list"));
            foreach (var fl in fls)
            {
                FarmList farmList = new FarmList();
                farmList.Enabled = true;
                farmList.Name = fl.Descendants("div").First(x => x.HasClass("listTitleText")).InnerText.Trim();
                farmList.Id = (int)Parser.RemoveNonNumeric(fl.Id);
                farmList.NumOfFarms = (int)Parser.RemoveNonNumeric(fl.Descendants("span").First(x => x.HasClass("raidListSlotCount")).InnerText.Split('/')[0]);
                list.Add(farmList);
            }
            return list;
        }
    }
}