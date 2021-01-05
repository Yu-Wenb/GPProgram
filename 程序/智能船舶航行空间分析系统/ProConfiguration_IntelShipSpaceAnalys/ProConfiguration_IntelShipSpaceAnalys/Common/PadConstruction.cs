using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class PadConstruction
    {
        public static async Task CreateKeyPoints(string maskName,string unionMaskName,string keyPointName ,double factor)
        {
            await QueuedTask.Run(() =>
            {
                StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + ConstDefintion.ConstPath_TimeFilterConfig, Encoding.Default);
                String line;
                //读取状态
                line = sr.ReadLine();
                string filterStatus = line;
                line = sr.ReadLine();
                int filterValue = Convert.ToInt32(line);
                sr.Close();
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(maskName);
                        FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(maskName);
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                        FeatureClass TargetShipObstacleLine = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShipObstacleLine);
                        TargetShipObstacleLine.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
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

                        voyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                        using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Row row = rowCursor.Current)
                                {
                                    Feature ship = row as Feature;
                                    MapPoint p_ship = ship.GetShape() as MapPoint;
                                    double CollisionRisk = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_CollisionRisk]);
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]) * factor * 0.78;
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]) * factor * 0.78;
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]) ;
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]) ;
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                    double ddv = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_ddv]);
                                    double tcr = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tcr]);
                                    double tmin = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tmin]);
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
                                    //根据时间过滤器
                                    if (filterStatus != ConstDefintion.ConstStr_TimeFilterStatusOFF)
                                    {
                                        int time = filterValue * 60;
                                        if (tdv1 > time) continue;
                                        else
                                        {
                                            if (tdv2 > time)
                                            {
                                                tdv2 = time;
                                            }
                                        }
                                    }
                                    //if (CommonMethod.JungeLeft(own_x - ellipseCenter.X, own_y - ellipseCenter.Y, own_cog) && CollisionRisk != 1) continue;
                                    GeodesicEllipseParameter geodesic = new GeodesicEllipseParameter()
                                    {
                                        Center = ellipseCenter,
                                        SemiAxis1Length = asemi,
                                        SemiAxis2Length = bsemi,
                                        LinearUnit = LinearUnit.Meters,
                                        OutGeometryType = GeometryType.Polygon,
                                        AxisDirection = AngularUnit.Degrees.ConvertToRadians(cog),
                                        VertexCount = 800
                                    };
                                    //创建原始位置的椭圆
                                    Geometry ellipse = GeometryEngine.Instance.GeodesicEllipse(geodesic, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                    double moveX = (aoffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) + boffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog));
                                    double moveY = (aoffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) - boffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog));
                                    Geometry moved_ellipse = GeometryEngine.Instance.Move(ellipse, moveX, moveY);
                                    Coordinate2D centerRevise = new Coordinate2D()
                                    {
                                        X = p_ship.X + moveX,
                                        Y = p_ship.Y + moveY
                                    };
                                    //基于TDV创建船舶领域与动界
                                    double moveXs = (tdv1 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    double moveYs = (tdv1 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    Geometry moved_start_ellipse = GeometryEngine.Instance.Move(moved_ellipse, moveXs, moveYs);
                                    Coordinate2D centerTs = new Coordinate2D()
                                    {
                                        X = centerRevise.X + moveXs,
                                        Y = centerRevise.Y + moveYs
                                    };
                                    double moveXe = (tdv2 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    double moveYe = (tdv2 * sog * ConstDefintion.ConstDouble_mpersTOkn * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                    Geometry moved_end_ellipse = GeometryEngine.Instance.Move(moved_ellipse, moveXe, moveYe);
                                    Coordinate2D centerTe = new Coordinate2D()
                                    {
                                        X = centerRevise.X + moveXe,
                                        Y = centerRevise.Y + moveYe
                                    };

                                    //最终图形由两个椭圆和连接椭圆的长方形组成
                                    Geometry e_s_start = GeometryEngine.Instance.SimplifyAsFeature(moved_start_ellipse, false);
                                    Geometry e_s_end = GeometryEngine.Instance.SimplifyAsFeature(moved_end_ellipse, false);
                                    MapPoint p_1 = MapPointBuilder.CreateMapPoint(centerTs.X - (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTs.Y + (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29);
                                    MapPoint p_2 = MapPointBuilder.CreateMapPoint(centerTs.X + (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTs.Y - (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29);
                                    MapPoint p_3 = MapPointBuilder.CreateMapPoint(centerTe.X + (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTe.Y - (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29);
                                    MapPoint p_4 = MapPointBuilder.CreateMapPoint(centerTe.X - (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTe.Y + (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29);
                                    IList<MapPoint> p1_4 = GetInternPoints(p_1, p_4);
                                    IList<MapPoint> p2_3 = GetInternPoints(p_2, p_3);
                                    p2_3 = p2_3.Reverse<MapPoint>().ToList();
                                    List<MapPoint> list2D = new List<MapPoint>();
                                    list2D.Add(p_1);
                                    foreach (MapPoint p in p1_4)
                                    {
                                        list2D.Add(p);
                                    }
                                    list2D.Add(p_4);
                                    list2D.Add(p_3);
                                    foreach (MapPoint p in p2_3)
                                    {
                                        list2D.Add(p);
                                    }
                                    list2D.Add(p_2);
                                    Polygon connect_R = PolygonBuilder.CreatePolygon(list2D, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                    Geometry simple_r = GeometryEngine.Instance.SimplifyAsFeature(connect_R, false);
                                    //融合图形
                                    IList<Geometry> g_List = new List<Geometry>() { e_s_start, simple_r, e_s_end };
                                    Geometry ellInstance = GeometryEngine.Instance.Union(g_List);
                                    using (RowBuffer rowBuffer = voyageMask.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = CollisionRisk;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv1] = tdv1;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv2] = tdv2;
                                        rowBuffer[ConstDefintion.ConstFieldName_asemi] = asemi;
                                        rowBuffer[ConstDefintion.ConstFieldName_bsemi] = bsemi;
                                        rowBuffer[ConstDefintion.ConstFieldName_cog] = cog;
                                        rowBuffer[ConstDefintion.ConstFieldName_sog] = sog;
                                        rowBuffer[ConstDefintion.ConstFieldName_centerX1] = centerTs.X;
                                        rowBuffer[ConstDefintion.ConstFieldName_centerY1] = centerTs.Y;
                                        rowBuffer[ConstDefintion.ConstFieldName_centerX2] = centerTe.X;
                                        rowBuffer[ConstDefintion.ConstFieldName_centerY2] = centerTe.Y;
                                        rowBuffer[voyageMaskDefinition.GetShapeField()] = ellInstance;

                                        using (Feature feature = voyageMask.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                    //创建本船与他船的冲突路径
                                    Coordinate2D ts_location = ellipseCenter;
                                    Coordinate2D ts_Ts = new Coordinate2D()//目标船冲突起点
                                    {
                                        X = ts_location.X + moveXs,
                                        Y = ts_location.Y + moveYs
                                    };
                                    Coordinate2D ts_Te = new Coordinate2D()//目标船冲突终点
                                    {
                                        X = ts_location.X + moveXe,
                                        Y = ts_location.Y + moveYe
                                    };
                                    List<Coordinate2D> ts_obstaclePointList = new List<Coordinate2D>() { ts_Ts,ts_Te };
                                    Polyline ts_obstacleLine = PolylineBuilder.CreatePolyline(ts_obstaclePointList,SpatialReferenceBuilder.CreateSpatialReference(3857));
                                    double kj_risk = 0;
                                    if (ddv > 1) kj_risk = 0;
                                    else if (ddv < 0.5) kj_risk = 1;
                                    else kj_risk = Math.Pow(2 - 2 * ddv, 3.03);
                                    using (RowBuffer rowBuffer = TargetShipObstacleLine.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[ConstDefintion.ConstFieldName_dcr] = kj_risk; 
                                        rowBuffer[ConstDefintion.ConstFieldName_tcr] = tcr;
                                        rowBuffer[ConstDefintion.ConstFieldName_risk] = CollisionRisk;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv1] = tdv1;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv2] = tdv2;
                                        rowBuffer[ConstDefintion.ConstFieldName_tmin] = tmin;
                                        rowBuffer[ConstDefintion.ConstFieldName_Shape] = ts_obstacleLine;

                                        using (Feature feature = TargetShipObstacleLine.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            });
            //创建航行位置Mask

            await QueuedTask.Run(async () =>
            {
                //Mask边缘
                //合并要素
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(unionMaskName))
                        {
                            u_VoyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(maskName))
                            {
                                IList<Geometry> u_list = new List<Geometry>();
                                FeatureClassDefinition u_voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(unionMaskName);
                                using (RowCursor rowCursor = voyageMask.Search(null, false))
                                {
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Feature f = rowCursor.Current as Feature)
                                        {
                                            u_list.Add(f.GetShape());
                                        }
                                    }
                                    //赋值
                                    using (RowBuffer rowBuffer = u_VoyageMask.CreateRowBuffer())
                                    {
                                        Geometry geometry = GeometryEngine.Instance.Union(u_list);
                                        rowBuffer[u_voyageMaskDefinition.GetShapeField()] = geometry;
                                        using (Feature feature = u_VoyageMask.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }

                            }
                        }
                    });
                }
                //运行要素边缘转点
                string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + keyPointName;
                var args = Geoprocessing.MakeValueArray(inpath);
                var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

                string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + unionMaskName;
                string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + keyPointName;
                var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
                var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);


            });
        }

        internal static async Task CreateOwnShipObstacleLine()
        {
            await QueuedTask.Run(() => 
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShipObstacleLine);
                    FeatureClass fc_ownShipObstacleLine = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShipObstacleLine);
                    fc_ownShipObstacleLine.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID > 0" });
                    FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                    MapPoint own_ship;
                    double own_cog;
                    double own_sog;
                    using (RowCursor rowCursor = fc_ownShip.Search(null, false))
                    {
                        rowCursor.MoveNext();
                        using (Feature row = rowCursor.Current as Feature)
                        {
                            own_ship = (row.GetShape() as MapPoint);
                            own_sog = Convert.ToDouble(row[ConstDefintion.ConstFieldName_sog]);
                            own_cog = Convert.ToDouble(row[ConstDefintion.ConstFieldName_cog]);
                        }
                    }
                    Coordinate2D os_start_cdn = new Coordinate2D()
                    {
                        X=own_ship.X,
                        Y=own_ship.Y
                    };
                    double angle = 90 - own_cog;
                    angle = angle / 180 * Math.PI;
                    double xMove = 8 * 1852 * Math.Cos(angle);
                    double yMove = 8 * 1852 * Math.Sin(angle);
                    Coordinate2D os_end_cdn = new Coordinate2D()
                    {
                        X = own_ship.X + xMove,
                        Y = own_ship.Y + yMove
                    };
                    Polyline pl_1 = PolylineBuilder.CreatePolyline(new List<Coordinate2D>() { os_start_cdn, os_end_cdn }, SpatialReferenceBuilder.CreateSpatialReference(3857));
                    gdb.ApplyEdits(() => 
                    {
                        //创建第一条线
                        using (RowBuffer rowBuffer = fc_ownShipObstacleLine.CreateRowBuffer())
                        {
                            // Either the field index or the field name can be used in the indexer.
                            rowBuffer[ConstDefintion.ConstFieldName_dcr] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_tcr] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_risk] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_tdv1] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_tdv2] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_tmin] = 0;
                            rowBuffer[ConstDefintion.ConstFieldName_Shape] = pl_1;

                            using (Feature feature = fc_ownShipObstacleLine.CreateRow(rowBuffer))
                            {
                                feature.Store();
                            }
                        }
                        //创建本船会遇冲突线
                        using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature ts_f = rowCursor.Current as Feature)
                                {
                                    double dcr = (double)ts_f[ConstDefintion.ConstFieldName_dcr];
                                    double tcr = (double)ts_f[ConstDefintion.ConstFieldName_tcr];
                                    double risk = (double)ts_f[ConstDefintion.ConstFieldName_risk];
                                    double tdv1 = (double)ts_f[ConstDefintion.ConstFieldName_tdv1] ;
                                    double tdv2 = (double)ts_f[ConstDefintion.ConstFieldName_tdv2];
                                    double tmin = (double)ts_f[ConstDefintion.ConstFieldName_tmin];
                                    double xTDV1 = Math.Cos(angle) * own_sog * ConstDefintion.ConstDouble_mpersTOkn * tdv1 + own_ship.X;
                                    double yTDV1=  Math.Sin(angle) * own_sog * ConstDefintion.ConstDouble_mpersTOkn * tdv1 + own_ship.Y;
                                    double xTDV2 = Math.Cos(angle) * own_sog * ConstDefintion.ConstDouble_mpersTOkn * tdv2 + own_ship.X;
                                    double yTDV2 = Math.Sin(angle) * own_sog * ConstDefintion.ConstDouble_mpersTOkn * tdv2 + own_ship.Y;
                                    Coordinate2D crd_tdv1 = new Coordinate2D()
                                    {
                                        X = xTDV1,
                                        Y = yTDV1
                                    };
                                    Coordinate2D crd_tdv2 = new Coordinate2D()
                                    {
                                        X = xTDV2,
                                        Y = yTDV2
                                    };
                                    Polyline pl_t = PolylineBuilder.CreatePolyline(new List<Coordinate2D>() { crd_tdv1, crd_tdv2 }, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                    using (RowBuffer rowBuffer = fc_ownShipObstacleLine.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[ConstDefintion.ConstFieldName_dcr] = dcr;
                                        rowBuffer[ConstDefintion.ConstFieldName_tcr] = tcr;
                                        rowBuffer[ConstDefintion.ConstFieldName_risk] = risk;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv1] = tdv1;
                                        rowBuffer[ConstDefintion.ConstFieldName_tdv2] = tdv2;
                                        rowBuffer[ConstDefintion.ConstFieldName_tmin] = tmin;
                                        rowBuffer[ConstDefintion.ConstFieldName_Shape] = pl_t;

                                        using (Feature feature = fc_ownShipObstacleLine.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            });
        }

        private static List<MapPoint> GetInternPoints(MapPoint pS, MapPoint pE)
        {
            List<MapPoint> points = new List<MapPoint>();
            double k = (pE.Y - pS.Y) / (pE.X - pS.X);

            double angle = Math.Atan(k);
            if (pE.X < pS.X)
            {
                angle += Math.PI;
            }
            bool xPlus = true;
            bool yPlus = true;

            if (pE.X < pS.X)
            {
                xPlus = false;
            }
            if (pE.Y < pS.Y)
            {
                yPlus = false;
            }
            int i = 1;
            if (xPlus && yPlus)
            {
                while ((pS.X + 30 * Math.Cos(angle) * i) < pE.X || pS.Y + 30 * Math.Sin(angle) * i < pE.Y)
                {
                    MapPoint p = MapPointBuilder.CreateMapPoint(pS.X + 30 * Math.Cos(angle) * i, pS.Y + 30 * Math.Sin(angle) * i, SpatialReferenceBuilder.CreateSpatialReference(3857));
                    i++;
                    points.Add(p);
                }
            }
            else if (!xPlus && yPlus)
            {
                while ((pS.X + 30 * Math.Cos(angle) * i) > pE.X || pS.Y + 30 * Math.Sin(angle) * i < pE.Y)
                {
                    MapPoint p = MapPointBuilder.CreateMapPoint(pS.X + 30 * Math.Cos(angle) * i, pS.Y + 30 * Math.Sin(angle) * i, SpatialReferenceBuilder.CreateSpatialReference(3857));
                    i++;
                    points.Add(p);
                }
            }
            else if (xPlus && !yPlus)
            {
                while ((pS.X + 30 * Math.Cos(angle) * i) < pE.X || pS.Y + 30 * Math.Sin(angle) * i > pE.Y)
                {
                    MapPoint p = MapPointBuilder.CreateMapPoint(pS.X + 30 * Math.Cos(angle) * i, pS.Y + 30 * Math.Sin(angle) * i, SpatialReferenceBuilder.CreateSpatialReference(3857));
                    i++;
                    points.Add(p);
                }
            }
            else//--
            {
                while ((pS.X + 30 * Math.Cos(angle) * i) > pE.X || pS.Y + 30 * Math.Sin(angle) * i > pE.Y)
                {
                    MapPoint p = MapPointBuilder.CreateMapPoint(pS.X + 30 * Math.Cos(angle) * i, pS.Y + 30 * Math.Sin(angle) * i, SpatialReferenceBuilder.CreateSpatialReference(3857));
                    i++;
                    points.Add(p);
                }
            }
            return points;
        }
        public static async Task KeyPointsIntegration()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb1 = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb1.ApplyEdits(() =>
                    {
                        FeatureClass voyageRiskKeyPoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        FeatureClassDefinition voyageRiskKeyPointDefinition = gdb1.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        voyageRiskKeyPoint.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                        string shapeFieldName = voyageRiskKeyPointDefinition.GetShapeField();

                        RiskAssessment(gdb1, voyageRiskKeyPoint, ConstDefintion.ConstFeatureClass_VoyageMaskInternalPoint,1);
                        RiskAssessment(gdb1, voyageRiskKeyPoint, ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint, 0);
                        double midLocaRisk = Math.Pow(0.5, 3.03);
                        RiskAssessment(gdb1, voyageRiskKeyPoint, ConstDefintion.ConstFeatureClass_VoyageMaskLocaMidPoint, midLocaRisk);
                        RiskAssessment(gdb1, voyageRiskKeyPoint,ConstDefintion.ConstFeatureClass_VoyageMaskRiskMidPoint, 0.5);
                    });
                }
            });
        }
        private static void RiskAssessment(Geodatabase gdb, FeatureClass keyPoints,string maskPoint,double risk)
        {
            FeatureClass voyageMaskPoint = gdb.OpenDataset<FeatureClass>(maskPoint);
            using (RowCursor rowCursor = voyageMaskPoint.Search(null, false))
            {
                while (rowCursor.MoveNext())
                {
                    using (Feature f = rowCursor.Current as Feature)
                    {
                        using (RowBuffer rowBuffer = keyPoints.CreateRowBuffer())
                        {
                            rowBuffer[ConstDefintion.ConstFieldName_ddv] = risk;
                            rowBuffer[ConstDefintion.ConstFieldName_Shape] = f.GetShape();
                            using (Feature feature = keyPoints.CreateRow(rowBuffer))
                            {
                                feature.Store();
                            }
                        }
                    }
                }
            }
        }
    }
}
