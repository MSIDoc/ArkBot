﻿using ArkBot.Ark;
using ArkBot.Data;
using ArkBot.ViewModel;
using ArkBot.WebApi.Model;
using ArkSavegameToolkitNet.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ArkBot.WebApi.Controllers
{
    public class PlayerController : ApiController
    {
        private ArkContextManager _contextManager;

        public PlayerController(ArkContextManager contextManager)
        {
            _contextManager = contextManager;
        }

        /// <param name="id">steamId</param>
        /// <returns></returns>
        public PlayerViewModel Get(string id)
        {
            var result = new PlayerViewModel
            {
            };

            var players = _contextManager.Servers.ToDictionary(x => x.Config.Key, x => x.Players?.FirstOrDefault(y => y.SteamId.Equals(id, StringComparison.OrdinalIgnoreCase)));
            foreach (var context in _contextManager.Servers)
            {
                PlayerServerViewModel vm = null;

                var player = players[context.Config.Key];
                if (player == null) vm = BuildViewModelForTransferedPlayer(context, id, players.Values.Where(x => x != null).Select(x => x.Id).ToArray()); //player have local profile on other server
                else vm = BuildViewModelForPlayer(context, player); //player with local profile

                if (vm == null) continue;

                result.Servers.Add(context.Config.Key, vm);
                result.MapNames.Add(context.Config.Key, context.SaveState?.MapName);
            }

            foreach (var context in _contextManager.Clusters)
            {
                var cloudInventory = context.CloudInventories?.FirstOrDefault(x => x.SteamId.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (cloudInventory == null) continue;

                var vm = BuildClusterViewModelForPlayer(context, cloudInventory);
                if (vm == null) continue;

                result.Clusters.Add(context.Config.Key, vm);
            }

            return result;
        }

        internal static PlayerServerViewModel BuildViewModelForTransferedPlayer(ArkServerContext context, string steamId, ulong[] playerIds)
        {
            if (playerIds == null || playerIds.Length == 0) return null;

            //there will be no player profile so most data cannot be set
            //a tribe where the player is a member may exist tho

            var tribe = context.Tribes?.FirstOrDefault(x => playerIds.Any(y => x.MemberIds.Contains((int)y)));
            if (tribe == null) return null;
            var playerId = playerIds.First(x => tribe.MemberIds.Contains((int)x));

            var vm = new PlayerServerViewModel
            {
                ClusterKey = context.Config.Key,
                SteamId = steamId,
                TribeId = tribe.Id,
                TribeName = tribe.Name,
                SavedAt = tribe.SavedAt
            };

            vm.Creatures.AddRange(BuildCreatureViewModelsForPlayerId(context, playerId));

            return vm;
        }

        internal static PlayerServerViewModel BuildViewModelForPlayer(ArkServerContext context, ArkPlayer player)
        {
            var vm = new PlayerServerViewModel
            {
                ClusterKey = context.Config.Key,
                SteamId = player.SteamId,
                CharacterName = player.CharacterName,
                Gender = player.Gender.ToString(),
                Level = player.CharacterLevel,
                Latitude = player.Location?.Latitude,
                Longitude = player.Location?.Longitude,
                TopoMapX = player.Location?.TopoMapX,
                TopoMapY = player.Location?.TopoMapY,
                EngramPoints = player.TotalEngramPoints,
                TribeId = player.TribeId,
                TribeName = player.TribeId.HasValue ? context.Tribes.FirstOrDefault(x => x.Id == player.TribeId.Value)?.Name : null,
                SavedAt = player.SavedAt
            };

            vm.Creatures.AddRange(BuildCreatureViewModelsForPlayerId(context, player.Id));
            
            return vm;
        }

        internal static List<TamedCreatureViewModel> BuildCreatureViewModelsForPlayerId(ArkServerContext context, ulong playerId)
        {
            var result = new List<TamedCreatureViewModel>();
            if (context.TamedCreatures != null)
            {
                var playercreatures = context.NoRafts.Where(x => (ulong)x.TargetingTeam == playerId || (x.OwningPlayerId.HasValue && (ulong)x.OwningPlayerId == playerId)).ToArray();
                var tribe = context.Tribes?.FirstOrDefault(x => x.MemberIds.Contains((int)playerId));
                var tribecreatures = tribe != null ? context.NoRafts.Where(x => x.TargetingTeam == tribe.Id && !playercreatures.Any(y => y.Id == x.Id)).ToArray() : new ArkTamedCreature[] { };
                foreach (var item in playercreatures.Select(x => new { c = x, o = "player" }).Concat(tribecreatures.Select(x => new { c = x, o = "tribe" })))
                {

                    var currentFood = item.c.CurrentStatusValues?.Length > 4 ? item.c.CurrentStatusValues[4] : null;
                    var maxFood = item.c.BaseStats?.Length > 4 && item.c.TamedStats?.Length > 4 ?
                        ArkContext.CalculateMaxStat(
                            ArkSpeciesStatsData.Stat.Food,
                            item.c.ClassName,
                            item.c.BaseStats[4],
                            item.c.TamedStats[4],
                            (decimal)(item.c.DinoImprintingQuality ?? 0f),
                            (decimal)(item.c.TamedIneffectivenessModifier ?? 0f)) : null;

                    //baby food formula: max * 0.1 + (max - (max * 0.1)) * age
                    if (maxFood.HasValue && item.c.BabyAge.HasValue) maxFood = maxFood.Value * 0.1 + (maxFood.Value - (maxFood.Value * 0.1)) * item.c.BabyAge.Value;

                    var foodStatus = currentFood.HasValue && maxFood.HasValue ? currentFood.Value / (float)maxFood.Value : (float?)null;
                    if (foodStatus.HasValue && foodStatus > 1f) foodStatus = 1f;

                    var aliases = ArkSpeciesAliases.Instance.GetAliases(item.c.ClassName);
                    var vmc = new TamedCreatureViewModel
                    {
                        Name = item.c.Name,
                        ClassName = item.c.ClassName,
                        Species = aliases?.FirstOrDefault(),
                        Aliases = aliases?.Skip(2).ToArray() ?? new string[] { }, //skip primary name and class name
                        Gender = item.c.Gender.ToString(),
                        BaseLevel = item.c.BaseLevel,
                        Level = item.c.Level,
                        BabyAge = item.c.IsBaby ? item.c.BabyAge : null,
                        Imprint = item.c.DinoImprintingQuality,
                        FoodStatus = foodStatus,
                        Latitude = item.c.Location?.Latitude,
                        Longitude = item.c.Location?.Longitude,
                        TopoMapX = item.c.Location?.TopoMapX,
                        TopoMapY = item.c.Location?.TopoMapY,
                        NextMating = item.c.NextAllowedMatingTimeApprox,
                        BabyNextCuddle = item.c.BabyNextCuddleTimeApprox,
                        OwnerType = item.o
                    };
                    result.Add(vmc);
                }
            }

            return result;
        }

        internal static PlayerClusterViewModel BuildClusterViewModelForPlayer(ArkClusterContext context, ArkCloudInventory cloudInventory)
        {
            var vm = new PlayerClusterViewModel();

            foreach (var c in cloudInventory.Dinos)
            {
                var aliases = ArkSpeciesAliases.Instance.GetAliases(c.ClassName);
                var vmc = new CloudCreatureViewModel
                {
                    Name = c.Name,
                    ClassName = c.ClassName,
                    Species = aliases?.FirstOrDefault(),
                    Aliases = aliases?.Skip(2).ToArray() ?? new string[] { }, //skip primary name and class name
                    Level = c.Level
                };
                vm.Creatures.Add(vmc);
            }

            return vm;
        }
    }
}
