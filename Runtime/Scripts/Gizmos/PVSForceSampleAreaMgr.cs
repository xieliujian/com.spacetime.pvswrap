
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// Manager for handling sample areas in PVS Force mode.
    /// </summary>
    public class PVSForceSampleAreaMgr
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSForceSampleAreaMgr s_Instance;

        /// <summary>
        /// Singleton instance of the PVSForceSampleAreaMgr.
        /// </summary>
        public static PVSForceSampleAreaMgr S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSForceSampleAreaMgr();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// List of MeshFilters that are part of the sample areas.
        /// </summary>
        List<Vector3> m_AreaPosList = new List<Vector3>();

        /// <summary>
        /// 
        /// </summary>
        public float calcTime;

        /// <summary>
        /// 
        /// </summary>
        Dictionary<int, bool> m_ForceSampleIdxDict = new Dictionary<int, bool>();

        /// <summary>
        /// 
        /// </summary>
        public List<Vector3> areaPosList
        {
            get
            {
                return m_AreaPosList;
            }
        }

        /// <summary>
        /// Initializes the PVSForceSampleAreaMgr.
        /// </summary>
        public void Collect()
        {
            var areaObjList = PVSWrapBridge.onGetForceSampleAreaObjList(true);

            InitCalcTime();
            InitAreaPosList(areaObjList);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Destroy()
        {
            PVSWrapBridge.onGetForceSampleAreaObjList(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sampleIndex"></param>
        /// <returns></returns>
        public bool IsForceSampleIdx(int _sampleIndex)
        {
            if (m_ForceSampleIdxDict.ContainsKey(_sampleIndex))
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSampleNeighbor"></param>
        /// <param name="_volumePos"></param>
        /// <param name="_volumeRot"></param>
        /// <param name="_volumeSize"></param>
        /// <param name="_cellNumVec"></param>
        /// <param name="_cellSize"></param>
        public void CalcForceSampleIdxDict(bool isSampleNeighbor, Vector3 _volumePos, Quaternion _volumeRot, Vector3 _volumeSize, Vector3 _cellNumVec, Vector3 _cellSize)
        {
            m_ForceSampleIdxDict.Clear();

            foreach (var areaPos in areaPosList)
            {
                var cellIndex = GridMath.GetIndexForWorldPos(areaPos, _volumePos, _volumeRot, 
                    _volumeSize, _volumeRot, _cellNumVec, _cellSize, out _, out _);
                m_ForceSampleIdxDict[cellIndex] = true;

                if (isSampleNeighbor)
                {
                    GridMath.UnflattenToXYZ(cellIndex, out int x, out int y, out int z, _cellNumVec);

                    // 取最多点位的数组
                    var offsetArray = PVSDefine.s_CamSamplePosOffsetArray;
                    foreach(var offset in offsetArray)
                    {
                        var newX = x + offset.x;
                        var newY = y + offset.y;
                        var newZ = z + offset.z;
                        var newCellIndex = GridMath.FlattenXYZClamp((int)newX, (int)newY, (int)newZ, _cellNumVec);
                        m_ForceSampleIdxDict[newCellIndex] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="areaObjList"></param>
        void InitAreaPosList(List<GameObject> areaObjList)
        {
            m_AreaPosList = PVSAreaUtils.CollectWorldVertices(areaObjList);
        }

        /// <summary>
        /// 
        /// </summary>
        void InitCalcTime()
        {
            calcTime = 0;
        }
    }
}

