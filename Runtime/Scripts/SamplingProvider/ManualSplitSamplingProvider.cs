using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class ManualSplitSamplingProvider : IActiveSamplingProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public static string ManualSplitSamplingProviderName => nameof(ManualSplitSamplingProvider);

        /// <summary>
        /// 
        /// </summary>
        public string Name => ManualSplitSamplingProviderName;

        /// <summary>
        /// 
        /// </summary>
        ManualSplitSamplingPointMode m_Sample = new ManualSplitSamplingPointMode();

        /// <summary>
        /// 
        /// </summary>
        float m_CamMaxDisOffset;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_samplingPointMode"></param>
        /// <param name="_forceSampleExpPoint"></param>
        /// <param name="bakeCellSize"></param>
        public void InitializeSamplingProvider(PVSSamplingPointMode _samplingPointMode, 
            bool _forceSampleExpPoint, Vector3 bakeCellSize, float _camMaxDisOffset)
        {
            m_CamMaxDisOffset = _camMaxDisOffset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsSamplingPositionActive(Vector3 pos)
        {
            var isValid = m_Sample.CanSample(pos, m_CamMaxDisOffset);
            if (!isValid)
                return false;

            return true;
        }
    }

}
