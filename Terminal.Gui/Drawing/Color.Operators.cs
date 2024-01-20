using System.Diagnostics.Contracts;
using System.Numerics;
namespace Terminal.Gui;

public readonly partial record struct Color {
	/// <summary>
	/// Implicit conversion from <see langword="int"/> to <see cref="Color"/>, via the <see cref="Color(int)"/> costructor.
	/// </summary>
	[Pure]
	public static implicit operator Color ( int rgba ) => new ( rgba );

	/// <summary>
	/// Implicit conversion from <see cref="Color"/> to <see langword="int"/> by returning the value of the <see cref="Rgba"/> field.
	/// </summary>
	[Pure]
	public static implicit operator int (Color color) => color.Rgba;

	/// <summary>
	/// Implicit conversion from <see langword="uint"/> to <see cref="Color"/>, via the <see cref="Color(uint)"/> costructor.
	/// </summary>
	[Pure]
	public static implicit operator Color ( uint u ) => new ( u );

	/// <summary>
	/// Implicit conversion from <see cref="Color"/> to <see langword="uint"/> by returning the value of the <see cref="Argb"/> field.
	/// </summary>
	[Pure]
	public static implicit operator uint (Color color) => color.Argb;

	/// <summary>
	/// Implicit conversion from <see cref="ColorName"/> to <see cref="Color"/> via <see cref="FromColorName"/>.
	/// </summary>
	[Pure]
	public static implicit operator Color ( ColorName colorName ) => FromColorName ( colorName );

	/// <summary>
	/// Implicit conversion from <see cref="Vector4"/> to <see cref="Color"/>, where (<see cref="Vector4.X"/>, <see cref="Vector4.Y"/>, <see cref="Vector4.Z"/>, <see cref="Vector4.W"/>) is (<see cref="A"/>,<see cref="R"/>,<see cref="G"/>,<see cref="B"/>),
	/// via <see cref="Color(int,int,int,int)"/>.
	/// </summary>
	/// <param name="v"></param>
	[Pure]
	public static implicit operator Color ( Vector4 v ) => new ( (byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W );
	
	/// <summary>
	/// Implicit conversion to <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>, <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
	/// </summary>
	/// <param name="color"></param>
	[Pure]
	public static implicit operator Vector4 ( Color color ) => new (color.A, color.B, color.G, color.R );
	/// <summary>
	/// Implicit conversion from <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>, <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
	/// </summary>
	/// <param name="v"></param>
	[Pure]
	public static implicit operator Color ( Vector3 v ) => new ( (byte)v.X, (byte)v.Y, (byte)v.Z );
	/// <summary>
	/// Implicit conversion from <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>, <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
	/// </summary>
	/// <param name="color"></param>
	[Pure]
	public static explicit operator Vector3 ( Color color ) =>  new( color.B, color.G, color.R );
}
