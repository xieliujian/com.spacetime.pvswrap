using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;


namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PVSBakeDataUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vPos"></param>
        /// <returns></returns>
        public static int PosToIndex(Vector3 vPos)
        {
            int mapSize = (int)PVSDefine.s_MapSize;
            int chunkSize = PVSDefine.s_ChunkSize;
            int nWidth = mapSize;
            int nHeight = mapSize;

            int nX = (int)((vPos.x + nWidth / 2) / chunkSize);
            int nZ = (int)((vPos.z + nHeight / 2) / chunkSize);

            int nIdx = nZ * ((int)(nWidth / chunkSize)) + nX;

            return nIdx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_chunkIdxArray"></param>
        /// <param name="_idxArray"></param>
        /// <returns></returns>
        public static byte[] SerializeWrite(bool _saveBigVisIndex, 
            ushort[] _chunkIdxArray, uint[] _idxArray)
        {
            VisibilityIndex[] visibilityIndex2;
            VisibilityUintIndex[] visibilityUintIndex2;
            ChunkIndex[] selectChunkIndex2;
            CompleteStream_SplatIndex(_idxArray, _chunkIdxArray, out visibilityIndex2, out visibilityUintIndex2, out selectChunkIndex2);

            if (visibilityIndex2.Length != selectChunkIndex2.Length)
            {
                Logger.LogError("[PVS][PVSBakeDataUtils][SerializeWrite] visibilityIndex2.Length != selectChunkIndex2.Length");
                return null;
            }

            RapidList<byte> saveBytes = new RapidList<byte>();
            RapidList<int> indexOffArray = new RapidList<int>();
            RapidList<byte> dataBytes = new RapidList<byte>();

            // 1.
            int offset = 0;
            for (int i = 0; i < selectChunkIndex2.Length; i++)
            {
                var chunkIndex = selectChunkIndex2[i];
                var idxIndex = visibilityIndex2[i];
                var idxUintIndex = visibilityUintIndex2[i];

                indexOffArray.Add(offset);
                offset = SerializeWrite(_saveBigVisIndex, dataBytes, ref chunkIndex, ref idxIndex, ref idxUintIndex);
            }

            // 2.
            for (int i = 0; i < indexOffArray.count; i++)
            {
                int sizeOff = indexOffArray[i] + indexOffArray.count * sizeof(int);
                var sizeByte = BitConverter.GetBytes(sizeOff);
                saveBytes.AddItems(sizeByte);
            }

            // 3.
            for (int i = 0; i < dataBytes.count; i++)
            {
                saveBytes.Add(dataBytes[i]);
            }

            return saveBytes.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_dataArray"></param>
        /// <param name="_posIdx"></param>
        /// <param name="_outChunkIdx"></param>
        /// <param name="_outRawIdx"></param>
        /// <param name="_outRawUintIdx"></param>
        public static void DeserializeRead(bool _saveBigVisIndex, byte[] _dataArray, int _posIdx, 
            out byte _outChunkIdx, out ushort _outRawIdx, out uint _outRawUintIdx)
        {
            _outChunkIdx = byte.MaxValue;
            _outRawIdx = ushort.MaxValue;
            _outRawUintIdx = uint.MaxValue;

            var splatSize = PVSDefine.s_BakeDataSplatSize;
            var chunkIdxOff = _posIdx / splatSize;
            var rawIdxOff = _posIdx % splatSize;

            var readerOff = chunkIdxOff * sizeof(int);
            var chunkReadOff = BitConverter.ToInt32(_dataArray, readerOff);
            DeserializeRead(_saveBigVisIndex, _dataArray, chunkReadOff, rawIdxOff, out _outChunkIdx, out _outRawIdx, out _outRawUintIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="_outSize"></param>
        /// <returns></returns>
        public static VisibilitySetRow[] CompressRawData(ushort[] indices, out int _outSize)
        {
            List<byte>[] temp = new List<byte>[256];
            for (int i = 0; i < indices.Length; ++i)
            {
                GridMath.FlattenUShort2Byte(indices[i], out byte height, out byte low);

                if (temp[height] == null)
                {
                    temp[height] = new List<byte>();
                }
                temp[height].Add(low);
            }

            int nUseCount = 0;
            for (int i = 0; i < temp.Length; i++)
            {
                if (temp[i] != null && temp[i].Count != 0)
                {
                    nUseCount++;
                }
            }

            VisibilitySetRow[] tempDatas = new VisibilitySetRow[nUseCount];
            int nCount = 0;
            int nSize = 0;
            for (int i = 0; i < temp.Length; i++)
            {
                if (temp[i] != null && temp[i].Count != 0)
                {
                    tempDatas[nCount].height = (byte)i;
                    nSize += sizeof(byte);
                    tempDatas[nCount].lowData = temp[i].ToArray();
                    nSize += tempDatas[nCount].lowData.Length * sizeof(byte);
                    nCount++;
                }
            }

            _outSize = nSize;

            return tempDatas;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_visibilityIndex"></param>
        /// <param name="_selectChunkIndex"></param>
        /// <param name="_visibilityIndex2"></param>
        /// <param name="_visibilityUintIndex2"></param>
        /// <param name="_selectChunkIndex2"></param>
        public static void CompleteStream_SplatIndex(uint[] _visibilityIndex, ushort[] _selectChunkIndex, 
            out VisibilityIndex[] _visibilityIndex2, out VisibilityUintIndex[] _visibilityUintIndex2,
            out ChunkIndex[] _selectChunkIndex2)
        {
            int nSplatMaxCount = _visibilityIndex.Length / PVSDefine.s_BakeDataSplatSize;
            int nSplatMod = _visibilityIndex.Length % PVSDefine.s_BakeDataSplatSize;
            if (nSplatMod > 0)
            {
                nSplatMaxCount = nSplatMaxCount + 1;
            }

            var visibilityIndex2 = new VisibilityIndex[nSplatMaxCount];
            var visibilityUintIndex2 = new VisibilityUintIndex[nSplatMaxCount];
            var selectChunkIndex2 = new ChunkIndex[nSplatMaxCount];

            int nNullCnt = 0;
            for (int i = 0; i < nSplatMaxCount; i++)
            {
                bool bIsNull = true;
                List<ushort> tempdata = new List<ushort>();
                List<uint> tempUnitdata = new List<uint>();
                List<ushort> chunkdata = new List<ushort>();
                int nMax = PVSDefine.s_BakeDataSplatSize;
                if (i == nSplatMaxCount - 1 && nSplatMod > 0)
                {
                    nMax = nSplatMod;
                }

                for (int n = 0; n < nMax; n++)
                {
                    var visTempIdx = i * PVSDefine.s_BakeDataSplatSize + n;
                    if (_visibilityIndex[visTempIdx] != uint.MaxValue)
                    {
                        bIsNull = false;
                    }

                    tempdata.Add((ushort)_visibilityIndex[visTempIdx]);
                    tempUnitdata.Add(_visibilityIndex[visTempIdx]);
                    chunkdata.Add(_selectChunkIndex[visTempIdx]);
                }

                if (bIsNull)
                {
                    tempdata.Clear();
                    chunkdata.Clear();
                    visibilityIndex2[i].data = tempdata.ToArray();
                    visibilityUintIndex2[i].data = tempUnitdata.ToArray();
                    selectChunkIndex2[i].data = chunkdata.ToArray();
                    nNullCnt++;
                }
                else
                {
                    visibilityIndex2[i].data = tempdata.ToArray();
                    visibilityUintIndex2[i].data = tempUnitdata.ToArray();
                    selectChunkIndex2[i].data = chunkdata.ToArray();
                }
            }

            _visibilityIndex2 = visibilityIndex2;
            _visibilityUintIndex2 = visibilityUintIndex2;
            _selectChunkIndex2 = selectChunkIndex2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_path"></param>
        public static void DeleteAssetsInFolder(string _path)
        {
#if UNITY_EDITOR
            // 检查路径是否存在
            if (!UnityEditor.AssetDatabase.IsValidFolder(_path))
                return;

            // 获取文件夹下的所有资源路径
            string[] assetPaths = UnityEditor.AssetDatabase.FindAssets("", new[] { _path });
            for (int i = 0; i < assetPaths.Length; i++)
            {
                assetPaths[i] = UnityEditor.AssetDatabase.GUIDToAssetPath(assetPaths[i]);
            }

            if (assetPaths.Length == 0)
                return;

            // 执行删除操作
            List<string> outInfo = new List<string>();
            bool result = UnityEditor.AssetDatabase.DeleteAssets(assetPaths, outInfo);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string CreateBinFolder(bool isCreateFolder)
        {
#if UNITY_EDITOR
            string occlusionName = "occlusion";

            string strPath = PVSBakeDataUtils.GetActiveScenePath();
            string dir = string.Format("{0}{1}", strPath, occlusionName);
            string path = "";
            string newPath = strPath.Remove(strPath.Length - 1);

            if (isCreateFolder)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
                {
                    string OcclusionName = occlusionName;
                    path = UnityEditor.AssetDatabase.CreateFolder(newPath, OcclusionName);
                }
            }

            Debug.Log("new CreatePath: " + path);

            return dir;
#else
            return "";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="chunkID"></param>
        /// <param name="_absFilePath"></param>
        /// <returns></returns>
        public static string CreateBinPath(string dir, int chunkID, out string _absFilePath)
        {
#if UNITY_EDITOR
            string dataAsset = string.Format("{0}/occlusion_{1}.bytes", dir, chunkID);

            string assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
            string absFilePath = assetRootPath.Substring(0, assetRootPath.Length - 6) + dataAsset;

            if (File.Exists(absFilePath))
            {
                File.Delete(absFilePath);
            }

            //清理老数据
            {
                string dataPerfab = string.Format("{0}/occlusion_{1}.prefab", dir, chunkID);
                assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
                string absFilePathPerfab = assetRootPath.Substring(0, assetRootPath.Length - 6) + dataPerfab;

                string dataPerfab2 = string.Format("{0}/occlusion_{1}.asset", dir, chunkID);
                assetRootPath = System.IO.Path.GetFullPath(Application.dataPath);
                string absFilePathPerfab2 = assetRootPath.Substring(0, assetRootPath.Length - 6) + dataPerfab2;

                if (File.Exists(absFilePathPerfab))
                {
                    File.Delete(absFilePathPerfab);
                }
                if (File.Exists(absFilePathPerfab2))
                {
                    File.Delete(absFilePathPerfab2);
                }
            }

            dataAsset = dataAsset.ToLower();
            _absFilePath = absFilePath;

            return dataAsset;
#else
            _absFilePath = "";
            return "";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absFilePath"></param>
        /// <param name="saveBytes"></param>
        public static void CreateBin_SaveFile(string absFilePath, RapidList<byte> saveBytes)
        {
            FileStream fs = File.OpenWrite(absFilePath);
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);
            fs.Write(saveBytes.ToArray(), 0, saveBytes.count);
            fs.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exBinData"></param>
        /// <param name="exLoadFileLength"></param>
        /// <param name="chunkID"></param>
        /// <param name="dataAsset"></param>
        /// <param name="saveBytes"></param>
        public static void CreateBin_FillData(Dictionary<int, PVSBakeDataSerialize> exBinData,
            Dictionary<string, int> exLoadFileLength, int chunkID, string dataAsset, RapidList<byte> saveBytes)
        {
            if (exBinData.ContainsKey(chunkID))
            {
                exBinData[chunkID] = new PVSBakeDataSerialize(saveBytes.ToArray(), false, saveBytes.count);
            }
            else
            {
                exBinData.Add(chunkID, new PVSBakeDataSerialize(saveBytes.ToArray(), false, saveBytes.count));
            }

            exLoadFileLength.Add(dataAsset, saveBytes.count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeSamplingPositionsCount"></param>
        /// <returns></returns>
        public static bool IsActivePosNumBeyondLimit(int activeSamplingPositionsCount)
        {
            var isInvalid = activeSamplingPositionsCount >= ushort.MaxValue;
            return isInvalid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_bakeData"></param>
        /// <param name="_isPrint"></param>
        /// <returns></returns>
        public static int CalcUnCompressRawDataSize(PVSVolumeBakeData _bakeData, bool _isPrint)
        {
            var unCompressRawDataSize = 0;

            var rawData = _bakeData.rawData;
            if (rawData == null)
                return 0;

            for (int i = 0; i < rawData.Length; i++)
            {
                var uncompressed = rawData[i].uncompressed;
                if (uncompressed == null)
                    continue;

                var dataSize = uncompressed.Length * sizeof(ushort);
                unCompressRawDataSize += dataSize;
            }

            if (_isPrint)
            {
                Logger.Log($"[PVS][CalcUnCompressRawDataSize] unCompressRawDataSize: {unCompressRawDataSize}");
            }

            return unCompressRawDataSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetActiveScenePath()
        {
#if UNITY_EDITOR
            var curScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string fileName = Path.GetFileNameWithoutExtension(curScene.path);
            string srcFile = Path.GetFullPath(curScene.path);
            srcFile = srcFile.Replace("-stream", "");
            string srcPath = srcFile.Replace(".unity", "");

            string[] strs = srcPath.Split('\\');
            if (strs[strs.Length - 1] != strs[strs.Length - 2])
            {
                strs[strs.Length - 1] = strs[strs.Length - 2];
            }
            string newPath = "";
            bool bCut = false;
            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i] == "Assets")
                {
                    bCut = true;
                }
                if (bCut)
                {
                    newPath += strs[i];
                    newPath += "/";
                }
            }
            return newPath;
#else
            return "";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volume"></param>
        /// <param name="id"></param>
        /// <param name="outCenterPos"></param>
        /// <param name="outBound"></param>
        public static void GetPVSAreaInfoByID(PVSVolume _volume, int id, out Vector3 outCenterPos, out Bounds outBound)
        {
            outCenterPos = Vector3.zero;
            outBound = new Bounds();

            if (_volume == null)
                return;

            var volumePos = _volume.transform.position;
            GetPVSAreaInfoByID(volumePos, id, out outCenterPos, out outBound);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_volumePos"></param>
        /// <param name="id"></param>
        /// <param name="outCenterPos"></param>
        /// <param name="outBound"></param>
        public static void GetPVSAreaInfoByID(Vector3 _volumePos, int id, out Vector3 outCenterPos, out Bounds outBound)
        {
            outCenterPos = Vector3.zero;
            outBound = new Bounds();

            var pvsChunkSize = PVSDefine.s_ChunkSize;
            var pvsMapSize = PVSDefine.s_MapSize;
            int width = pvsMapSize / pvsChunkSize;
            int x = id % width;
            int y = id / width;
            var leftBottomPos = new Vector3(x * pvsChunkSize, 0f, y * pvsChunkSize);
            leftBottomPos -= new Vector3(pvsMapSize * 0.5f, 0f, pvsMapSize * 0.5f);
            outCenterPos = leftBottomPos + new Vector3(pvsChunkSize * 0.5f, 0f, pvsChunkSize * 0.5f);
            outCenterPos.y += _volumePos.y;

            outBound = new Bounds(outCenterPos, new Vector3(pvsChunkSize, pvsChunkSize, pvsChunkSize));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_dataArray"></param>
        /// <param name="_chunkReadOff"></param>
        /// <param name="_rawIdxOff"></param>
        /// <param name="_outChunkIdx"></param>
        /// <param name="_outRawIdx"></param>
        /// <param name="_outRawUintIdx"></param>
        static void DeserializeRead(bool _saveBigVisIndex, byte[] _dataArray, int _chunkReadOff, int _rawIdxOff, 
            out byte _outChunkIdx, out ushort _outRawIdx, out uint _outRawUintIdx)
        {
            _outChunkIdx = byte.MaxValue;
            _outRawIdx = ushort.MaxValue;
            _outRawUintIdx = uint.MaxValue;

            var readerOff = _chunkReadOff;
            var hasData = _dataArray[readerOff];
            readerOff += sizeof(byte);
            if (hasData <= 0)
                return;

            if (_saveBigVisIndex)
            {
                var rawDataUnitSize = sizeof(uint) + sizeof(byte);
                var rawLocalReaderOff = readerOff + rawDataUnitSize * _rawIdxOff;

                _outChunkIdx = _dataArray[rawLocalReaderOff];
                var rawUintIdxOff = rawLocalReaderOff + sizeof(byte);
                _outRawUintIdx = BitConverter.ToUInt32(_dataArray, rawUintIdxOff);
            }
            else
            {
                var rawDataUnitSize = sizeof(ushort) + sizeof(byte);
                var rawLocalReaderOff = readerOff + rawDataUnitSize * _rawIdxOff;

                _outChunkIdx = _dataArray[rawLocalReaderOff];
                var rawIdIdxHigh = _dataArray[rawLocalReaderOff + 1];
                var rawIdIdxLower = _dataArray[rawLocalReaderOff + 2];
                _outRawIdx = GridMath.UnflattenByte2UShort(rawIdIdxHigh, rawIdIdxLower);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_saveBigVisIndex"></param>
        /// <param name="_dataBytes"></param>
        /// <param name="_chunkIndex"></param>
        /// <param name="_idxIndex"></param>
        /// <param name="idxUintIndex"></param>
        /// <returns></returns>
        static int SerializeWrite(bool _saveBigVisIndex, RapidList<byte> _dataBytes, 
            ref ChunkIndex _chunkIndex, ref VisibilityIndex _idxIndex, ref VisibilityUintIndex idxUintIndex)
        {
            var chunkDataArray = _chunkIndex.data;
            var idxDataArray = _idxIndex.data;
            var idxUintDataArray = idxUintIndex.data;
            var hasData = chunkDataArray != null && chunkDataArray.Length > 0 && idxDataArray != null && idxDataArray.Length > 0;
            var hasDataBye = hasData ? PVSDefine.s_OneByte : PVSDefine.s_EmptyByte;
            _dataBytes.Add(hasDataBye);

            if (hasData)
            {
                if (chunkDataArray.Length != idxDataArray.Length)
                {
                    Logger.LogError("[PVS][PVSBakeDataUtils][SerializeWrite] chunkDataArray.Length != idxDataArray.Length");
                    return _dataBytes.count;
                }

                for (int i = 0; i < chunkDataArray.Length; i++)
                {
                    ushort chunkIdx = chunkDataArray[i];
                    ushort rawIdIdx = idxDataArray[i];
                    uint rawIdUintIdx = idxUintDataArray[i];

                    // chunkIdx 项目有效值不会超过 64，用byte存储不会溢出 
                    byte chunkIdxByte = (byte)chunkIdx; 
                    GridMath.FlattenUShort2Byte(rawIdIdx, out byte rawIdIdxHigh, out byte rawIdIdxLower);

                    _dataBytes.Add(chunkIdxByte);

                    if (_saveBigVisIndex)
                    {
                        var rawIdSizeByte = BitConverter.GetBytes(rawIdUintIdx);
                        _dataBytes.AddItems(rawIdSizeByte);
                    }
                    else
                    {
                        _dataBytes.Add(rawIdIdxHigh);
                        _dataBytes.Add(rawIdIdxLower);
                    }
                }
            }

            return _dataBytes.count;
        }
    }
}
