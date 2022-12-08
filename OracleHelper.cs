using Microsoft.Xna.Framework;
using OracleClass.Common.Players;
using OracleClass.Content.DamageClasses;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace OracleClass {
    /// <summary>Using this enum instead of integers makes code a little more readable</summary>
    public enum OracleWeaponType {
        Focus,
        Cane,
        Glyph
    }

    #region Oracle Helpers

    public static class OracleHelpers {
        /// <summary>Returns whether or not a given item is an Oracle weapon</summary>
        public static bool IsOracleWeapon(this Item item) {
            return item.CountsAsClass<OracleDamageClass>();
        }

        /// <summary>
        /// Returns the <c>OracleWeapon</c> instance for a given item <para />
        /// Does <b>not</b> perform null checks, use <c>TryGetOracleWeapon</c> if you need them
        /// </summary>
        public static OracleWeapon GetOracleWeapon(this Item item) {
            return item.ModItem as OracleWeapon;
        }

        /// <summary>
        /// Gets the <c>OracleWeapon</c> instance for a given item <para />
        /// This will perform null checks, use <c>GetOracleWeapon</c> if you don't need them
        /// </summary>
        /// <param name="oracleWeapon">The <c>OracleWeapon</c> instance of the given item</param>
        /// <returns>Returns true if the given item is an <c>OracleWeapon</c>, returns false otherwise</returns>
        public static bool TryGetOracleWeapon(this Item item, out OracleWeapon oracleWeapon) {
            oracleWeapon = null;

            if (item.ModItem is not null && item.ModItem is OracleWeapon ow) {
                oracleWeapon = ow;
                return true;
            }

            return false;
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
    /// <item><term>HandleSoulConsumption</term><description> Whether or not this item will handle soul consumption for us. Set this to false for held projectile weapons</description></item>
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
        public float SoulCapacityMultiplierFromPrefix { get; set; } = 1f;

        /// <summary>The number of frames it takes for this weapons soul to recover completely</summary>
        public int SoulRecoveryFrames { get; set; }

        /// <summary>Whether or not this item should handle its own soul consumption. Set this to false for held projectile weapons</summary>
        public bool HandleSoulConsumption { get; set; }

        /// <summary>Whether or not this weapon is exhausted. An exhausted weapon usually cannot be used</summary>
        public bool Exhausted { get; set; } = false;

        /// <summary>The current exhausted cooldown delay. Prevents a weapon from naturally regaining soul</summary>
        public int SoulRecoveryCooldown { get; set; } = 0;

        // This prevents an item from being used while it's exhausted
        public override bool CanUseItem(Player player) => !Exhausted && base.CanUseItem(player);

        // This just consumes soul as we use our weapon
        public override bool? UseItem(Player player) {
            if (HandleSoulConsumption) {
                CurSoulCapacity--;
                player.GetModPlayer<OraclePlayer>().ReduceSoulOnAllWeapons(Item);
                SoulRecoveryCooldown = 5 * 60;
                if (CurSoulCapacity <= 0) {
                    Exhausted = true;
                }
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
                    Exhausted = false;
                    _soulRecovery = 0;
                }
            }

            // Handles decreasing our recovery cooldown
            if (SoulRecoveryCooldown > 0) {
                SoulRecoveryCooldown--;
            }

            base.UpdateInventory(player);
        }

        public override bool? PrefixChance(int pre, UnifiedRandom rand) {
            switch (pre) {
                case -1:
                    return !Main.rand.NextBool(4);
            }

            return base.PrefixChance(pre, rand);
        }

        public override int ChoosePrefix(UnifiedRandom rand) {
            var modPrefixes = PrefixLoader.GetPrefixesInCategory(PrefixCategory.Custom);

            List<int> rollable = new();
            foreach (var modPrefix in modPrefixes) {
                if (modPrefix.CanRoll(Item)) {
                    rollable.Add(modPrefix.Type);
                }
            }
            rollable.AddRange(Helpers.universalPrefixes);

            return rand.NextFromList(rollable.ToArray());
        }

        // This adds a "X soul capacity" tooltip under a weapons damage
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            // Handles our soul capacity tooltip
            int soulCapacityIndex = tooltips.FindIndex(tip => tip.Name == "Knockback") + 1;
            TooltipLine soulCapacityTooltip = new(Mod, "SoulCapacity", $"{CurSoulCapacity}/{MaxSoulCapacity} soul capacity");
            tooltips.Insert(soulCapacityIndex, soulCapacityTooltip);

            // Handles our prefix tooltip  // TODO: test this
            if (SoulCapacityMultiplierFromPrefix != 1f) {
                var tooltip = tooltips.FindLast(tip => tip.Name.StartsWith("Prefix"));
                if (tooltip is not null) {
                    int soulCapacityBonusIndex = tooltip.Name == "PrefixKnockback" ? tooltips.IndexOf(tooltip) : tooltips.IndexOf(tooltip) + 1;
                    int soulCapacityBonus = (int)MathF.Abs((1f - SoulCapacityMultiplierFromPrefix) * 100f);
                    string plusOrMinus = SoulCapacityMultiplierFromPrefix < 1f ? "-" : "+";
                    TooltipLine newLine = new(Mod, "PrefixSummonTagBonus", $"{plusOrMinus}{soulCapacityBonus}% summon tag damage");
                    newLine.IsModifier = true;
                    newLine.IsModifierBad = SoulCapacityMultiplierFromPrefix < 1f;
                    tooltips.Insert(soulCapacityBonusIndex, newLine);
                }
            }

            base.ModifyTooltips(tooltips);
        }

        public override void OnCreate(ItemCreationContext context) {
            SoulCapacityMultiplierFromPrefix = 1f;
        }

        // Not 100% sure /when/ this applies but example instanced item does this
        // https://github.com/tModLoader/tModLoader/blob/daa239205ab69743d44f7bb64c9cd3e080024a51/ExampleMod/Content/Items/ExampleInstancedItem.cs
        public override ModItem Clone(Item newEntity) {
            OracleWeapon clone = (OracleWeapon)base.Clone(newEntity);
            clone.CurSoulCapacity = CurSoulCapacity;
            clone.SoulCapacityMultiplierFromPrefix = SoulCapacityMultiplierFromPrefix;
            clone.Exhausted = Exhausted;
            clone.SoulRecoveryCooldown = SoulRecoveryCooldown;
            return clone;
        }

        public override void SaveData(TagCompound tag) {
            tag["SoulCapacityMultiplierFromPrefix"] = SoulCapacityMultiplierFromPrefix;
        }

        public override void LoadData(TagCompound tag) {
            SoulCapacityMultiplierFromPrefix = tag.GetFloat("SoulCapacityMultiplierFromPrefix");
        }
    }

    /// <summary>
    /// This is a helper class to make creating and handling held projectile weapons. <para />
    /// All this class will handle for you is how the projectile appears and soul draining while you channel the item <para />
    /// This class also contains a frame counter property that's already set up for you, no need to use the <c>frameCount</c> field or create your own. It also doesn't use any ai fields, so you can use those for whatever you want <para />
    /// Be sure to set these properties in the SetDefaults hook <para />
    /// <list type="bullet">
    /// <item><term>HoldOutOffset</term><description> How far away the projectile will display from your character</description></item>
    /// <item><term>OracleType</term><description> The type of oracle weapon, either Focus, Cane or Glyph</description></item>
    /// <item><term>RotationOffset</term><description> How many extra radians this projectile will rotate when it's pointed to the mouse</description></item>
    /// <item><term>UseTimeOverride</term><description> Setting this will make the projectile treat that new value as the base useTime of the weapon</description></item>
    /// </list>
    /// This class overrides these ModProjectile methods, so be sure to either call base or understand what each override does when overriding in your weapon
    /// <list type="bullet">
    /// <item><term>AI</term><description> Handles losing soul from your item, projectile direction, location and rotation and sets some values on the player</description></item>
    /// <item><term>SendExtraAI and ReceiveExtraAI</term><description> Handles sending and receiving AI fields created by this class</description></item>
    /// </list>
    /// This class provides these virtual methods, be sure to take advantage of them
    /// <list type="bullet">
    /// <item><term>OnUseItem</term><description> This is called each time the projectile consumes some soul</description></item>
    /// <item><term>OnFinishedCharging</term><description> This is called when the projectile is finished channelling, either from the player letting go or running out of soul. It has a total soul consume parameter</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="Terraria.ModLoader.ModProjectile" />
    public abstract class HeldProjectile : ModProjectile {
        /// <summary>Just a helper property, feel free to use this in your projectile</summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>How far away this projectile will appear from the player</summary>
        public float HoldOutOffset { get; set; }

        /// <summary>The type of this oracle weapon</summary>
        public OracleWeaponType OracleType { get; set; }

        /// <summary>How much this projetile should be rotated when it points to the mouse</summary>
        public float RotationOffset { get; set; }

        /// <summary>This property acts as a frame counter, you can use it to apply spin-up effects</summary>
        public int AI_FrameCount { get; set; } = 0;

        // This is just a field and accessor property, makes it easier to have the firerate increase with time
        private int _useTimeOverride = -1;
        public int UseTimeOverride {
            get {
                if (_useTimeOverride != -1) {
                    return _useTimeOverride;
                }
                return Owner.HeldItem.useTime;
            }
            set {
                _useTimeOverride = value;
            }
        }

        /// <summary>Called when the projetile consumes soul from the held item. Takes into account <c>UseTimeOverride</c>
        public virtual void OnUseItem() { }

        /// <summary>Called when the projectile is killed</summary>
        /// <param name="totalCharge">The total amount of soul this item consumed while charging</param>
        public virtual void OnFinishedCharging(int totalSoulConsumed) { }

        // This is a helper property that just applies our attack speed
        private int UseTimeAfterBuffs => (int)((float)UseTimeOverride / Owner.GetWeaponAttackSpeed(Owner.HeldItem));

        // This is a counter that just keeps track of taking away our soul
        private int _soulConsumeCooldown = 0;

        // This counter keeps track of how much soul this projectile has consume
        private int _totalSoulConsumed = 0;

        // AI takes care of a few things, make sure to return base if you override it 
        public override void AI() {
            // Just a var we use a couple times
            Vector2 toMouse = Main.MouseWorld - Projectile.Center;
            toMouse.Normalize();

            // Take soul from our players held item
            _soulConsumeCooldown--;
            if (_soulConsumeCooldown <= 0) {
                // Do our stuff if we've consumed soul from the weapon
                var heldOracleWeapon = Owner.HeldItem.GetOracleWeapon();
                heldOracleWeapon.CurSoulCapacity--;
                Owner.GetModPlayer<OraclePlayer>().ReduceSoulOnAllWeapons(Owner.HeldItem);
                heldOracleWeapon.SoulRecoveryCooldown = 5 * 60;
                _soulConsumeCooldown = UseTimeAfterBuffs;
                _totalSoulConsumed++;
                if (heldOracleWeapon.CurSoulCapacity <= 0) {
                    heldOracleWeapon.CurSoulCapacity = 0;
                    heldOracleWeapon.Exhausted = true;
                }
                OnUseItem();
            }

            // Kill the projectile if we stop using it or can't use it
            if (!Owner.channel || Owner.HeldItem.GetOracleWeapon().CurSoulCapacity <= 0) {
                Projectile.Kill();
            }

            // Set direction and rotation
            Projectile.direction = 1;
            if (Math.Sign(Main.MouseWorld.X - Owner.Center.X) == -1)
                Projectile.direction = -1;
            Projectile.rotation = toMouse.ToRotation() - Projectile.direction * RotationOffset + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            // Set position and velocity
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter) + toMouse * HoldOutOffset;
            Projectile.velocity = Vector2.Zero;

            // Set timeleft
            Projectile.timeLeft = 2;

            // Set some values on our player
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.itemRotation = Projectile.DirectionFrom(Owner.MountedCenter).ToRotation();
            if (Projectile.Center.X < Owner.MountedCenter.X) {
                Owner.itemRotation += (float)Math.PI;
            }
            Owner.itemRotation = MathHelper.WrapAngle(Owner.itemRotation);

            // Increment our frame count
            AI_FrameCount++;

            base.AI();
        }

        // This just calls our OnFinishedCharging virtual method
        public override void Kill(int timeLeft) {
            OnFinishedCharging(_totalSoulConsumed);

            base.Kill(timeLeft);
        }

        // Sends and receives our ai fields, not even sure if we need this but w/e
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(_soulConsumeCooldown);
            writer.Write(AI_FrameCount);

            base.SendExtraAI(writer);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            _soulConsumeCooldown = reader.ReadInt32();
            AI_FrameCount = reader.ReadInt32();

            base.ReceiveExtraAI(reader);
        }
    }

    #endregion
}