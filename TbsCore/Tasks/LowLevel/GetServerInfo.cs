﻿using System.Threading.Tasks;
using TbsCore.Helpers;
using TbsCore.Models.AccModels;

namespace TbsCore.Tasks.LowLevel
{
    public class GetServerInfo : BotTask
    {
        public override async Task<TaskRes> Execute(Account acc)
        {
            await NavigationHelper.ToDorf2(acc);

            // Get Map size
            var size = DriverHelper.GetJsObj<long>(acc, "window.TravianDefaults.Map.Size.top");
            acc.AccInfo.MapSize = (int)size;

            // Get server speed
            var speed = DriverHelper.GetJsObj<long>(acc, "Travian.Game.speed");
            acc.AccInfo.ServerSpeed = (int)speed;

            // Get server version
            acc.AccInfo.ServerVersion = (acc.Wb.Html.GetElementbyId("sidebarBoxDailyquests") == null ? Classificator.ServerVersionEnum.T4_5 : Classificator.ServerVersionEnum.TTwars);

            return TaskRes.Executed;
        }
    }
}