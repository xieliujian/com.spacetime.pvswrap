using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    
    public class PVSSamplePosOffset : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        Renderer m_Render;
        MaterialPropertyBlock m_Mpb;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public PVSSamplePosOffsetMgr offsetMgr;
        [HideInInspector]
        public bool isMainPos;
        [HideInInspector]
        public bool isOffsetMaskPos;
        [HideInInspector]
        public bool isEmptyPos;
        [HideInInspector]
        public string cubeName;
        [HideInInspector]
        public GameObject go;

        /// <summary>
        /// 
        /// </summary>
        public int posIndex;
        public Vector3 pos;
        public Vector3 cellSize;

        /// <summary>
        /// 
        /// </summary>
        public bool isIgnore;
        public bool isSceneShow;

        /// <summary>
        /// 
        /// </summary>
        void Update()
        {
            RefreshName();
            RefreshPropertyBlock();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            InitBoxCollider();
            InitRender();
            InitMat();
            InitPropertyBlock();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            if (m_Mpb != null)
            {
                m_Mpb.Clear();
                m_Mpb = null;
            }

            if (go != null)
            {
                GameObject.Destroy(go);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshSceneShow()
        {
            if (offsetMgr == null)
                return;

            offsetMgr.RefreshSceneShow();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_visible"></param>
        public void SetVisible(bool _visible)
        {
            if (m_Render == null)
                return;

            m_Render.enabled = _visible;
        }

        /// <summary>
        /// 
        /// </summary>
        void InitBoxCollider()
        {
            if (go == null)
                return;

            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;

            GameObject.Destroy(collider);
        }

        /// <summary>
        /// 
        /// </summary>
        void InitMat()
        {
            if (m_Render == null)
                return;

            var mat = m_Render.sharedMaterial;
            if (mat == null)
                return;

            mat.SetFloat("_ReceiveShadows", 0);
            mat.DisableKeyword("_RECEIVE_SHADOWS_OFF");
        }

        /// <summary>
        /// 
        /// </summary>
        void InitRender()
        {
            if (go == null)
                return;

            m_Render = go.GetComponent<Renderer>();
            m_Render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Render.receiveShadows = false;
        }

        /// <summary>
        /// 
        /// </summary>
        void RefreshName()
        {
            var name = cubeName;
            if (isMainPos)
            {
                name += " (Main)";
            }

            if (isOffsetMaskPos)
            {
                name += " (Mask)";
            }

            if (isEmptyPos)
            {
                name += " (Empty)";
            }

            if (isIgnore)
            {
                name += " (Ignore)";
            }

            if (isSceneShow)
            {
                name += " (Show)";
            }

            gameObject.name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        void RefreshPropertyBlock()
        {
            if (m_Render == null)
                return;

            Color color = Color.green;
            if (isMainPos)
            {
                color = Color.white;
            }
            else if (isOffsetMaskPos)
            {
                color = Color.gray;
            }
            else if (isEmptyPos)
            {
                color = Color.gray;
            }

            m_Mpb.SetColor("_BaseColor", color);
            m_Render.SetPropertyBlock(m_Mpb);
        }

        /// <summary>
        /// 
        /// </summary>
        void InitPropertyBlock()
        {
            m_Mpb = new MaterialPropertyBlock();
            RefreshPropertyBlock();
        }
    }
}

