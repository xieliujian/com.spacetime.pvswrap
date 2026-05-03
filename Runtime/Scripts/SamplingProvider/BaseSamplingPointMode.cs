using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    internal abstract class BaseSamplingPointMode
    {
        /// <summary>
        /// 
        /// </summary>
        public PVSSamplingPointMode sampleMode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        public abstract bool CanSample(Vector3 _pos, float _camMaxDisOffset);

#if ART_SCENE_PROJECT

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <param name="_voxelPos"></param>
        /// <param name="_canWalk"></param>
        protected virtual void CheckCurCanWalk(Vector3 _pos, float _camMaxDisOffset, out Vector3 _voxelPos, out bool _canWalk)
        {
            _canWalk = PVSWrapBridge.voxelIsInCanWalkVoxelArea(_pos, out _voxelPos);

            var isCanWalkDistance = CheckCanWalkDistance(_pos, _voxelPos, _camMaxDisOffset);
            if (!isCanWalkDistance)
            {
                _canWalk = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pos"></param>
        /// <param name="_voxelPos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <returns></returns>
        protected virtual bool CheckCanWalkDistance(Vector3 _pos, Vector3 _voxelPos, float _camMaxDisOffset)
        {
            float minDistance = PVSDefine.s_GameCamMinDistance;
            float maxDistance = _camMaxDisOffset;
            float yDistance = _pos.y - _voxelPos.y;
            if (yDistance > minDistance && yDistance < maxDistance)
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_posX"></param>
        /// <param name="_posY"></param>
        /// <param name="_posZ"></param>
        /// <param name="_srcPos"></param>
        /// <returns></returns>
        protected bool IsSamePoint(float _posX, float _posY, float _posZ, Vector3 _srcPos)
        {
            var isValid = (_posX == _srcPos.x && _posY == _srcPos.y && _posZ == _srcPos.z);
            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_newPos"></param>
        /// <param name="_camMaxDisOffset"></param>
        /// <param name="_voxelPos1"></param>
        /// <returns></returns>
        protected bool InCanWalkVoxelArea(Vector3 _newPos, float _camMaxDisOffset, out Vector3 _voxelPos1)
        {
            bool isCanWalk = false;

            var isVoxelArea = PVSWrapBridge.voxelIsInCanWalkVoxelArea(_newPos, out _voxelPos1);
            if (isVoxelArea)
            {
                var isCanWalkDistance = CheckCanWalkDistance(_newPos, _voxelPos1, _camMaxDisOffset);
                if (isCanWalkDistance)
                {
                    isCanWalk = true;
                }
            }

            return isCanWalk;
        }
#endif
    }
}


