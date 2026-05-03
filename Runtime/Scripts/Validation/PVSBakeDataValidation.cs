using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ST.Core;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 
    /// </summary>
    public class PVSBakeDataValidation
    {
        /// <summary>
        /// 
        /// </summary>
        static PVSBakeDataValidation s_Instance;

        /// <summary>
        /// 
        /// </summary>
        public static PVSBakeDataValidation S
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PVSBakeDataValidation();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int unCompressRawDataSize;
        public int compressRawDataSize;
        public Dictionary<string, int> compressRawDataSizeDict = new Dictionary<string, int>();

        /// <summary>
        /// 
        /// </summary>
        public HashSet<ushort> rawShareDataSet = new HashSet<ushort>();

        /// <summary>
        /// 
        /// </summary>
        public List<PVSWorldPosInfo> totalSamplePosList = new List<PVSWorldPosInfo>();
        public List<int> activeSampleIDList = new List<int>();
        public List<PVSWorldPosInfo> invalidSamplePosList = new List<PVSWorldPosInfo>();

        /// <summary>
        /// 
        /// </summary>
        public int forceSampleIndexNum;
        public int defaultSampleOffsetTypeNum;
        public int areaDefaultSampleOffsetTypeNum;
        public int camDirSampleOffsetTypeNum;

        /// <summary>
        /// 
        /// </summary>
        public void Validation()
        {
            PVSVolume volume = null;
            GameObject CullingVolume = GameObject.Find(PVSDefine.s_VolumeName);
            if (CullingVolume != null)
            {
                volume = CullingVolume.GetComponent<PVSVolume>();
            }

            if (volume == null)
                return;

            Validation(volume);
        }

        /// <summary>
        /// 
        /// </summary>
        void Validation(PVSVolume _volume)
        {
            var _bakeData = _volume.volumeBakeData;
            if (_bakeData == null)
                return;

            CalcRawDataSize(_bakeData);
            CalcRawDataShareData(_bakeData);
            CalcActiveSampleIDList(_bakeData);
            CalcForceSampleIdxNum(_bakeData);
            CalcSamplePosList(_bakeData, _volume);
            SaveIniFile(_volume, _bakeData);
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcRawDataShareData(PVSVolumeBakeData _bakeData)
        {
            rawShareDataSet.Clear();

            var rawDataArray = _bakeData.rawData;
            if (rawDataArray == null || rawDataArray.Length == 0)
            {
                Logger.LogError("[PVS][CalcRawDataShareData] rawDataArray is null or empty.");
                return;
            }

            var tempRawDataList = PVSCompressRawDataUtils.GetValidRawDataList(rawDataArray);
            rawShareDataSet = PVSCompressRawDataUtils.FindCommonNumbers(tempRawDataList.ToArray());

            Logger.Log($"[PVS][CalcRawDataShareData] rawShareDataSet.Count: " +
                $"{rawShareDataSet.Count} / rawDataArray.Length: {tempRawDataList.Count}");
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcForceSampleIdxNum(PVSVolumeBakeData _bakeData)
        {
            forceSampleIndexNum = 0;

            var samplePosList = _bakeData.allSamplePosInfoList;
            for (int i = 0; i < samplePosList.Count; i++)
            {
                var posInfo = samplePosList[i];
                var isForce = posInfo.isForceSamplePos;

                if (isForce)
                {
                    forceSampleIndexNum++;
                }
            }

            Logger.Log($"[PVS][CalcForceSampleIdxNum] forceSampleIndexNum : {forceSampleIndexNum} / " +
                $"{samplePosList.Count}");
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcActiveSampleIDList(PVSVolumeBakeData _bakeData)
        {
            activeSampleIDList.Clear();

            var rawDataArray = _bakeData.rawData;
            if (rawDataArray == null || rawDataArray.Length == 0)
            {
                Logger.LogError("[PVS][CalcActiveSampleIDList] rawDataArray is null or empty.");
                return;
            }

            for (int i = 0; i < rawDataArray.Length; i++)
            {
                var rawData = rawDataArray[i];
                var isHasData = rawData.HasData();
                if (!isHasData)
                    continue;

                activeSampleIDList.Add(i);
            }

            Logger.Log($"[PVS][CalcActiveSampleIDList] activeSampleIDList.Count: " +
                $"{activeSampleIDList.Count}");
        }

        /// <summary>
        /// 
        /// </summary>
        void SaveIniFile(PVSVolume _volume, PVSVolumeBakeData _bakeData)
        {
            if (_bakeData == null)
                return;

            var strSceneDir = PVSBakeDataUtils.GetActiveScenePath();
            var filePath = strSceneDir + "BakeDataValidation.ini";

            System.Text.StringBuilder content = new System.Text.StringBuilder();
            content.Append($"IsBakeSuccess = {invalidSamplePosList.Count <= 0}\n");
            content.Append($"ActiveSamplePosNum = {activeSampleIDList.Count}\n");
            content.Append($"forceSampleIndexNum = {forceSampleIndexNum}\n");
            content.Append($"defaultSampleOffsetTypeNum = {defaultSampleOffsetTypeNum}\n");
            content.Append($"areaDefaultSampleOffsetTypeNum = {areaDefaultSampleOffsetTypeNum}\n");
            content.Append($"camDirSampleOffsetTypeNum = {camDirSampleOffsetTypeNum}\n");

            PrintCompressRawDataSizeInfo(content, _bakeData);
            PrintChunkSamplePointNumDict(content, _volume, _bakeData);

            File.WriteAllText(filePath, content.ToString(), System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 
        /// </summary>
        void PrintChunkSamplePointNumDict(System.Text.StringBuilder content, PVSVolume _volume, PVSVolumeBakeData _bakeData)
        {
            var cellInfoList = _bakeData.cellInfoList;
            if (cellInfoList.Count <= 0)
                return;

            content.Append("\n[ChunkPointNum]\n");

            foreach(var cellInfo in cellInfoList)
            {
                if (cellInfo == null)
                    continue;

                var chunkIdx = cellInfo.cellID;
                var pointNum = cellInfo.pointNum;
                var bounds = cellInfo.bounds;

                content.Append($"ChunkIdx: {chunkIdx}, " +
                               $"PointNum: {pointNum}, " +
                               $"Center: {bounds.center} " +
                               $"\n");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void PrintCompressRawDataSizeInfo(System.Text.StringBuilder content, PVSVolumeBakeData _bakeData)
        {
            float precent = compressRawDataSize / (float)unCompressRawDataSize;
            content.Append($"unCompressRawDataSize = {unCompressRawDataSize}\n");
            content.Append($"compressRawDataSize = {compressRawDataSize}\n");
            content.Append($"CompressPercent = {precent}\n");
        }

        /// <summary>
        /// 
        /// </summary>
        void ClearSampleOffsetMaskList(PVSVolume _cullingVolume)
        {
            _cullingVolume.CreateCommonBakeDataAsset();

            var commonBakeData = _cullingVolume.commonBakeData;
            if (commonBakeData == null)
                return;

            commonBakeData.ClearSampleOffsetMaskList();
        }

        /// <summary>
        /// 
        /// </summary>
        void AddSampleOffsetMaskList(PVSVolume _cullingVolume, Vector3 _samplePos, Vector3 _cellSize, uint _samplePosOffsetMask)
        {
            var isValid = PVSCameraUtils.IsSamplePosOffsetMaskValid(_samplePosOffsetMask);
            if (!isValid)
                return;

            _cullingVolume.CreateCommonBakeDataAsset();

            var commonBakeData = _cullingVolume.commonBakeData;
            if (commonBakeData == null)
                return;

            commonBakeData.AddSampleOffsetMaskList(_samplePos, _cellSize, _samplePosOffsetMask);
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcSamplePosList(PVSVolumeBakeData _bakeData, PVSVolume _volume)
        {
            totalSamplePosList.Clear();
            invalidSamplePosList.Clear();
            defaultSampleOffsetTypeNum = areaDefaultSampleOffsetTypeNum = camDirSampleOffsetTypeNum = 0;
            ClearSampleOffsetMaskList(_volume);

            var samplePosList = _bakeData.allSamplePosInfoList;
            var rawDataArray = _bakeData.rawData;
            if (rawDataArray == null || samplePosList.Count != rawDataArray.Length)
            {
                Logger.LogError("[PVS][CalcInvalidSamplePosList] samplePosList.Count != rawDataArray.Length");
                return;
            }

            totalSamplePosList.AddRange(samplePosList);
            Logger.Log($"[PVS][CalcInvalidSamplePosList] samplePosList.Count: " +
                $"{totalSamplePosList.Count}");

            for (int i = 0; i < samplePosList.Count; i++)
            {
                var posInfo = samplePosList[i];
                var samplePos = posInfo.pos;
                var rawData = rawDataArray[i];
                var leafNodeIdx = posInfo.leafNodeIdx;
                var isInDefaultSampleOffsetArea = posInfo.isInDefaultSampleOffsetArea;
                var cellSize = _bakeData.GetCellSize(leafNodeIdx);

                PVSTemp.rapidListUshort.Clear();
                _volume.GetIndicesForWorldPos(samplePos, PVSTemp.rapidListUshort, true, 
                    out _, out _, out uint _samplePosOffsetMask);
                
                var hasData = PVSTemp.rapidListUshort.count > 0;
                var isValid = IsSamplePosValid(rawData, PVSTemp.rapidListUshort);
                if (!isValid)
                {
                    invalidSamplePosList.Add(posInfo);
                }
                else
                {
                    CalcSampleOffsetTypeNum(hasData, isInDefaultSampleOffsetArea, _samplePosOffsetMask);
                    AddSampleOffsetMaskList(_volume, samplePos, cellSize, _samplePosOffsetMask);
                }
            }

            if (invalidSamplePosList.Count > 0)
            {
                Logger.LogError($"[PVS][CalcInvalidSamplePosList] Invalid sample pos Num: " +
                    $"{invalidSamplePosList.Count} / Sample pos Num : {totalSamplePosList.Count}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcSampleOffsetTypeNum(bool _hasData, bool _isInDefaultSampleOffsetArea, uint _samplePosOffsetMask)
        {
            if (!_hasData)
                return;

            var isType = PVSCameraUtils.IsCamSampleOffsetDefaultType(_samplePosOffsetMask);
            if (isType)
            {
                defaultSampleOffsetTypeNum++;

                if (_isInDefaultSampleOffsetArea)
                {
                    areaDefaultSampleOffsetTypeNum++;
                }
            }
            else
            {
                camDirSampleOffsetTypeNum++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        bool IsSamplePosValid(RawData _rawData, RapidList<ushort> _rapidListUshort)
        {
            ushort[] uncompressPosArray = _rawData.uncompressed;
            if (uncompressPosArray == null || _rapidListUshort == null)
                return false;

            if (uncompressPosArray.Length != _rapidListUshort.count)
                return false;

            List<ushort> uncompressPosList = new List<ushort>();
            uncompressPosList.AddRange(uncompressPosArray);
            uncompressPosList.Sort();

            List<ushort> rapidTempList = new List<ushort>();
            for (int i = 0; i < _rapidListUshort.count; i++)
            {
                rapidTempList.Add(_rapidListUshort.buffer[i]);
            }
            rapidTempList.Sort();

            var isValid = IsSamplePosValid(uncompressPosList, rapidTempList);
            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        bool IsSamplePosValid(List<ushort> _uncompressPosList, List<ushort> _rapidTempList)
        {
            bool isValid = true;

            int posNum = _rapidTempList.Count;
            for (int i = 0; i < posNum; i++)
            {
                var samplePos = _rapidTempList[i];

                var uncompressPos = _uncompressPosList[i];
                if (uncompressPos != samplePos)
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcRawDataSize(PVSVolumeBakeData _bakeData)
        {
            unCompressRawDataSize = PVSBakeDataUtils.CalcUnCompressRawDataSize(_bakeData, false);
            CalcCompressRawDataSize(_bakeData);

            float precent = compressRawDataSize / (float)unCompressRawDataSize;
            Logger.Log($"[PVS][CalcRawDataSize] " +
                $"unCompressRawDataSize: {unCompressRawDataSize}, compressRawDataSize: {compressRawDataSize} " +
                $"CompressPercent {precent}" );
        }

        /// <summary>
        /// 
        /// </summary>
        void CalcCompressRawDataSize(PVSVolumeBakeData _bakeData)
        {
            compressRawDataSizeDict.Clear();
            foreach(var iter in _bakeData.exLoadFileLength)
            {
                compressRawDataSizeDict.Add(iter.Key, iter.Value);
            }

            compressRawDataSize = 0;
            foreach (var iter in compressRawDataSizeDict)
            {
                compressRawDataSize += iter.Value;
            }
        }
    }
}
