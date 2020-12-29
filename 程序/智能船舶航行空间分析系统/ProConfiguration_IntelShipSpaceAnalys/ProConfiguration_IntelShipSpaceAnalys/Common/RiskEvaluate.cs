using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Internal.Core;
using ArcGIS.Core.Data.Raster;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class RiskEvaluate
    {
        internal static async Task CreateEvaluatePointsAsync()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                        FeatureClass fc_evaluatePoints = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskEvaluatePoint);
                        FeatureClassDefinition fcd_evaluatePoints = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskEvaluatePoint);
                        fc_evaluatePoints.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                        double own_x;
                        double own_y;
                        double own_cog;
                        using (RowCursor rowCursor = fc_ownShip.Search(null, false))
                        {
                            rowCursor.MoveNext();
                            using (Feature row = rowCursor.Current as Feature)
                            {
                                own_x = (row.GetShape() as MapPoint).X;
                                own_y = (row.GetShape() as MapPoint).Y;
                                own_cog = Convert.ToDouble(row[ConstDefintion.ConstFieldName_cog]);
                            }
                        }
                        using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Row row = rowCursor.Current)
                                {
                                    Feature ship = row as Feature;
                                    MapPoint p_ship = ship.GetShape() as MapPoint;
                                    double CollisionRisk = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_CollisionRisk]);
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]);
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]);
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]);
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                     
                                    //真北转坐标北
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
                                    if (CommonMethod.JungeLeft(own_x - ellipseCenter.X, own_y - ellipseCenter.Y, own_cog) && CollisionRisk != 1) continue;
                                    //创建原始位置的椭圆
                                    double moveX = (aoffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) + boffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog));
                                    double moveY = (aoffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) - boffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog));
                                    Coordinate2D centerRevise = new Coordinate2D()
                                    {
                                        X = p_ship.X + moveX,
                                        Y = p_ship.Y + moveY
                                    };
                                    //基于TDV创建起点与终点椭圆中心
                                    double moveXs = (tdv1 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    double moveYs = (tdv1 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    Coordinate2D centerTs = new Coordinate2D()
                                    {
                                        X = centerRevise.X + moveXs,
                                        Y = centerRevise.Y + moveYs
                                    };
                                    //长半轴
                                    double angle1 = AngularUnit.Degrees.ConvertToRadians(cog) + Math.PI;
                                    CreateSemiEvaluatePoint(angle1,asemi, centerTs, fc_evaluatePoints);
                                    //短半轴
                                    double angle2 = AngularUnit.Degrees.ConvertToRadians(cog) + Math.PI / 2;
                                    double angle3 = AngularUnit.Degrees.ConvertToRadians(cog) - Math.PI / 2;
                                    CreateSemiEvaluatePoint(angle2, bsemi, centerTs, fc_evaluatePoints);
                                    CreateSemiEvaluatePoint(angle3, bsemi, centerTs, fc_evaluatePoints);
                                    double moveXe = (tdv2 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    double moveYe = (tdv2 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    Coordinate2D centerTe = new Coordinate2D()
                                    {
                                        X = centerRevise.X + moveXe,
                                        Y = centerRevise.Y + moveYe
                                    };
                                    double angle4 = AngularUnit.Degrees.ConvertToRadians(cog);
                                    CreateSemiEvaluatePoint(angle4, asemi, centerTe, fc_evaluatePoints);
                                    //短半轴
                                    CreateSemiEvaluatePoint(angle2, bsemi, centerTe, fc_evaluatePoints);
                                    CreateSemiEvaluatePoint(angle3, bsemi, centerTe, fc_evaluatePoints);
                                }
                            }
                        }
                    });
                }
            });
        }
        private static void CreateSemiEvaluatePoint(double angle,double semi, Coordinate2D point ,FeatureClass fc_evaluatePoints)
        {
            for (int i = 0; i < 21; i++)
            {
                double yStep = Math.Sin(angle) * semi * 0.05;
                double xStep = Math.Cos(angle) * semi * 0.05;
                double risk = CalCollisionRisk(i);
                Coordinate2D evaluatePoint = new Coordinate2D()
                {
                    X = point.X + xStep * i,
                    Y = point.Y + yStep * i
                };
                MapPoint p = MapPointBuilder.CreateMapPoint(evaluatePoint, SpatialReferenceBuilder.CreateSpatialReference(3857));
                using (RowBuffer rowBuffer = fc_evaluatePoints.CreateRowBuffer())
                {
                    // Either the field index or the field name can be used in the indexer.
                    rowBuffer[ConstDefintion.ConstFieldName_r_estimate] = risk;
                    rowBuffer[ConstDefintion.ConstFieldName_factor] = 0.05 * i;
                    rowBuffer[ConstDefintion.ConstFieldName_Shape] = p;
                    using (Feature feature = fc_evaluatePoints.CreateRow(rowBuffer))
                    {
                        feature.Store();
                    }
                }
            }
        }
        private static double CalCollisionRisk(int i)
        {
            double f = i * 0.05;
            if (f < 0.5) return 1;
            else if (f > 1) return 0;
            else return Math.Pow((2-2*f),3.03);
        }
    }
}
