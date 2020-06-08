using System;

namespace SubnauticaMap
{
	[Flags]
	public enum PingTypeFlags : uint
	{
		Lifepod = 0x1u,
		Seamoth = 0x2u,
		Cyclops = 0x4u,
		Exosuit = 0x8u,
		Rocket = 0x10u,
		Beacon = 0x20u,
		Signal = 0x40u,
		MapRoomCamera = 0x80u,
		Sunbeam = 0x100u
	}
}
