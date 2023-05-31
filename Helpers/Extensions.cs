using OracleClass.Content.DamageClasses;
using OracleClass.Helpers.Abstracts;
using Terraria;

namespace OracleClass.Helpers;

public static class Extensions
{
	/// <summary>Returns whether or not a given item is an Oracle weapon</summary>
	public static bool IsOracleWeapon(this Item item) => item.CountsAsClass<OracleDamageClass>();

	/// <summary>
	///     Returns the <c>OracleWeapon</c> instance for a given item
	///     <para />
	///     Does <b>not</b> perform null checks, use <c>TryGetOracleWeapon</c> if you need them
	/// </summary>
	public static OracleWeapon GetOracleWeapon(this Item item) => item.ModItem as OracleWeapon;

	/// <summary>
	///     Gets the <c>OracleWeapon</c> instance for a given item
	///     <para />
	///     This will perform null checks, use <c>GetOracleWeapon</c> if you don't need them
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
	///     Returns the <c>OracleWeaponType</c> for a given item
	///     <para />
	///     Does <b>not</b> perform null checks, make sure to check <c>item.ModItem is not null</c> if applicable
	/// </summary>
	public static Enums.OracleWeaponType GetOracleWeaponType(this Item item) => item.GetOracleWeapon().OracleType;
}