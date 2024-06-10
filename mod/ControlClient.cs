﻿/*
 * ControlValley
 * Stardew Valley Support for Twitch Crowd Control
 * Copyright (C) 2021 TerribleTable
 * LGPL v2.1
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
 * USA
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace ControlValley
{
    public class ControlClient
    {
        public static readonly string CV_HOST = "127.0.0.1";
        public static readonly int CV_PORT = 51337;

        private static readonly string[] _no_spawn = { "hospital", "islandsouth" };

        private Dictionary<string, CrowdDelegate> Delegate { get; set; }
        private IPEndPoint Endpoint { get; set; }
        private Dictionary<GameLocation, List<Monster>> Monsters { get; set; }
        private Queue<CrowdRequest> Requests { get; set; }
        private bool Running { get; set; }
        private bool Saving { get; set; }
        private bool Spawn { get; set; }

        private bool paused = false;
        public static Socket Socket { get; set; }

        public ControlClient()
        {
            Endpoint = new IPEndPoint(IPAddress.Parse(CV_HOST), CV_PORT);
            Monsters = new Dictionary<GameLocation, List<Monster>>();
            Requests = new Queue<CrowdRequest>();
            Running = true;
            Saving = false;
            Spawn = true;
            Socket = null;

            Delegate = new Dictionary<string, CrowdDelegate>()
            {
                {"horserace", CrowdDelegates.PlayHorseRace},
                {"horseraceend", CrowdDelegates.StopHorseRace},

                {"downgrade_axe", CrowdDelegates.DowngradeAxe},
                {"downgrade_boots", CrowdDelegates.DowngradeBoots},
                {"downgrade_fishingrod", CrowdDelegates.DowngradeFishingRod},
                {"downgrade_hoe", CrowdDelegates.DowngradeHoe},
                {"downgrade_pickaxe", CrowdDelegates.DowngradePickaxe},
                {"downgrade_trashcan", CrowdDelegates.DowngradeTrashCan},
                {"downgrade_wateringcan", CrowdDelegates.DowngradeWateringCan},
                {"downgrade_weapon", CrowdDelegates.DowngradeWeapon},
                {"energize_10", CrowdDelegates.Energize10},
                {"energize_25", CrowdDelegates.Energize25},
                {"energize_50", CrowdDelegates.Energize50},
                {"energize_full", CrowdDelegates.EnergizeFull},
                {"give_buff_adrenaline", CrowdDelegates.GiveBuffAdrenaline},
                {"give_buff_darkness", CrowdDelegates.GiveBuffDarkness},
                {"give_buff_frozen", CrowdDelegates.GiveBuffFrozen},
                {"give_buff_invincibility", CrowdDelegates.GiveBuffInvincibility},
                {"give_buff_nauseous", CrowdDelegates.GiveBuffNauseous},
                {"give_buff_slime", CrowdDelegates.GiveBuffSlime},
                {"give_buff_speed", CrowdDelegates.GiveBuffSpeed},
                {"give_buff_tipsy", CrowdDelegates.GiveBuffTipsy},
                {"give_buff_warrior", CrowdDelegates.GiveBuffWarrior},
                {"give_money_100", CrowdDelegates.GiveMoney100},
                {"give_money_1000", CrowdDelegates.GiveMoney1000},
                {"give_money_10000", CrowdDelegates.GiveMoney10000},
                {"give_stardrop", CrowdDelegates.GiveStardrop},
                {"heal_10", CrowdDelegates.Heal10},
                {"heal_25", CrowdDelegates.Heal25},
                {"heal_50", CrowdDelegates.Heal50},
                {"heal_full", CrowdDelegates.HealFull},
                {"hurt_10", CrowdDelegates.Hurt10},
                {"hurt_25", CrowdDelegates.Hurt25},
                {"hurt_50", CrowdDelegates.Hurt50},
                {"kill", CrowdDelegates.Kill},
                {"passout", CrowdDelegates.PassOut},
                {"remove_money_100", CrowdDelegates.RemoveMoney100},
                {"remove_money_1000", CrowdDelegates.RemoveMoney1000},
                {"remove_money_10000", CrowdDelegates.RemoveMoney10000},
                {"remove_stardrop", CrowdDelegates.RemoveStardrop},
                {"spawn_bat", CrowdDelegates.SpawnBat},
                {"spawn_slime", CrowdDelegates.SpawnGreenSlime},
                {"spawn_redslime", CrowdDelegates.SpawnRedSlime},
                {"spawn_frostjelly", CrowdDelegates.SpawnFrostJelly},
                {"spawn_redsludge", CrowdDelegates.SpawnRedSludge},
                {"spawn_bluesquid", CrowdDelegates.SpawnBlueSquid},
                {"spawn_skelton", CrowdDelegates.SpawnSkeleton},
                {"spawn_skeletonmage", CrowdDelegates.SpawnSkeletonMage},
                {"spawn_fly", CrowdDelegates.SpawnFly},
                {"spawn_frostbat", CrowdDelegates.SpawnFrostBat},
                {"spawn_ghost", CrowdDelegates.SpawnGhost},
                {"spawn_lavabat", CrowdDelegates.SpawnLavaBat},
                {"spawn_serpent", CrowdDelegates.SpawnSerpent},
                {"spawn_bomb", CrowdDelegates.SpawnBomb},


                {"tire_10", CrowdDelegates.Tire10},
                {"tire_25", CrowdDelegates.Tire25},
                {"tire_50", CrowdDelegates.Tire50},
                {"upgrade_axe", CrowdDelegates.UpgradeAxe},
                {"upgrade_backpack", CrowdDelegates.UpgradeBackpack},
                {"upgrade_boots", CrowdDelegates.UpgradeBoots},
                {"upgrade_fishingrod", CrowdDelegates.UpgradeFishingRod},
                {"upgrade_hoe", CrowdDelegates.UpgradeHoe},
                {"upgrade_pickaxe", CrowdDelegates.UpgradePickaxe},
                {"upgrade_trashcan", CrowdDelegates.UpgradeTrashCan},
                {"upgrade_wateringcan", CrowdDelegates.UpgradeWateringCan},
                {"upgrade_weapon", CrowdDelegates.UpgradeWeapon},
                {"warp_beach", CrowdDelegates.WarpBeach},
                {"warp_desert", CrowdDelegates.WarpDesert},
                {"warp_farm", CrowdDelegates.WarpFarm},
                {"warp_island", CrowdDelegates.WarpIsland},
                {"warp_mountain", CrowdDelegates.WarpMountain},
                {"warp_railroad", CrowdDelegates.WarpRailroad},
                {"warp_sewer", CrowdDelegates.WarpSewer},
                {"warp_tower", CrowdDelegates.WarpTower},
                {"warp_town", CrowdDelegates.WarpTown},
                {"warp_woods", CrowdDelegates.WarpWoods},
                {"give_sword", CrowdDelegates.GiveSword},
                {"give_cookie", CrowdDelegates.GiveCookie},
                {"give_supermeal", CrowdDelegates.GiveSuperMeal},
                {"give_diamond", CrowdDelegates.GiveDiamond},
                {"give_copperbar", CrowdDelegates.GiveCopperBar},
                {"give_ironbar", CrowdDelegates.GiveIronBar},
                {"give_goldbar", CrowdDelegates.GiveGoldBar},
                {"give_wood", CrowdDelegates.GiveWood},
                {"give_stone", CrowdDelegates.GiveStone},
                {"msg_santa", CrowdDelegates.SantaMSG},
                {"msg_car", CrowdDelegates.CarMSG},
                {"msg_pizza", CrowdDelegates.PizzaMSG},
                {"msg_grow", CrowdDelegates.GrowMSG},
                {"msg_lottery", CrowdDelegates.LotteryMSG},
                {"msg_tech", CrowdDelegates.TechMSG},
                {"hair_brown", CrowdDelegates.BrownHair},
                {"hair_blonde", CrowdDelegates.BlondeHair},
                {"hair_red", CrowdDelegates.RedHair},
                {"hair_green", CrowdDelegates.GreenHair},
                {"hair_blue", CrowdDelegates.BlueHair},
                {"hair_yellow", CrowdDelegates.YellowHair},
                {"hair_purple", CrowdDelegates.PurpleHair},
                {"hair_orange", CrowdDelegates.OrangeHair},
                {"hair_teal", CrowdDelegates.TealHair},
                {"hair_pink", CrowdDelegates.PinkHair},
                {"hair_black", CrowdDelegates.BlackHair},
                {"hair_white", CrowdDelegates.WhiteHair},
                {"hair_style", CrowdDelegates.HairStyle},
                {"gender", CrowdDelegates.Gender},


                {"spawn_bug", CrowdDelegates.SpawnBug},
                {"spawn_wildernessgolem", CrowdDelegates.SpawnWildernessGolem},
                {"give_buff_monstermusk", CrowdDelegates.GiveMonsterMuskBuff},
                {"msg_crowdcontrolpro", CrowdDelegates.CrowdControlProMSG},
                {"emote_sad", CrowdDelegates.PlayerEmote},
                {"emote_heart", CrowdDelegates.PlayerEmote},
                {"emote_exclamation", CrowdDelegates.PlayerEmote},
                {"emote_note", CrowdDelegates.PlayerEmote},
                {"emote_sleep", CrowdDelegates.PlayerEmote},
                {"emote_game", CrowdDelegates.PlayerEmote},
                {"emote_question", CrowdDelegates.PlayerEmote},
                {"emote_x", CrowdDelegates.PlayerEmote},
                {"emote_pause", CrowdDelegates.PlayerEmote},
                {"emote_blush", CrowdDelegates.PlayerEmote},
                {"emote_angry", CrowdDelegates.PlayerEmote},
                {"emote_yes", CrowdDelegates.PlayerEmote},
                {"emote_no", CrowdDelegates.PlayerEmote},
                {"emote_sick", CrowdDelegates.PlayerEmote},
                {"emote_laugh", CrowdDelegates.PlayerEmote},
                {"emote_taunt", CrowdDelegates.PlayerEmote},

                {"emote_surprised", CrowdDelegates.PlayerEmote},
                {"emote_hi", CrowdDelegates.PlayerEmote},
                {"emote_uh", CrowdDelegates.PlayerEmote},
                {"emote_music", CrowdDelegates.PlayerEmote},
                {"emote_jar", CrowdDelegates.PlayerEmote},
                {"emote_happy", CrowdDelegates.PlayerEmote},

                {"divorce", CrowdDelegates.Divorce},
                {"removechildren", CrowdDelegates.TurnChildrenToDoves},
                {"swimwear_on", CrowdDelegates.ChangeSwimClothes},
                {"swimwear_off", CrowdDelegates.ChangeSwimClothes},

            };
        }

        private void ClientLoop()
        {
            UI.ShowInfo("Connected to Crowd Control v1.4");

            var timer = new Timer(timeUpdate, null, 0, 200);

            try
            {
                while (Running)
                {
                    CrowdRequest req = CrowdRequest.Recieve(this, Socket);
                    if (req == null || req.IsKeepAlive()) continue;

                    lock (Requests)
                        Requests.Enqueue(req);
                }
            }
            catch (Exception)
            {
                UI.ShowError("Disconnected from Crowd Control");
                Socket.Close();
            }
        }

        public void timeUpdate(System.Object state)
        {
            if (Saving || Game1.isTimePaused || Game1.activeClickableMenu != null)
            {
                BuffThread.addTime(200);
                paused = true;
            }
            else if (paused)
            {
                paused = false;
                BuffThread.unPause();
                BuffThread.tickTime(200);
            }
            else
            {
                BuffThread.tickTime(200);
            }
        }

        public bool CanSpawn() => Spawn;
        public bool IsRunning() => Running;

        public void NetworkLoop()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            int maxAttempts = 3;
            int attempts = 0;

            while (Running && attempts < maxAttempts)
            {
                UI.ShowInfo("Attempting to connect to Crowd Control");
                if (attempts == maxAttempts - 1) UI.ShowError("Final connection attempt. Make sure Crowd Control is running.");


                try
                {
                    Socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    if (Socket.BeginConnect(Endpoint, null, null).AsyncWaitHandle.WaitOne(10000, true) && Socket.Connected)
                        ClientLoop();
                    else
                        UI.ShowError("Failed to connect to Crowd Control");
                    Socket.Close();
                }
                catch (Exception e)
                {
                    UI.ShowError(e.GetType().Name);
                    UI.ShowError("Failed to connect to Crowd Control");
                }

                attempts++;
                Thread.Sleep(5000);
            }
        }

        public void OnSaved(object sender, SavedEventArgs args)
        {
            Saving = false;
        }

        public void OnSaving(object sender, SavingEventArgs args)
        {
            Saving = true;
            foreach (KeyValuePair<GameLocation, List<Monster>> pair in Monsters)
            {
                foreach (Monster monster in pair.Value)
                    pair.Key.characters.Remove(monster);
                pair.Value.Clear();
            }
        }

        public void OnWarped(object sender, WarpedEventArgs args)
        {
            Spawn = Array.IndexOf(_no_spawn, args.NewLocation.Name.ToLower()) < 0;
        }

        public void RequestLoop()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            while (Running)
            {
                try
                {
                    while (Saving || Game1.isTimePaused)
                        Thread.Yield();

                    CrowdRequest req = null;
                    lock (Requests)
                    {
                        if (Requests.Count == 0)
                            continue;
                        req = Requests.Dequeue();
                    }

                    string code = req.GetReqCode();
                    try
                    {
                        CrowdResponse res = Delegate[code](this, req);
                        if (res == null)
                        {
                            new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, $"Request error for '{code}'").Send(Socket);
                        }

                        res.Send(Socket);
                    }
                    catch (KeyNotFoundException)
                    {
                        new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, $"Request error for '{code}'").Send(Socket);
                    }
                }
                catch (Exception)
                {
                    UI.ShowError("Disconnected from Crowd Control");
                    Socket.Close();
                }
            }
        }

        public void Stop()
        {
            Running = false;
        }

        public void TrackMonster(Monster monster)
        {
            GameLocation location = Game1.player.currentLocation;
            if (!Monsters.ContainsKey(location))
                Monsters[location] = new List<Monster>();
            Monsters[location].Add(monster);
        }
    }
}
