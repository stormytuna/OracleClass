using OracleClass.Common.Players;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OracleClass {
    /// <summary>Using this enum instead of integers makes code a little more readable</summary>
    public enum OracleWeaponType {
        Focus,
        Cane,
        Glyph
    }

    #region Oracle Helpers

    public static class OracleHelpers {
        /// <summary>
        /// Returns the <c>OracleWeapon</c> instance for a given item <para />
        /// Does <b>not</b> perform null checks, make sure to check <c>item.ModItem is not null</c> if applicable
        /// </summary>
        public static OracleWeapon GetOracleWeapon(this Item item) {
            return item.ModItem as OracleWeapon;
        }

        /// <summary>
        /// Returns the <c>OracleWeaponType</c> for a given item <para />
        /// Does <b>not</b> perform null checks, make sure to check <c>item.ModItem is not null</c> if applicable
        /// </summary>
        public static OracleWeaponType GetOracleWeaponType(this Item item) {
            return item.GetOracleWeapon().OracleType;
        }
    }

    #endregion

    #region Abstract Classes

    /// <summary>
    /// This is a helper class to make creating and handling oracle weapons easier. <para />
    /// Be sure to set these properties in the SetDefaults hook <para />
    /// <list type="bullet">
    /// <item><term>OracleType</term><description> The type of oracle weapon, either Focus, Cane or Glyph</description></item>
    /// <item><term>BaseSoulCapacity</term><description> The base soul capacity of this weapon</description></item>
    /// <item><term>SoulRecoveryFrames</term><description> The amount of frames it takes to go from 0 soul capacity to max soul capacity, not including cooldown frames from exhausting</description></item>
    /// </list>
    /// This class overrides these ModItem methods, so be sure to either call base or understand what each override does when overriding in your weapon
    /// <list type="bullet">
    /// <item><term>CanUseItem</term><description> Checks if this weapon is exhausted</description></item>
    /// <item><term>UseItem</term><description> Handles current soul capacity being reduced and the weapon being exhausted if applicable</description></item>
    /// <item><term>UpdateInventory</term><description> Handles soul recovery and soul recovery cooldown</description></item>
    /// <item><term>ModifyTooltips</term><description> Handles displaying "X/Y soul capacity" and "+|- Z% soul capacity" tooltips</description></item>
    /// <item><term>Clone</term><description> Handles instanced modded data being persistent between clones of the same item</description></item>
    /// <item><term>SaveData and LoadData</term><description> Handles saving and loading of instanced modded data on this item</description></item>
    /// </list>
    /// </summary>
    public abstract class OracleWeapon : ModItem {
        /// <summary>The type of this oracle weapon</summary>
        public OracleWeaponType OracleType { get; set; }

        /// <summary>The soul capacity of this weapon <b>after</b> buffs from armour, accessories and reforges</summary>
        public int MaxSoulCapacity {
            get {
                return (int)((float)BaseSoulCapacity * SoulCapacityMultiplierFromPrefix * Main.LocalPlayer.GetModPlayer<OraclePlayer>().SoulCapacityMultiplier);
            }
        }

        /// <summary>The current soul capacity of this weapon</summary>
        public int CurSoulCapacity { get; set; }

        /// <summary>Just a helper property that returns current soul / max soul</summary>
        public float CurSoulCapacityNormalized {
            get {
                return (float)CurSoulCapacity / (float)MaxSoulCapacity;
            }
        }

        /// <summary>The base soul capacity of this weapon <b>before</b> buffs from armour, accessories and reforges</summary>
        public int BaseSoulCapacity { get; set; }

        /// <summary>The multiplier to base soul capacity from this weapons prefix</summary>
        public float SoulCapacityMultiplierFromPrefix { private get; set; } = 1f;

        /// <summary>The number of frames it takes for this weapons soul to recover completely</summary>
        public int SoulRecoveryFrames { get; set; }

        /// <summary>Whether or not this weapon is exhausted. An exhausted weapon usually cannot be used</summary>
        public bool Exhausted { get; set; } = false;

        /// <summary>The current exhausted cooldown delay. Prevents a weapon from naturally regaining soul</summary>
        public int SoulRecoveryCooldown { get; set; } = 0;

        // This prevents an item from being used while it's exhausted
        public override bool CanUseItem(Player player) => !Exhausted && base.CanUseItem(player);

        // This just consumes soul as we use our weapon
        public override bool? UseItem(Player player) {
            CurSoulCapacity--;
            SoulRecoveryCooldown = 5 * 60;
            if (CurSoulCapacity <= 0) {
                Exhausted = true;
            }

            return base.UseItem(player);
        }

        // This handles our soul recovering
        // I know this is kinda spaghetti but it works so w/e
        private int _soulRecovery = 0;
        public override void UpdateInventory(Player player) {
            // Handles soul recovering
            if (SoulRecoveryCooldown <= 0 && CurSoulCapacity < MaxSoulCapacity) {
                // Sets our starting soulRecovery if we need to
                if (_soulRecovery < (int)(CurSoulCapacityNormalized * (float)SoulRecoveryFrames)) {
                    _soulRecovery = (int)(CurSoulCapacityNormalized * (float)SoulRecoveryFrames);
                }

                // Increase our soul recovered
                _soulRecovery++;

                // Check if we should update our current soul capacity 
                if (CurSoulCapacity < (int)(((float)_soulRecovery / (float)SoulRecoveryFrames) * MaxSoulCapacity)) {
                    CurSoulCapacity = (int)(((float)_soulRecovery / (float)SoulRecoveryFrames) * MaxSoulCapacity);
                }

                // Check if we've finished recovering soul
                if (_soulRecovery >= SoulRecoveryFrames) {
                    CurSoulCapacity = MaxSoulCapacity;
                    _soulRecovery = 0;
                }
            }

            // Handles decreasing our recovery cooldown
            if (SoulRecoveryCooldown > 0) {
                SoulRecoveryCooldown--;
            }

            base.UpdateInventory(player);
        }

        // This adds a "X soul capacity" tooltip under a weapons damage
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int index = tooltips.FindIndex(tip => tip.Name == "Knockback") + 1;
            TooltipLine soulCapacityTooltip = new(Mod, "SoulCapacity", $"{CurSoulCapacity}/{MaxSoulCapacity} soul capacity");
            tooltips.Insert(index, soulCapacityTooltip);

            base.ModifyTooltips(tooltips);
        }

        // This ensures clones of this item have the same soul capacity multiplier from prefix
        // Not 100% sure /when/ this applies but example instanced item does this
        // https://github.com/tModLoader/tModLoader/blob/daa239205ab69743d44f7bb64c9cd3e080024a51/ExampleMod/Content/Items/ExampleInstancedItem.cs
        public override ModItem Clone(Item newEntity) {
            OracleWeapon clone = (OracleWeapon)base.Clone(newEntity);
            clone.SoulCapacityMultiplierFromPrefix = SoulCapacityMultiplierFromPrefix;
            return clone;
        }

        // These save and load this weapons soul capacity multiplier from prefix
        // We need to do this because prefixes only apply their stats once on application
        public override void SaveData(TagCompound tag) {
            tag["soulCapacityMultiplierFromPrefix"] = SoulCapacityMultiplierFromPrefix;

            base.SaveData(tag);
        }
        public override void LoadData(TagCompound tag) {
            SoulCapacityMultiplierFromPrefix = tag.GetFloat("soulCapacityMultiplierFromPrefix");

            base.LoadData(tag);
        }
    }

    public abstract class HeldProjectile : ModProjectile {

    }

    #endregion
}