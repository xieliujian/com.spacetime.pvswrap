using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public static class PVSBakerUtils
    {
        /// <summary>
        /// Determines if a GameObject should be exported to the database.
        /// </summary>
        /// <param name="_gameObj"></param>
        /// <returns></returns>
        public static bool IsGameObjExportDB(GameObject _gameObj)
        {
            var script = _gameObj.GetComponent<PVSGizmos>();
            if (script != null)
                return false;

            return true;
        }

        /// <summary>
        /// Calculates the alpha value for a material based on its properties.
        /// </summary>
        /// <param name="_material"></param>
        /// <returns></returns>
        public static float CalcMaterialAlphaByBaker(Material _material)
        {
            var isWater = PVSUtil.IsStylizedWater(_material);

            float alpha = 0.0f;
            if (isWater)
            {
                alpha = PVSDefine.s_Baker_AlphaClip_95;
            }
            else
            {
                alpha = CalcMaterialAlphaByBaker_Default(_material);
            }

            return alpha;
        }

        /// <summary>
        /// Calculates the alpha value for a material based on its properties.
        /// </summary>
        /// <param name="_material"></param>
        /// <returns></returns>
        static float CalcMaterialAlphaByBaker_Default(Material _material)
        {
            float alpha = 0.0f;
            bool isTransparent = PVSUtil.IsMaterialTransparent(_material);
            bool isTransparent2 = false;
            var surfacePropId = PVSDefine.s_ShaderSurfacePropId;

            if (_material != null)
            {
                if (_material.HasProperty(surfacePropId))
                {
                    if (_material.GetFloat(surfacePropId) == 1)
                    {
                        isTransparent2 = true;
                    }

                    if (_material.GetFloat(surfacePropId) == 2)
                    {
                        isTransparent = false;
                    }
                }
            }

            alpha = isTransparent ? PVSDefine.s_Baker_AlphaClip_75 : 0;
            if (isTransparent2)
            {
                alpha = PVSDefine.s_Baker_AlphaClip_95;
            }

            return alpha;
        }
    }
}
