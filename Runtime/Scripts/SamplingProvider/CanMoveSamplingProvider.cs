using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class CanMoveSamplingProvider : IActiveSamplingProvider
    {
        public static string CanMoveSamplingProviderName =>  nameof(CanMoveSamplingProvider);
        
        public string Name => CanMoveSamplingProviderName;

        PVSSamplingPointMode m_SamplingPointMode;

        bool m_ForceSampleExpPoint = false;
        float m_CamMaxDisOffset;

        Vector3 bakeCellSize;

        Dictionary<PVSSamplingPointMode, BaseSamplingPointMode> m_SampleDic = new Dictionary<PVSSamplingPointMode, BaseSamplingPointMode>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplingPointMode"></param>
        /// <param name="_forceSampleExpPoint"></param>
        /// <param name="_bakeCellSize"></param>
        public void InitializeSamplingProvider(PVSSamplingPointMode _samplingPointMode,
            bool _forceSampleExpPoint, Vector3 _bakeCellSize, float _camMaxDisOffset)
        {
            m_SamplingPointMode = _samplingPointMode;
            m_ForceSampleExpPoint = _forceSampleExpPoint;
            bakeCellSize = _bakeCellSize;
            m_CamMaxDisOffset = _camMaxDisOffset;

            m_SampleDic.Clear();
            AddSampleDict(new HighSamplingPointMode());
            AddSampleDict(new HighLeftRightSamplingPointMode());
            AddSampleDict(new HighLeftRightDownSamplingPointMode());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsSamplingPositionActive(Vector3 pos)
        {
            var isValid = IsInCanMoveArea(pos);
            if (isValid)
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        bool IsInCanMoveArea(Vector3 pos)
        {
            var ignorePointAreaMgr = PVSIgnorePointAreaGizmosMgr.S;
            if (ignorePointAreaMgr != null)
            {
                var isInIgnoreArea = ignorePointAreaMgr.IsPointInGizmos(pos);
                if (isInIgnoreArea)
                    return false;
            }

            var sample = FindSample(m_SamplingPointMode);
            if (sample == null)
                return false;

            var isValid = sample.CanSample(pos, m_CamMaxDisOffset);
            if (!isValid)
                return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_sample"></param>
        void AddSampleDict(BaseSamplingPointMode _sample)
        {
            m_SampleDic.Add(_sample.sampleMode, _sample);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplingPointMode"></param>
        /// <returns></returns>
        BaseSamplingPointMode FindSample(PVSSamplingPointMode _samplingPointMode)
        {
            BaseSamplingPointMode _sample = null;
            m_SampleDic.TryGetValue(_samplingPointMode, out _sample);
            return _sample;
        }
    }
}

