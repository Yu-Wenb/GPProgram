using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class ConstDefintion
    {
        #region DAML ID
        public const string ConstID_ContentDockPane = "esri_core_contentsDockPane";
        public const string ConstID_SketchLineTool = "esri_editing_SketchLineTool";
        public const string ConstID_EditingTab = "esri_editing_EditingTab";
        public const string ConstID_CreateFeaturesDockPane = "esri_editing_CreateFeaturesDockPane";
        public const string ConstID_DPObstacleExtraction = "ProConfiguration_IntelShipSpaceAnalys_DockPane_DPObstacleExtraction";
        #endregion

        #region 要素类 地图 字段名 gdb等Pro工程要素
        public const string ConstMap_SpaceAnalyzeMap = "SpaceAnalys";
        public const string ConstMap_EncounterSafety = "EncounterSafety";


        public const string ConstLayer_RaderRangeLayerName = "rader";
        public const string ConstLayer_OwnShipLayerName = "本船";
        public const string ConstLayer_TargetShipLayerName = "目标船";

        public const string ConstRaster_RaderRangeDsName = "rader.tif";
        public const string ConstFeatureClass_Course = "course";
        public const string ConstFeatureClass_SelectedCourse = "SelectedCourse";
        public const string ConstFeatureClass_CourseBuffer = "CourseBuffer";
        public const string ConstFeatureClass_CourseObstacle = "CourseObstacle";
        public const string ConstFeatureClass_CourseObstacleThiessen = "CourseObstacleThiessen";
        public const string ConstFeatureClass_CourseObstacleThiessenClip = "CourseObstacleThiessenClip";
        public const string ConstFeatureClass_AisPoint = "AisLocation";
        public const string ConstFeatureClass_OwnShip = "OwnShip";
        public const string ConstFeatureClass_TargetShip = "TargetShip";
        public const string ConstFeatureClass_ShipDomianEllipse = "ShipDomianEllipse";

        public const string ConstFeatureClass_VoyageMask = "VoyageMask";
        public const string ConstFeatureClass_UnionVoyageMask = "UnionVoyageMask";
        public const string ConstFeatureClass_VoyageMaskOutlinePoint = "VoyageMaskOutlinePoint";

        public const string ConstFeatureClass_VoyageShipMask = "VoyageShipMask";
        public const string ConstFeatureClass_UnionVoyageShipMask = "UnionVoyageShipMask";
        public const string ConstFeatureClass_VoyageMaskInternalPoint = "VoyageMaskInternalPoint";

        public const string ConstFeatureClass_VoyageLocaMidRiskMask = "VoyageLocaMidRiskMask";
        public const string ConstFeatureClass_UnionVoyageLocationMidMask = "UnionVoyageLocationMidMask";
        public const string ConstFeatureClass_VoyageMaskLocaMidPoint = "VoyageMaskLocaMidPoint";

        public const string ConstFeatureClass_VoyageRiskMidRiskMask = "VoyageRiskMidRiskMask";
        public const string ConstFeatureClass_UnionVoyageRiskMidMask = "UnionVoyageRiskMidMask";
        public const string ConstFeatureClass_VoyageMaskRiskMidPoint = "VoyageMaskRiskMidPoint";

        public const string ConstFeatureClass_VoyageRiskEvaluatePoint = "VoyageRiskEvaluatePoint";
        public const string ConstFeatureClass_VoyageRiskKeyPoint = "VoyageRiskKeyPoint";
        public const string ConstFeatureClass_SOPBuffer = "SOPBuffer";
        public const string ConstFeatureClass_StaticObstructPoint = "StaticObstructPoint";
        public const string ConstFeatureClass_SOPBufferPoint = "SOPBufferPoint";
        public const string ConstFeatureClass_SOPIDEPoint = "SOPIDEPoint";
        public const string ConstFeatureClass_SOPyBuffer = "SOPyBuffer";
        public const string ConstFeatureClass_StaticObstructPolygon = "StaticObstructPolygon"; 

        public const string ConstFieldName_mmsi = "mmsi";
        public const string ConstFieldName_cog= "cog";
        public const string ConstFieldName_sog = "sog";
        public const string ConstFieldName_heading = "heading";
        public const string ConstFieldName_length = "length";
        public const string ConstFieldName_width = "width";
        public const string ConstFieldName_asemi = "asemi";
        public const string ConstFieldName_bsemi = "bsemi";
        public const string ConstFieldName_aoffset = "aoffset";
        public const string ConstFieldName_boffset = "boffset";
        public const string ConstFieldName_ddv = "ddv";
        public const string ConstFieldName_tdv1 = "tdv1";
        public const string ConstFieldName_tdv2 = "tdv2";
        public const string ConstFieldName_dcpa = "dcpa";
        public const string ConstFieldName_tcpa = "tcpa";
        public const string ConstFieldName_CollisionRisk = "CollisionRisk";
        public const string ConstFieldName_centerX1 = "centerX1";
        public const string ConstFieldName_centerX2 = "centerX2";
        public const string ConstFieldName_centerY1 = "centerY1";
        public const string ConstFieldName_centerY2 = "centerY2";
        public const string ConstFieldName_AffectDis = "AffectDis";
        public const string ConstFieldName_AffectDegree = "AffectDegree";
        public const string ConstFieldName_r_estimate = "r_estimate";
        public const string ConstFieldName_r_real = "r_real";
        public const string ConstFieldName_factor = "factor";
        public const string ConstFieldName_tmin = "tmin";
        public const string ConstFieldName_tcr = "tcr";
        public const string ConstFieldName_Shape = "Shape";
        #endregion

        #region 自定义

        public const string ConstStr_GeoprocessCancling = "取消";
        public const string ConstStr_EncounterSituationHeadOn = "HeadOn";
        public const string ConstStr_EncounterSituationCrossing = "Crossing";
        public const string ConstStr_EncounterSituationOvertaking = "Overtaking";
        public const string ConstStr_CalJoinSpeed = "speed";
        public const string ConstStr_CalJoinAngle = "angle";
        public const string ConstStr_TimeFilterStatusON = "ON";
        public const string ConstStr_TimeFilterStatusOFF = "OFF";
        public const string ConstPath_TimeFilterConfig = @"\timeFilter.txt";
        public const double ConstDouble_mpersTOkn = 0.5144; //1852/3600
        #endregion
    }
}
