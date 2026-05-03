using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSGizmosInspector : Editor
    {
        /// <summary>
        /// 
        /// </summary>
        protected string m_Desc;

        /// <summary>
        /// 
        /// </summary>
        public override void OnInspectorGUI()
        {
            var script = (PVSGizmos)target;
            if (script == null)
                return;

            var style = PVSEditUtils.GetMiddleTextStyle();
            GUILayout.Label(PVSDefine.s_StartAreaDesc, style);
            GUILayout.Label(m_Desc, style);
            GUILayout.Label(PVSDefine.s_EndAreaDesc, style);

            base.OnInspectorGUI();

            DrawDebug(script);
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawDebug(PVSGizmos _script)
        {
            if (!_script.isOpenDebug)
                return;

            _script.debugPos = EditorGUILayout.Vector3Field("调试位置", _script.debugPos);

            if (GUILayout.Button("测试调试位置"))
            {
                if (_script.IsPointInGizmos(_script.debugPos))
                {
                    Logger.LogError($"{_script.debugPos} 在区域内");
                }
                else
                {
                    Logger.LogError($"{_script.debugPos} 不在区域内");
                }
            }
        }
    }
}
