using System;

namespace SubnauticaMap
{
	[Flags]
	public enum MapIconFlags : uint
	{
		Wreck = 0x1u,
		HeatArea = 0x2u,
		PrecursorSurfacePipe = 0x4u
	}
}
