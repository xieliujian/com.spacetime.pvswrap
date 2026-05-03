using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ST.Core.Logging.Logger;

#if LR_SCENE_MERGE_ENABLED
using LR.Scene.SceneMerge;
#endif

namespace ST.PVS
{
#if LR_SCENE_MERGE_ENABLED
    /// <summary>
    /// 
    /// </summary>
    public class PVSSceneMergeUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_objDict"></param>
        /// <param name="_outRenderSet"></param>
        public static void FillBakeGroupSet(Dictionary<int, List<GameObject>> _objDict, HashSet<List<Renderer>> _renderListSet)
        {
            var exportCfg = SceneMergeUtils.FindExportConfig();
            if (exportCfg == null)
            {
                Logger.LogError("[PVS] PVSSceneMergeUtils.FillBakeGroupSet exportCfg == null");
                return;
            }

            // 1.
            var cellList = new List<SceneMergeExportCell>();
            FillBakeGroupOrFillCellNode(exportCfg, _objDict, _renderListSet, cellList);

            // 2.
            var cellExportBlockList = SceneMergePVSUtils.CreateCellExportBlockList(exportCfg, cellList);
            var splitCellExportBlockList = SceneMergePVSUtils.SplitCellExportBlockList(cellExportBlockList);
            var pvsVolume = PVSVolumeUtils.GetCullingVolume();
            if (pvsVolume != null)
            {
                pvsVolume.mergeExportBlockArray = splitCellExportBlockList.ToArray();
            }

            // 3.
            FillMergeBakeGroup(_renderListSet, splitCellExportBlockList);
        }

        /// <summary>
        /// 
        /// </summary>
        static void FillMergeBakeGroup(HashSet<List<Renderer>> _renderListSet, List<SceneMergeCellExportBlock> _cellExportBlockList)
        {
            foreach (var block in _cellExportBlockList)
            {
                if (block == null)
                    continue;

                var renderList = block.GetRenderList();
                if (renderList == null || renderList.Count <= 0)
                    continue;

                _renderListSet.Add(renderList);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void FillBakeGroupOrFillCellNode(SceneMergeExportConfig _exportCfg, 
            Dictionary<int, List<GameObject>> _objDict, 
            HashSet<List<Renderer>> _renderListSet, 
            List<SceneMergeExportCell> _cellList)
        {
            foreach (var iter in _objDict)
            {
                foreach (var obj in iter.Value)
                {
                    var isMergeObj = SceneMergePVSUtils.IsMergeObj(_exportCfg, obj);
                    if (isMergeObj)
                    {
                        SceneMergePVSUtils.CreateCellNode(_cellList, _exportCfg, null, obj);
                    }
                    else
                    {
                        var isObjCollect = PVSVolumeUtils.IsObjCollectInBakeGroup(obj, out List<Renderer> renderers);
                        if (!isObjCollect)
                            continue;

                        _renderListSet.Add(renderers);
                    }
                }
            }
        }
    }
#endif
}
