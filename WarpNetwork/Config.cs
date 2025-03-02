﻿using StardewModdingAPI;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using WarpNetwork.api;

namespace WarpNetwork
{
    enum WarpEnabled
    {
        AfterObelisk,
        Always,
        Never
    }
    class Config
    {
        public WarpEnabled VanillaWarpsEnabled { get; set; }
        public WarpEnabled FarmWarpEnabled { get; set; }
        public bool AccessFromDisabled { get; set; }
        public bool AccessFromWand { get; set; }
        public bool PatchObelisks { get; set; }
        public bool MenuEnabled { get; set; }
        public bool WarpCancelEnabled { get; set; }
        public bool WandReturnEnabled { get; set; }

        public void ResetToDefault()
        {
            VanillaWarpsEnabled = WarpEnabled.AfterObelisk;
            FarmWarpEnabled = WarpEnabled.AfterObelisk;
            AccessFromDisabled = false;
            AccessFromWand = false;
            PatchObelisks = true;
            MenuEnabled = true;
            WarpCancelEnabled = false;
            WandReturnEnabled = true;
        }
        public void ApplyConfig()
        {
            ModEntry.helper.WriteConfig(this);
            ModEntry.helper.GameContent.InvalidateCache(ModEntry.pathLocData);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ObeliskCheckRequired()
            => VanillaWarpsEnabled == WarpEnabled.AfterObelisk || FarmWarpEnabled == WarpEnabled.AfterObelisk;
        public void RegisterModConfigMenu(IManifest manifest)
        {
            if (!ModEntry.helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
                return;
            IGMCMAPI api = ModEntry.helper.ModRegistry.GetApi<IGMCMAPI>("spacechase0.GenericModConfigMenu");
            api.Register(manifest, ResetToDefault, ApplyConfig);

            api.AddSectionTitle(manifest, () => manifest.Name, () => manifest.Description);

            api.AddTextOption(
                manifest,
                () => VanillaWarpsEnabled.ToString(),
                (string c) => VanillaWarpsEnabled = Utils.ParseEnum<WarpEnabled>(c),
                () => ModEntry.i18n.Get("cfg-warpsenabled.label"),
                () => ModEntry.i18n.Get("cfg-warpsenabled.desc"),
                Enum.GetNames(typeof(WarpEnabled))
            );
            api.AddTextOption(
                manifest,
                () => FarmWarpEnabled.ToString(),
                (string c) => FarmWarpEnabled = Utils.ParseEnum<WarpEnabled>(c),
                () => ModEntry.i18n.Get("cfg-farmenabled.label"),
                () => ModEntry.i18n.Get("cfg-farmenabled.desc"),
                Enum.GetNames(typeof(WarpEnabled))
            );
            api.AddBoolOption(
                manifest,
                () => AccessFromDisabled,
                (bool b) => AccessFromDisabled = b,
                () => ModEntry.i18n.Get("cfg-accessdisabled.label"),
                () => ModEntry.i18n.Get("cfg-accessdisabled.desc")
            );
            api.AddBoolOption(
                manifest,
                () => AccessFromWand,
                (bool b) => AccessFromWand = b,
                () => ModEntry.i18n.Get("cfg-accesswand.label"),
                () => ModEntry.i18n.Get("cfg-accesswand.desc")
            );
            api.AddBoolOption(
                manifest,
                () => PatchObelisks,
                (bool b) => PatchObelisks = b,
                () => ModEntry.i18n.Get("cfg-obeliskpatch.label"),
                () => ModEntry.i18n.Get("cfg-obeliskpatch.desc")
            );
            api.AddBoolOption(
                manifest,
                () => MenuEnabled,
                (bool b) => MenuEnabled = b,
                () => ModEntry.i18n.Get("cfg-menu.label"),
                () => ModEntry.i18n.Get("cfg-menu.desc")
            );
            api.AddBoolOption(
                manifest,
                () => WarpCancelEnabled,
                (bool b) => WarpCancelEnabled = b,
                () => ModEntry.i18n.Get("cfg-warpcancel.label"),
                () => ModEntry.i18n.Get("cfg-warpcancel.desc")
            );
            api.AddBoolOption(
                manifest,
                () => WandReturnEnabled,
                (bool b) => WandReturnEnabled = b,
                () => ModEntry.i18n.Get("cfg-wandreturn.label"),
                () => ModEntry.i18n.Get("cfg-wandreturn.desc")
            );
        }
        internal string AsText()
        {
            StringBuilder sb = new();
            sb.AppendLine().AppendLine("Config:");
            sb.Append("\tVanillaWarpsEnabled: ").AppendLine(VanillaWarpsEnabled.ToString());
            sb.Append("\tFarmWarpEnabled: ").AppendLine(FarmWarpEnabled.ToString());
            sb.Append("\tAccessFromDisabled: ").AppendLine(AccessFromDisabled.ToString());
            sb.Append("\tAccessFromWand: ").AppendLine(AccessFromWand.ToString());
            sb.Append("\tPatchObelisks: ").AppendLine(PatchObelisks.ToString());
            sb.Append("\tMenuEnabled: ").AppendLine(MenuEnabled.ToString());
            return sb.ToString();
        }
        public Config()
        {
            ResetToDefault();
        }
    }
}
