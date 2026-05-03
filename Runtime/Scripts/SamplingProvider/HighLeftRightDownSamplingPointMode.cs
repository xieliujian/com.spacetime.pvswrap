using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    internal class HighLeftRightDownSamplingPointMode : BaseSamplingPointMode
    {
        /// <summary>
        /// 
        /// </summary>
        public HighLeftRightDownSamplingPointMode()
        {
            sampleMode = PVSSamplingPointMode.High_LeftRight_Down;
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
            var isDownMaskValid = CheckDownMaskInvalid(_pos, _camMaxDisOffset);
            CheckAreaRangeCanWalk(_pos, isDownMaskValid, _camMaxDisOffset, ref voxelPos, ref canWalk);
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
        /// <param name="_isDownMaskValid"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <param name="_voxelPos"></param>
        /// <param name="_canWalk"></param>
        void CheckAreaRangeCanWalk(Vector3 _pos, bool _isDownMaskValid, float _camMaxDisOffset, ref Vector3 _voxelPos, ref bool _canWalk)
        {
            if (_canWalk)
                return;

            var offsetArray = PVSDefine.s_OffsetArray;
            float offUnitSize = PVSDefine.s_OffsetUnitSize;
            int maxLoop = (int)(_camMaxDisOffset / offUnitSize);
            int maxZLoop = (int)(PVSDefine.s_GameCamMaxZDownDis / offUnitSize);
            for (int horizontal = -maxLoop; horizontal <= maxLoop; horizontal++)
            {
                int verticalEnd = _isDownMaskValid ? 1 : maxZLoop;

                for (int vertical = 0; vertical <= verticalEnd; vertical++)
                {
                    for (int i = 0; i < offsetArray.Length; i++)
                    {
                        var posX = _pos.x + offsetArray[i].x * offUnitSize * horizontal;
                        var posZ = _pos.z + offsetArray[i].z * offUnitSize * horizontal;
                        var posY = _pos.y + offUnitSize * vertical;
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

                    if (_canWalk)
                        break;
                }

                if (_canWalk)
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <returns></returns>
        bool CheckDownMaskInvalid(Vector3 _pos, float _camMaxDisOffset)
        {
            int layer = 1 << PVSConstants.CamBakeLayer | 1 << PVSConstants.CamBakeDisLayer;
            var cellSize = Vector3.one;
            var dir = Vector3.down;
            var distance = _camMaxDisOffset;

            _pos.y += distance;

            List<Vector3> posList = new List<Vector3>();
            posList.Add(_pos);

            PVSVolume.AddSamplePosOffsetRayList(posList, PVSSamplePosOffsetMask.DownMask, _pos, cellSize, 0.5f);

            bool isAllHit = true;
            foreach (var newPos in posList)
            {
                var isHit = Physics.Raycast(newPos, dir, distance, layer);
                if (!isHit)
                {
                    isAllHit = false;
                    break;
                }
            }

            if (isAllHit)
            {
                return true;
            }

            return false;
        }

#endif
    }
}
