﻿using Microsoft.Xna.Framework;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using WarpNetwork.api;
using WarpNetwork.models;

namespace WarpNetwork
{
    class WarpHandler
    {
        internal static readonly PerScreen<Point?> DesertWarp = new();
        internal static readonly PerScreen<string> wandLocation = new();
        internal static readonly PerScreen<Point> wandTile = new();
        private static readonly WarpNetHandler returnHandler = new(() => wandLocation.Value is not null, () => "RETURN", () => ModEntry.i18n.Get("dest-return"), ReturnToPrev);
        public static void ShowWarpMenu(string exclude = "", bool consume = false)
        {
            if (!ModEntry.config.MenuEnabled)
            {
                ShowFailureText();
                ModEntry.monitor.Log("Warp menu is disabled in config, menu not displayed.");
                return;
            }

            List<WarpLocation> dests = new();
            Dictionary<string, WarpLocation> locs = Utils.GetWarpLocations();

            if (locs.ContainsKey(exclude))
            {
                if (!locs[exclude].Enabled && !ModEntry.config.AccessFromDisabled)
                {
                    ShowFailureText();
                    ModEntry.monitor.Log("Access from locked locations is disabled, menu not displayed.");
                    return;
                }
            }
            string normalized = exclude.ToLowerInvariant();
            if (normalized == "_wand" && ModEntry.config.WandReturnEnabled && wandLocation.Value is not null)
            {
                var dest = Game1.getLocationFromName(wandLocation.Value);
                if (dest is not null)
                    dests.Add(new CustomWarpLocation(returnHandler));
            }
            foreach ((string id, WarpLocation loc) in locs)
            {
                string normid = id.ToLowerInvariant();
                if (
                    !loc.AlwaysHide && (
                    normalized == "_force" ||
                    (loc.Enabled && normid != normalized) ||
                    (normid == "farm" && normalized == "_wand")
                    )
                )
                {
                    if (loc is CustomWarpLocation || Game1.getLocationFromName(loc.Location) != null)
                        dests.Add(locs[id]);
                    else
                        ModEntry.monitor.Log("Invalid Location name '" + loc.Location + "'; skipping entry.", LogLevel.Warn);
                }
            }
            if (dests.Count == 0)
            {
                ModEntry.monitor.Log("No valid warp destinations, menu not displayed.");
                ShowFailureText();
                return;
            }
            Item stack = consume ? Game1.player.CurrentItem : null;
            Game1.activeClickableMenu = new WarpMenu(dests, (WarpLocation where) =>
            {
                WarpToLocation(where, normalized == "_wand");
                Utils.reduceItemCount(Game1.player, stack, 1);
            });
        }
        internal static void ShowFailureText()
        {
            Game1.drawObjectDialogue(Game1.parseText(ModEntry.i18n.Get("ui-fail")));
        }
        internal static void ShowFestivalNotReady()
        {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2973"));
        }
        private static void ReturnToPrev()
        {
            (int x, int y) = wandTile.Value;
            string loc = wandLocation.Value;
            //MUST copy to preserve. warp is called on delay and values may change.
            DoWarpEffects(() => Game1.warpFarmer(loc, x, y, false));
        }
        public static void HandleAction(object sender, EventArgsAction action)
        {
            if (action.Action.ToLower() == "warpnetwork")
            {
                ModEntry.monitor.Log("Warp Network Activated");
                string[] args = action.ActionString.Split(' ');
                ShowWarpMenu((args.Length > 1) ? args[1] : "");
            }
            if (action.Action.ToLower() == "warpnetworkto")
            {
                ModEntry.monitor.Log("Direct Warp Activated");
                string[] args = action.ActionString.Split(' ');
                DirectWarp(args);
            }
        }
        public static bool DirectWarp(string[] args)
        {
            if (args.Length > 1)
            {
                bool force = (args.Length > 2);
                return DirectWarp(args[1], force);
            }
            else
            {
                ModEntry.monitor.Log("Warning! Map '" + Game1.currentLocation.Name + "' has invalid WarpNetworkTo property! Location MUST be specified!", LogLevel.Warn);
                return false;
            }
        }
        public static bool DirectWarp(string location, bool force)
        {
            if (location is null)
            {
                ModEntry.monitor.Log("Destination is null! Cannot warp!", LogLevel.Error);
                ShowFailureText();
                return false;
            }
            if (location.ToLowerInvariant() == "_return")
            {
                if (returnHandler.getEnabled())
                    returnHandler.Warp();
                else
                    return false;
                return true;
            }
            Dictionary<string, WarpLocation> locs = Utils.GetWarpLocations();
            WarpLocation loc = locs[location];
            if (locs.ContainsKey(location))
            {
                if (Game1.getLocationFromName(loc.Location) is not null)
                {
                    if (!Utils.IsFestivalAtLocation(loc.Location) || Utils.IsFestivalReady())
                    {
                        if (force || loc.Enabled)
                        {
                            WarpToLocation(loc);
                            return true;
                        }
                        else
                        {
                            ShowFailureText();
                            return false;
                        }
                    }
                    else
                    {
                        ModEntry.monitor.Log("Failed to warp to '" + loc.Location + "': Festival at location not ready.", LogLevel.Debug);
                        ShowFestivalNotReady();
                        return false;
                    }
                }
                else
                {
                    ModEntry.monitor.Log("Failed to warp to '" + loc.Location + "': Location with that name does not exist!", LogLevel.Error);
                    ShowFailureText();
                    return false;
                }
            }
            else
            {
                ModEntry.monitor.Log("Warp to '" + location + "' failed: warp network location not registered with that name", LogLevel.Warn);
                ShowFailureText();
                return false;
            }
        }
        internal static void WarpToLocation(WarpLocation where, bool fromWand = false)
        {
            if (where is CustomWarpLocation custom)
            {
                custom.handler.Warp();
                if (custom.handler == returnHandler)
                    wandLocation.Value = null;
                return;
            }
            if (Game1.currentLocation.Name != "Temp")
            {
                wandLocation.Value = Game1.currentLocation.NameOrUniqueName;
                wandTile.Value = Game1.player.getTileLocationPoint();
            }
            int x = where.X;
            int y = where.Y;
            if (where.Location == "Farm")
            {
                if (fromWand)
                {
                    Point dest = GetFrontDoor(Game1.player);
                    DoWarpEffects(() => Game1.warpFarmer("Farm", dest.X, dest.Y, false));
                    return;
                }
                Point farmTotem = Utils.GetActualFarmPoint(x, y);
                x = farmTotem.X;
                y = farmTotem.Y;
            }
            if (where.Location == "Desert")
            {
                //desert has bus scene hardcoded. Must warp to hardcoded spot, then use obelisk patch to move the player afterwards.
                if (!where.OverrideMapProperty)
                {
                    DesertWarp.Value = Game1.getLocationFromName("Desert").GetMapPropertyPosition("WarpNetworkEntry", where.X, where.Y);
                }
                else
                {
                    DesertWarp.Value = new Point(where.X, where.Y);
                }
                DoWarpEffects(() => Game1.warpFarmer("Desert", 35, 43, false));
            }
            if (!where.OverrideMapProperty)
            {
                Point coords = Game1.getLocationFromName(where.Location).GetMapPropertyPosition("WarpNetworkEntry", x, y);
                DoWarpEffects(() => Game1.warpFarmer(where.Location, coords.X, coords.Y, false));
            }
            else
            {
                DoWarpEffects(() => Game1.warpFarmer(where.Location, x, y, false));
            }
        }
        private static Point GetFrontDoor(Farmer who)
        {
            FarmHouse home = Utility.getHomeOfFarmer(who);
            if (!(home is null))
            {
                return home.getFrontDoorSpot();
            }
            return Game1.getLocationFromName("Farm").GetMapPropertyPosition("FarmHouseEntry", 64, 15);
        }
        private static void DoWarpEffects(Action action)
        {
            Farmer who = Game1.player;
            // reflection
            Multiplayer mp = ModEntry.helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            // --
            for (int index = 0; index < 12; ++index)
                mp.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
                    354,
                    Game1.random.Next(25, 75), 6, 1,
                    new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)),
                    false,
                    Game1.random.NextDouble() < 0.5)
                    );
            who.currentLocation.playSound("wand", NetAudio.SoundContext.Default);
            Game1.displayFarmer = false;
            who.temporarilyInvincible = true;
            who.temporaryInvincibilityTimer = -2000;
            who.freezePause = 1000;
            Game1.flashAlpha = 1f;
            DelayedAction.fadeAfterDelay(new Game1.afterFadeFunction(() =>
            {
                action();
                Game1.changeMusicTrack("none");
                Game1.fadeToBlackAlpha = 0.99f;
                Game1.screenGlow = false;
                Game1.player.temporarilyInvincible = false;
                Game1.player.temporaryInvincibilityTimer = 0;
                Game1.displayFarmer = true;
            }), 1000);
            new Rectangle(who.GetBoundingBox().X, who.GetBoundingBox().Y, 64, 64).Inflate(192, 192);
            int num = 0;
            for (int index = who.getTileX() + 8; index >= who.getTileX() - 8; --index)
            {
                mp.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(6, new Vector2(index, who.getTileY()) * 64f, Color.White, 8, false, 50f, 0, -1, -1f, -1, 0)
                {
                    layerDepth = 1f,
                    delayBeforeAnimationStart = num * 25,
                    motion = new Vector2(-0.25f, 0.0f)
                });
                ++num;
            }
        }
    }
}
