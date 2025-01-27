﻿using System;
using System.Collections.Generic;
using System.Text;
using TbsCore.Helpers;

namespace TbsCore.Models.CombatModels
{
    public class CombatDeffender
    {
        /// <summary>
        /// List of (Troops, hero, tribe), since deffender can have armies from multiple accounts / villages
        /// </summary>
        public List<CombatArmy> Armies { get; set; }
        
        /// <summary>
        /// Population of the deffender, morale bonus depends on it
        /// </summary>
        public int Population { get; set; }
        
        /// <summary>
        /// Level of the wall inside the village
        /// </summary>
        public int WallLevel { get; set; }

        /// <summary>
        /// Level of Palace / Residence
        /// </summary>
        public int PalaceLevel { get; set; }
        
        /// <summary>
        /// Which tribe is the deffender. Wall bonus depends on the tribe
        /// </summary>
        public Classificator.TribeEnum DeffTribe { get; set; }

        /// <summary>
        /// Ally metalurgy percentage (2% => 2, 4% => 4)
        /// </summary>
        public int Metalurgy { get; set; }

        public CombatPoints GetDeffense()
        {
            CombatPoints ret = CombatPoints.Zero();
            foreach (var army in Armies)
            {
                ret.Add(army.GetDeffense());
            }
            return ret;
        }

    }
}
