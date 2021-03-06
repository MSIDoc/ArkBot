﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot.WebApi.Model
{
    public class PlayerServerViewModel
    {
        public PlayerServerViewModel()
        {
            Creatures = new List<TamedCreatureViewModel>();
        }

        public string ClusterKey { get; set; }
        public string SteamId { get; set; }
        public string CharacterName { get; set; }
        public string Gender { get; set; }
        public string TribeName { get; set; }
        public int? TribeId { get; set; }
        public int? Level { get; set; }
        public int? EngramPoints { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public float? TopoMapX { get; set; }
        public float? TopoMapY { get; set; }
        public DateTime SavedAt { get; set; }
        public List<TamedCreatureViewModel> Creatures { get; set; }
    }
}
