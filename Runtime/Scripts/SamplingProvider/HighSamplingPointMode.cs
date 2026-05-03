using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    internal class HighSamplingPointMode : BaseSamplingPointMode
    {
        /// <summary>
        /// 
        /// </summary>
        public HighSamplingPointMode()
        {
            sampleMode = PVSSamplingPointMode.High;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <returns></returns>
        public override bool CanSample(Vector3 _pos, float _camMaxDisOffset)
        {
#if ART_SCENE_PROJECT

            CheckCurCanWalk(_pos, _camMaxDisOffset, out Vector3 voxelPos, out bool canWalk);
            CheckAreaRangeCanWalk(_pos, _camMaxDisOffset, ref voxelPos, ref canWalk);
            if (!canWalk)
                return false;

            return true;

#else
            return false;
#endif
        }

#if ART_SCENE_PROJECT

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <param name="_voxelPos"></param>
        /// <param name="_canWalk"></param>
        void CheckAreaRangeCanWalk(Vector3 _pos, float _camMaxDisOffset, ref Vector3 _voxelPos, ref bool _canWalk)
        {
            if (_canWalk)
                return;

            var offsetArray = PVSDefine.s_OffsetArray;
            float offUnitSize = PVSDefine.s_OffsetUnitSize;
            for (int i = 0; i < offsetArray.Length; i++)
            {
                var posX = _pos.x + offsetArray[i].x * offUnitSize;
                var posZ = _pos.z + offsetArray[i].z * offUnitSize;
                var posY = _pos.y;
                Vector3 newPos = new Vector3(posX, posY, posZ);
                var isSamePos = IsSamePoint(posX, posY, posZ, _pos);
                if (isSamePos)
                    continue;

                var isCanWalk = InCanWalkVoxelArea(newPos, _camMaxDisOffset, out Vector3 voxelPos1);
                if (isCanWalk)
                {
                    _canWalk = true;
                    _voxelPos.y = voxelPos1.y;
                    break;
                }
            }
        }

#endif
    }
}

