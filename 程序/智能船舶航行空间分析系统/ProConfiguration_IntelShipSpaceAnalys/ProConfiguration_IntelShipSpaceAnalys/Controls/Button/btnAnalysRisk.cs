using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    internal class btnAnalysRisk : Button
    {
        protected override void OnClick()
        {
            //ProcessAsync();
            CreateVoyageMaskAsync();
        }
        private static async Task CreateVoyageMaskAsync()
        {
            double f = 1 - (Math.Pow(0.5, (1 / 3.03))) / 2;
            await PadConstruction.CreateKeyPoints(ConstDefintion.ConstFeatureClass_VoyageRiskMidRiskMask, ConstDefintion.ConstFeatureClass_UnionVoyageRiskMidMask, ConstDefintion.ConstFeatureClass_VoyageMaskRiskMidPoint, f);
            await PadConstruction.CreateKeyPoints(ConstDefintion.ConstFeatureClass_VoyageLocaMidRiskMask, ConstDefintion.ConstFeatureClass_UnionVoyageLocationMidMask, ConstDefintion.ConstFeatureClass_VoyageMaskLocaMidPoint, 0.75);
            await PadConstruction.CreateKeyPoints(ConstDefintion.ConstFeatureClass_VoyageMask, ConstDefintion.ConstFeatureClass_UnionVoyageMask, ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint, 1);
            await PadConstruction.CreateKeyPoints(ConstDefintion.ConstFeatureClass_VoyageShipMask, ConstDefintion.ConstFeatureClass_UnionVoyageShipMask, ConstDefintion.ConstFeatureClass_VoyageMaskInternalPoint, 0.5);
            await PadConstruction.KeyPointsIntegration();
            await PadConstruction.CreateOwnShipObstacleLine();

        }
        private async Task ProcessAsync()
        {
            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                IList<Layer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().ToList();
            FeatureLayer l_ownShip = listLayers.First(l => l.Name == ConstDefintion.ConstLayer_OwnShipLayerName) as FeatureLayer;
            FeatureLayer l_targetShip = listLayers.First(l => l.Name == ConstDefintion.ConstLayer_TargetShipLayerName) as FeatureLayer;
            FeatureClass fc_ownShip = l_ownShip.GetFeatureClass();
            FeatureClass fc_targetShip = l_targetShip.GetFeatureClass();

            Feature f_ownShip;
            //获取本船的Feature
            QueryFilter qf = new QueryFilter()
            {
                ObjectIDs = new List<long>() { 1 }
            };
            RowCursor rowCursor1 = fc_ownShip.Search(qf, false);

            rowCursor1.MoveNext();
            Row row1 = rowCursor1.Current;

            f_ownShip = row1 as Feature;
            MapPoint p_ownship = f_ownShip.GetShape() as MapPoint;
            IList<MapPoint> points = new List<MapPoint>();
            points.Add(CreateTargetShipPoint(p_ownship, -2, 6));
            points.Add(CreateTargetShipPoint(p_ownship, 2, 6));
            points.Add(CreateTargetShipPoint(p_ownship, -2, 2));
            points.Add(CreateTargetShipPoint(p_ownship, 2, 2));
            points.Add(CreateTargetShipPoint(p_ownship, -3, 1));
            points.Add(CreateTargetShipPoint(p_ownship, 3, 1));
            points.Add(CreateTargetShipPoint(p_ownship, -3, 5));
            points.Add(CreateTargetShipPoint(p_ownship, 3, 5));

                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    foreach(MapPoint point in points)
                    {
                        gdb.ApplyEdits(() =>
                        {
                            using (RowBuffer rowBuffer = fc_targetShip.CreateRowBuffer())
                            {
                                rowBuffer["Shape"] = point;
                                using (Feature feature = fc_targetShip.CreateRow(rowBuffer))
                                {
                                    feature.Store();
                                }
                            }

                        });
                    }
                }
            });
        }

        private MapPoint CreateTargetShipPoint(MapPoint own_ship,double movedX , double movedY)
        {
            double lon = own_ship.X;
            double lat = own_ship.Y;
            double m_movedX = movedX * 1852;
            double m_movedY = movedY * 1852;
            MapPoint newTarget = MapPointBuilder.CreateMapPoint(lon+ m_movedX, lat+ m_movedY, own_ship.SpatialReference);
            return newTarget;
        }
    }
}
