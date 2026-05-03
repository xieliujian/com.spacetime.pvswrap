
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSVolumeBakeData
    {
        /// <summary>
        /// Adds neighbor cell content to each cell then downsamples the entire grid.
        /// </summary>
        public void MergeDownsample()
        {
            if (cellCount.x == 1 && cellCount.y == 1 && cellCount.z == 1)
            {
                Debug.LogWarning("Unable to downsample any further.");

                return;
            }

            PVSVolumeBakeData tmpBakeData = ScriptableObject.CreateInstance<PVSVolumeBakeData>();

            // Half resolution; divide by two
            Vector3Int OptimizedCellSize = new Vector3Int(2, 2, 2);

            Vector3Int newTmpDim = new Vector3Int(
                (int)((cellCount.x % OptimizedCellSize.x == 0)
                    ? cellCount.x
                    : cellCount.x + OptimizedCellSize.x - (cellCount.x % OptimizedCellSize.x)),

                (int)((cellCount.y % OptimizedCellSize.y == 0)
                    ? cellCount.y
                    : cellCount.y + OptimizedCellSize.y - (cellCount.y % OptimizedCellSize.y)),

                (int)((cellCount.z % OptimizedCellSize.z == 0)
                    ? cellCount.z
                    : cellCount.z + OptimizedCellSize.z - (cellCount.z % OptimizedCellSize.z)));

            Vector3Int optDim = new Vector3Int(
                ((int)newTmpDim.x / (int)OptimizedCellSize.x),
                ((int)newTmpDim.y / (int)OptimizedCellSize.y),
                ((int)newTmpDim.z / (int)OptimizedCellSize.z));

            tmpBakeData.rawData = new RawData[optDim.x * optDim.y * optDim.z];

            tmpBakeData.cellCount = optDim; ;
            tmpBakeData.cellSize = new Vector3(cellSize.x * OptimizedCellSize.x, cellSize.y * OptimizedCellSize.y,
                cellSize.z * OptimizedCellSize.z);

            int totalCount = Mathf.CeilToInt(cellCount.x * cellCount.y * cellCount.z);


#if false
	        HashSet<ushort> tmpHash = new HashSet<ushort>();
	        
	        for (int index = 0; index < totalCount; ++index)
	        {
		        GridMath.UnflattenToXYZ(index, out int x, out int y, out int z, cellCount);

		        int optX = x / (int)OptimizedCellSize.x;
		        int optY = y / (int)OptimizedCellSize.y;
		        int optZ = z / (int)OptimizedCellSize.z;
		        
		        int tmpBakeDataSampleIndex = GridMath.FlattenXYZ(optX, optY, optZ, optDim);
		        
		        tmpHash.Clear();
		        
		        // Merge neighbor cells
		        for (int xx = -1; xx <= 1; ++xx)
		        {
			        for (int yy = -1; yy <= 1; ++yy)
			        {
				        for (int zz = -1; zz <= 1; ++zz)
				        {
					        if (!GridMath.IsXYZInBounds(x + xx, y + yy, z + zz, cellCount))
					        {
						        continue;
					        }

					        int sampleIndex = GridMath.FlattenXYZ(x + xx, y + yy, z + zz, cellCount);

					        if (rawData[sampleIndex].uncompressed == null)
					        {
						        continue;
					        }
					        
					        foreach (ushort neighborIndex in rawData[sampleIndex].uncompressed)
					        {
						        tmpHash.Add(neighborIndex);
					        }
				        }
			        }
		        }
		        
		        // Add existing indices back in or they would be lost
		        if (tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed != null)
		        {
			        foreach (ushort existingIndex in tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed)
			        {
				        tmpHash.Add(existingIndex);
			        }
		        }

		        tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed = tmpHash.ToArray();
		        
		        
#if UNITY_EDITOR
		        if (index % 128 == 0)
		        {
			        UnityEditor.EditorUtility.DisplayProgressBar("Performing Merge-Downsample step",
				        $"Cell: {index}/{totalCount}", index / (float) totalCount);
		        }
#endif
	        }
#else
            const int batchSize = 32;

            IEnumerable<IGrouping<int, int>> batches = Enumerable.Range(0, totalCount)
                .GroupBy(val => (val % batchSize));

            int processedElementCount = 0;

            var downsampleTasks = batches.Select(groups =>
            {
#pragma warning disable 1998
                return Task.Run(async () =>
#pragma warning restore 1998
                {
                    HashSet<ushort> tmpHash = new HashSet<ushort>();

                    int groupSize = 0;

                    foreach (var index in groups)
                    {
                        GridMath.UnflattenToXYZ(index, out int x, out int y, out int z, cellCount);

                        int optX = x / (int)OptimizedCellSize.x;
                        int optY = y / (int)OptimizedCellSize.y;
                        int optZ = z / (int)OptimizedCellSize.z;

                        int tmpBakeDataSampleIndex = GridMath.FlattenXYZ(optX, optY, optZ, optDim);

                        tmpHash.Clear();

                        // Merge neighbor cells
                        for (int xx = -1; xx <= 1; ++xx)
                        {
                            for (int yy = -1; yy <= 1; ++yy)
                            {
                                for (int zz = -1; zz <= 1; ++zz)
                                {
                                    if (!GridMath.IsXYZInBounds(x + xx, y + yy, z + zz, cellCount))
                                    {
                                        continue;
                                    }

                                    int sampleIndex = GridMath.FlattenXYZ(x + xx, y + yy, z + zz, cellCount);

                                    if (rawData[sampleIndex].uncompressed == null)
                                    {
                                        continue;
                                    }

                                    foreach (ushort neighborIndex in rawData[sampleIndex].uncompressed)
                                    {
                                        tmpHash.Add(neighborIndex);
                                    }
                                }
                            }
                        }

                        // Add existing indices back in or they would be lost
                        if (tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed != null)
                        {
                            foreach (ushort existingIndex in tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed)
                            {
                                tmpHash.Add(existingIndex);
                            }
                        }

                        tmpBakeData.rawData[tmpBakeDataSampleIndex].uncompressed = tmpHash.ToArray();

                        ++groupSize;
                    }

                    System.Threading.Interlocked.Add(ref processedElementCount, groupSize);

                });
            });

            var task = Task.WhenAll(downsampleTasks);
            var taskAwaiter = task.GetAwaiter();

            for (int currentValue = 0; currentValue != totalCount; currentValue = 
                System.Threading.Interlocked.CompareExchange(ref processedElementCount, 0, 0))
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Performing Merge-Downsample step",
                    $"Cell: {currentValue}/{totalCount}", currentValue / (float)totalCount);
#endif

                if (task.Wait(100))
                {
                    // Task finished within timeout and we are done.
                    break;
                }
            }

            System.Diagnostics.Debug.Assert(processedElementCount == totalCount);

            // Unnecessary but just for completeness.
            taskAwaiter.GetResult();
#endif

            rawData = tmpBakeData.rawData;
            cellCount = tmpBakeData.cellCount;
            cellSize = tmpBakeData.cellSize;

            GameObject.DestroyImmediate(tmpBakeData);
        }
    }
}
