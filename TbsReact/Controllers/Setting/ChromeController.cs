﻿using Microsoft.AspNetCore.Mvc;
using TbsReact.Models;
using TbsReact.Models.Setting;
using TbsReact.Singleton;

namespace TbsReact.Controllers.Setting
{
    [ApiController]
    [Route("settings/chrome/{indexAcc:int}")]
    public class ChromeController : ControllerBase
    {
        [HttpGet]
        public ActionResult<Chrome> GetSetting(int indexAcc)
        {
            var account = AccountData.GetAccount(indexAcc);

            if (account == null)
            {
                return NotFound();
            }
            var acc = AccountManager.GetAccount(account);

            var setting = new Chrome
            {
                Click = new Range
                {
                    Min = acc.Settings.DelayClickingMin,
                    Max = acc.Settings.DelayClickingMax,
                },
                DisableImages = acc.Settings.DisableImages,
                AutoClose = acc.Settings.AutoCloseDriver,
            };

            return Ok(setting);
        }

        [HttpPut]
        public ActionResult PutSetting(int indexAcc, [FromBody] Chrome setting)
        {
            var account = AccountData.GetAccount(indexAcc);

            if (account == null)
            {
                return NotFound();
            }
            var acc = AccountManager.GetAccount(account);

            acc.Settings.DelayClickingMin = setting.Click.Min;
            acc.Settings.DelayClickingMax = setting.Click.Max;
            acc.Settings.DisableImages = setting.DisableImages;
            acc.Settings.AutoCloseDriver = setting.AutoClose;

            return Ok();
        }
    }
}