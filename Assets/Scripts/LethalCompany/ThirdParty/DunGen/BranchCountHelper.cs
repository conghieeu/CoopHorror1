using System;
using System.Linq;
using DunGen.Graph;
using UnityEngine;

namespace DunGen
{
	public static class BranchCountHelper
	{
		public static void ComputeBranchCounts(DungeonFlow dungeonFlow, RandomStream randomStream, DungeonProxy proxyDungeon, ref int[] mainPathBranches)
		{
			switch (dungeonFlow.BranchMode)
			{
			case BranchMode.Local:
				ComputeBranchCountsLocal(randomStream, proxyDungeon, ref mainPathBranches);
				break;
			case BranchMode.Global:
				ComputeBranchCountsGlobal(dungeonFlow, randomStream, proxyDungeon, ref mainPathBranches);
				break;
			default:
				throw new NotImplementedException($"{typeof(BranchMode).Name}.{dungeonFlow.BranchMode} is not implemented");
			}
		}

		private static void ComputeBranchCountsLocal(RandomStream randomStream, DungeonProxy proxyDungeon, ref int[] mainPathBranches)
		{
			for (int i = 0; i < mainPathBranches.Length; i++)
			{
				TileProxy tileProxy = proxyDungeon.MainPathTiles[i];
				if (!(tileProxy.Placement.Archetype == null))
				{
					int random = tileProxy.Placement.Archetype.BranchCount.GetRandom(randomStream);
					random = Mathf.Min(random, tileProxy.UnusedDoorways.Count());
					mainPathBranches[i] = random;
				}
			}
		}

		private static void ComputeBranchCountsGlobal(DungeonFlow dungeonFlow, RandomStream randomStream, DungeonProxy proxyDungeon, ref int[] mainPathBranches)
		{
			int random = dungeonFlow.BranchCount.GetRandom(randomStream);
			int num = proxyDungeon.MainPathTiles.Count((TileProxy t) => t.Placement.Archetype != null);
			float num2 = (float)random / (float)num;
			float num3 = num2;
			int num4 = random;
			for (int num5 = 0; num5 < mainPathBranches.Length; num5++)
			{
				if (num4 <= 0)
				{
					break;
				}
				TileProxy tileProxy = proxyDungeon.MainPathTiles[num5];
				if (!(tileProxy.Placement.Archetype == null))
				{
					int num6 = tileProxy.UnusedDoorways.Count();
					int num7 = Mathf.FloorToInt(num3);
					num7 = Mathf.Min(num7, num6, tileProxy.Placement.Archetype.BranchCount.Max, num4);
					num3 -= (float)num7;
					if (num7 < num6 && num7 < num4 && randomStream.NextDouble() < (double)num3)
					{
						num7++;
						num3 = 0f;
					}
					num3 += num2;
					num4 -= num7;
					mainPathBranches[num5] = num7;
				}
			}
		}
	}
}
