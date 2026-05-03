using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS Camera component.
    /// </summary>
    public partial class PVSCamera
    {
#if UNITY_EDITOR

        /// <summary>
        /// GUI last frame hash to avoid unnecessary updates.
        /// </summary>
        int m_guiLastFrameHash = -1;
        string m_guiLastText = null;

        /// <summary>
        /// Displays the GUI for the PVS Camera stats in-game.
        /// </summary>
        void OnGUI()
        {
            DrawGUI_InGameStats();
        }

        /// <summary>
        /// 
        /// </summary>
        void DrawGUI_InGameStats()
        {
            if (!showInGameStats)
                return;

            if (m_guiLastFrameHash != s_LastFrameHash || m_guiLastText == null)
            {
                m_guiLastText = "* PVS Stats *\n"
                                + $"Total renderers: {lastTotal}\n"

                                + $" - Culled: {lastCulled} ({Mathf.Round((lastCulled / (float)lastTotal) * 100f)}%)\n"
                                + $" - Visible: {lastVisible}\n"
                                + $" - Hash: {s_LastFrameHash}\n";

                m_guiLastFrameHash = s_LastFrameHash;
            }

            GUI.skin.box.fontSize = 30;
            GUI.skin.box.alignment = TextAnchor.UpperLeft;
            GUILayout.Box(m_guiLastText, System.Array.Empty<GUILayoutOption>());
        }
#endif
    }
}

