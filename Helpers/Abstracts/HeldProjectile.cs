using System;
using System.IO;
using Microsoft.Xna.Framework;
using OracleClass.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace OracleClass.Helpers.Abstracts;

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
        public Enums.OracleWeaponType OracleType { get; set; }

        /// <summary>How much this projectile should be rotated when it points to the mouse</summary>
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
        /// <param name="totalSoulConsumed">The total amount of soul this item consumed while charging</param>
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
            if (!Owner.channel || Owner.HeldItem.GetOracleWeapon().CurSoulCapacity <= 0 || Owner.CCed) {
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