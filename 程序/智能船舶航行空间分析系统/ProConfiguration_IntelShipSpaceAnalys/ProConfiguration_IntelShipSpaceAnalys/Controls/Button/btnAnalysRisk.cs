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
            await CreateArenaAsync();
            await CreateShipDomian();
            await CreateLocaMidAsync();
            await CreateRiskMidAsync();
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb1 = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb1.ApplyEdits(() =>
                    {
                        FeatureClass voyageRiskKeyPoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        FeatureClassDefinition voyageRiskKeyPointDefinition = gdb1.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        voyageRiskKeyPoint.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                        FeatureClass voyageMaskInternalPoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMaskInternalPoint);
                        using (RowCursor rowCursor = voyageMaskInternalPoint.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = rowCursor.Current as Feature)
                                {
                                    using (RowBuffer rowBuffer = voyageRiskKeyPoint.CreateRowBuffer())
                                    {
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = 1;
                                        rowBuffer[voyageRiskKeyPointDefinition.GetShapeField()] = f.GetShape();
                                        using (Feature feature = voyageRiskKeyPoint.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }

                        FeatureClass voyageMaskOutlinePoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint);
                        using (RowCursor rowCursor = voyageMaskOutlinePoint.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = rowCursor.Current as Feature)
                                {
                                    using (RowBuffer rowBuffer = voyageRiskKeyPoint.CreateRowBuffer())
                                    {
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = 0;
                                        rowBuffer[voyageRiskKeyPointDefinition.GetShapeField()] = f.GetShape();
                                        using (Feature feature = voyageRiskKeyPoint.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }

                        FeatureClass voyageMaskLocaMidPoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMaskLocaMidPoint);
                        using (RowCursor rowCursor = voyageMaskLocaMidPoint.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = rowCursor.Current as Feature)
                                {
                                    using (RowBuffer rowBuffer = voyageRiskKeyPoint.CreateRowBuffer())
                                    {
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = Math.Pow(0.5, 3.03);
                                        rowBuffer[voyageRiskKeyPointDefinition.GetShapeField()] = f.GetShape();
                                        using (Feature feature = voyageRiskKeyPoint.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }

                        FeatureClass voyageMaskRiskMidPoint = gdb1.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMaskRiskMidPoint);
                        using (RowCursor rowCursor = voyageMaskRiskMidPoint.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = rowCursor.Current as Feature)
                                {
                                    using (RowBuffer rowBuffer = voyageRiskKeyPoint.CreateRowBuffer())
                                    {
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = 0.5;
                                        rowBuffer[voyageRiskKeyPointDefinition.GetShapeField()] = f.GetShape();
                                        using (Feature feature = voyageRiskKeyPoint.CreateRow(rowBuffer))
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

        private static async Task CreateRiskMidAsync()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskMidRiskMask);
                        FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskMidRiskMask);
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
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
                        double f = 1 - (Math.Pow(0.5,(1/3.03))) / 2;
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
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]) * f*0.78;
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]) * f*0.78;
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]);
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
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
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_UnionVoyageRiskMidMask))
                        {
                            u_VoyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskMidRiskMask))
                            {
                                IList<Geometry> u_list = new List<Geometry>();
                                FeatureClassDefinition u_voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_UnionVoyageRiskMidMask);
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
                string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskRiskMidPoint;
                var args = Geoprocessing.MakeValueArray(inpath);
                var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

                string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_UnionVoyageRiskMidMask;
                string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskRiskMidPoint;
                var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
                var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);


            });
        }

        private static async Task CreateLocaMidAsync()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageLocaMidRiskMask);
                        FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageLocaMidRiskMask);
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
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
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]) *0.75*0.78;
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]) *0.75*0.78;
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]);
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
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
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_UnionVoyageLocationMidMask))
                        {
                            u_VoyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageLocaMidRiskMask))
                            {
                                IList<Geometry> u_list = new List<Geometry>();
                                FeatureClassDefinition u_voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_UnionVoyageLocationMidMask);
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
                string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskLocaMidPoint;
                var args = Geoprocessing.MakeValueArray(inpath);
                var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

                string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_UnionVoyageLocationMidMask;
                string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskLocaMidPoint;
                var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
                var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);


            });
        }

        private static async Task CreateShipDomian()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageShipMask);
                        FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageShipMask);
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
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
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi])/2*0.78;
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi])/2*0.78;
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]);
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
                                    //if (CommonMethod.JungeLeft(own_x - ellipseCenter.X, own_y - ellipseCenter.Y, own_cog) && CollisionRisk!=1) continue;
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
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_UnionVoyageShipMask))
                        {
                            u_VoyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageShipMask))
                            {
                                IList<Geometry> u_list = new List<Geometry>();
                                FeatureClassDefinition u_voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_UnionVoyageShipMask);
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
                string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskInternalPoint;
                var args = Geoprocessing.MakeValueArray(inpath);
                var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

                string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_UnionVoyageShipMask;
                string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskInternalPoint;
                var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
                var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);


            });
        }

        private static async Task CreateArenaAsync()
        {
            await QueuedTask.Run(() =>
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        //置空原船舶领域图层
                        FeatureClass fc_targetShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_TargetShip);
                        FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMask);
                        FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageMask);
                        FeatureClass voyageRiskKeyPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        FeatureClassDefinition voyageRiskKeyPointDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                        //
                        FeatureClass fc_ownShip = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                        double own_x;
                        double own_y;
                        double owm_cog;
                        using (RowCursor rowCursor = fc_ownShip.Search(null, false))
                        {
                            rowCursor.MoveNext();
                            using (Feature row = rowCursor.Current as Feature)
                            {
                                own_x = (row.GetShape() as MapPoint).X;
                                own_y = (row.GetShape() as MapPoint).Y;
                                owm_cog =Convert.ToDouble(row[ConstDefintion.ConstFieldName_cog]);
                            }
                        }
                        //
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
                                    double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi])*0.78;
                                    double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi])*0.78;
                                    double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                    double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset]);
                                    double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                    double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                    double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                    double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                    cog = CommonMethod.GIScoord2ShipCoord(cog);
                                    Coordinate2D ellipseCenter = new Coordinate2D()
                                    {
                                        X = p_ship.X,
                                        Y = p_ship.Y
                                    };
                                    if (!(CollisionRisk > 0)) continue;
                                    //if (CommonMethod.JungeLeft(own_x - ellipseCenter.X, own_y - ellipseCenter.Y, owm_cog) && CollisionRisk != 1) continue;
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
                                    MapPoint p_2 = MapPointBuilder.CreateMapPoint(centerTs.X + (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTs.Y - (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29) ;
                                    MapPoint p_3 = MapPointBuilder.CreateMapPoint(centerTe.X + (bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29, centerTe.Y - (bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog))) * 1.29) ;
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
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_UnionVoyageMask))
                        {
                            u_VoyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMask))
                            {
                                IList<Geometry> u_list = new List<Geometry>();
                                FeatureClassDefinition u_voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_UnionVoyageMask);
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
                string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint;
                var args = Geoprocessing.MakeValueArray(inpath);
                var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

                string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_UnionVoyageMask;
                string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint;
                var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
                var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);


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
            else if(!xPlus && yPlus)
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
