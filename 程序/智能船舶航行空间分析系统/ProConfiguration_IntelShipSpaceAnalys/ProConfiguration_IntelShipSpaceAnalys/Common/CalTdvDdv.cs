using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class CalTdvDdv
    {
        public double a;
        public double x;
        public double y;
        public double asemi { get; set; }
        public double bsemi { get; set; }
        public double vx;
        public double vy;
        public double aoffset { get; set; }
        public double boffset { get; set; }

        public double DDV { get; set; }

        public double TDV1 { get; set; }
        public double TDV2 { get; set; }

        public CalTdvDdv(Feature ownShip, Feature targetShip)
        {
            MapPoint p2 = targetShip.GetShape() as MapPoint;
            double x2 = p2.X;
            double y2 = p2.Y;
            MapPoint p = ownShip.GetShape() as MapPoint;
            double x1 = p.X;
            double y1 = p.Y;

            x = x1 - x2;
            y = y1 - y2;

            double v1 = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;
            double v2 = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;

            double cog1 = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_cog]);
            double cog2 = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_cog]);

            double r_cog1 = AngularUnit.Degrees.ConvertToRadians(cog1);
            double r_cog2 = AngularUnit.Degrees.ConvertToRadians(cog2);
            double vx1 = Math.Cos(r_cog1) * v1;
            double vy1 = Math.Sin(r_cog1) * v1;
            double vx2 = Math.Cos(r_cog2) * v2;
            double vy2 = Math.Sin(r_cog2) * v2;
            vx = vx1 - vx2;
            vy = vy1 - vy2;

            asemi = ShipDomain.GetSemiAB(ownShip, targetShip, "a");
            bsemi = ShipDomain.GetSemiAB(ownShip, targetShip, "b");

            aoffset = ShipDomain.GetDertaA(ownShip, targetShip);
            boffset = 0;

            a = r_cog2;
        }
        private bool Calculate()
        {
            double A1 = Math.Cos(a) * Math.Cos(a) / asemi / asemi + Math.Sin(a) * Math.Sin(a) / bsemi / bsemi;
            double B1 = 2 * Math.Sin(a) * Math.Cos(a) * ((1 / asemi / asemi) - (1 / bsemi / bsemi));
            double C1 = Math.Cos(a) * Math.Cos(a) / bsemi / bsemi + Math.Sin(a) * Math.Sin(a) / asemi / asemi;
            double h = aoffset * Math.Cos(a) + boffset * Math.Sin(a);
            double k = aoffset * Math.Sin(a) - boffset * Math.Cos(a);
            double XE = x - h;
            double YE = y - k;

            double A2 = A1 * h * h + B1 * h * k + C1 * k * k - 1;
            double B21 = (2 * A1 * x + B1 * y) * h + (2 * C1 * y + B1 * x) * k;
            double B22 = (2 * A1 * vx + B1 * vy) * h + (2 * C1 * vy + B1 * vx) * k;
            double C21 = A1 * x * x + B1 * x * y + C1 * y * y;
            double C22 = 2 * (A1 * x * vx + C1 * y * vy) + B1 * (x * vy + y * vx);
            double C23 = A1 * vx * vx + B1 * vx * vy + C1 * vy * vy;

            double D1 = B22 * B22 - 4 * A2 * C23;
            double E1 = 2 * B21 * B22 - 4 * A2 * C22;
            double F1 = B21 * B21 - 4 * A2 * C21;

            double D2 = D1 * (B22 * B22 - D1);
            double E2 = E1 * (B22 * B22 - D1);
            double F2 = F1 * B22 * B22 - E1 * E1 / 4;

            double tmin1 = (-E2 - Math.Sqrt(E2 * E2 - 4 * D2 * F2)) / (2 * D2);
            double tmin2 = (-E2 + Math.Sqrt(E2 * E2 - 4 * D2 * F2)) / (2 * D2);

            //double tmin1 = (-E2 - Math.Sqrt(4 * D2 * F2 -E2 * E2)) / (2 * D2);
            //double tmin2 = (-E2 + Math.Sqrt(4 * D2 * F2 - E2 * E2)) / (2 * D2);
            //DDV
            if(tmin1<0 && tmin2 < 0) //都小于0 表示最小会遇点已经在当前时刻之前，故设其碰撞危险度为0 。一个大于一个小于 说明处于危险中。两个大于表示在将来t时刻到达最小会遇点
            {
                DDV = 0;
            }
            else
            {
                if (tmin1 < tmin2)
                {
                    double ft11 = (-(B21 + B22 * tmin1) - Math.Sqrt(D1 * tmin1 * tmin1 + E1 * tmin1 + F1)) / (2 * A2);
                    double ft21 = (-(B21 + B22 * tmin1) + Math.Sqrt(D1 * tmin1 * tmin1 + E1 * tmin1 + F1)) / (2 * A2);
                    if (ft11 > 0 && ft21 > 0)
                    {
                        double ft = Math.Min(ft11, ft21);
                        DDV = Math.Max(0, 1 - ft);
                    }
                    else
                    {
                        if (ft11 > 0)
                        {
                            DDV = Math.Max(0, 1 - ft11);
                        }
                        if (ft21 > 0)
                        {
                            DDV = Math.Max(0, 1 - ft21);
                        }
                    }
                }
                else
                {
                    double ft12 = (-(B21 + B22 * tmin2) - Math.Sqrt(D1 * tmin2 * tmin2 + E1 * tmin2 + F1)) / (2 * A2);
                    double ft22 = (-(B21 + B22 * tmin2) + Math.Sqrt(D1 * tmin2 * tmin2 + E1 * tmin2 + F1)) / (2 * A2);

                    if (ft12 > 0 && ft22 > 0)
                    {
                        double ft = Math.Min(ft12, ft22);
                        DDV = Math.Max(0, 1 - ft);
                    }
                    else
                    {
                        if (ft12 > 0)
                        {
                            DDV = Math.Max(0, 1 - ft12);
                        }
                        if (ft22 > 0)
                        {
                            DDV = Math.Max(0, 1 - ft22);
                        }
                    }
                }
            }

            //TDV
            double A3 = A1 * vx * vx + B1 * vx * vy + C1 * vy * vy;
            double B3 = 2 * (A1 * XE * vx + C1 * YE * vy) + B1 * (XE * vy + YE * vx);
            double C3 = A1 * XE * XE + B1 * XE * YE + C1 * YE * YE - 1;
            double t1 = (-B3 - Math.Sqrt(B3 * B3 - 4 * A3 * C3)) / (2 * A3);
            double t2 = (-B3 + Math.Sqrt(B3 * B3 - 4 * A3 * C3)) / (2 * A3);
            if (t1 < t2)
            {
                TDV1 = t1;
                TDV2 = t2;
            }
            else
            {
                TDV1 = t2;
                TDV2 = t1;
            }
            return true;
        }
        public static async void GetDDVTDV()
        {
            await CommonMethod.OpenMap(ConstDefintion.ConstMap_EncounterSafety);
            //计算DDV TDV
            await QueuedTask.Run(() =>
            {
                IList<Layer> listLayers = MapView.Active.Map.GetLayersAsFlattenedList().ToList();
                FeatureLayer l_ownShip = listLayers.First(l => l.Name == ConstDefintion.ConstLayer_OwnShipLayerName) as FeatureLayer;
                FeatureLayer l_targetShip = listLayers.First(l => l.Name == ConstDefintion.ConstLayer_TargetShipLayerName) as FeatureLayer;
                FeatureClass fc_ownShip = l_ownShip.GetFeatureClass();
                FeatureClass fc_targetShip = l_targetShip.GetFeatureClass();

                Feature f_ownShip;
                Feature f_targetShip;
                //获取本船的Feature
                QueryFilter qf = new QueryFilter()
                {
                    ObjectIDs = new List<long>() { 1 }
                };
                RowCursor rowCursor1 = fc_ownShip.Search(qf, false);
                
                    rowCursor1.MoveNext();
                Row row1 = rowCursor1.Current;
                    
                        f_ownShip = row1 as Feature;
                    
                
                //遍历目标船的Feature
                using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                {
                    while (rowCursor.MoveNext())
                    {
                        using (Row row = rowCursor.Current)
                        {
                            f_targetShip = row as Feature;
                            long objectid = f_targetShip.GetObjectID();
                            if (objectid == 5)
                            {

                            }
                            CalTdvDdv ctd = new CalTdvDdv(f_ownShip, f_targetShip);
                            if (ctd.Calculate())
                            {
                                using(Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                                {
                                    gdb.ApplyEdits(()=> 
                                    {
                                        Feature target = row as Feature;
                                        target[ConstDefintion.ConstFieldName_asemi] = ctd.asemi;
                                        target[ConstDefintion.ConstFieldName_bsemi] = ctd.bsemi;
                                        target[ConstDefintion.ConstFieldName_aoffset] = ctd.aoffset;
                                        target[ConstDefintion.ConstFieldName_ddv] = ctd.DDV;
                                        target[ConstDefintion.ConstFieldName_tdv1] = ctd.TDV1;
                                        target[ConstDefintion.ConstFieldName_tdv2] = ctd.TDV2;
                                        target.Store();
                                    });
                                }
                            }
                        }
                    }
                }
                CreateAllDomain(fc_targetShip);
                CreateVoyageMaskAsync(fc_targetShip);
            });
        }

        private static async Task CreateVoyageMaskAsync(FeatureClass fc_targetShip)
        {
            //创建航行位置Mask
            using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
            {
                gdb.ApplyEdits(() =>
                {
                    //置空原船舶领域图层
                    FeatureClass voyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMask);
                    FeatureClassDefinition voyageMaskDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageMask);
                    FeatureClass voyageRiskKeyPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                    FeatureClassDefinition voyageRiskKeyPointDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                    voyageRiskKeyPoint.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                    voyageMask.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                    using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                Feature ship = row as Feature;
                                MapPoint p_ship = ship.GetShape() as MapPoint;
                                double ddv = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_ddv]);
                                if (!(ddv > 0)) continue;
                                double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]);
                                double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]);
                                double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                double sog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_sog]);
                                double tdv1 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv1]);
                                double tdv2 = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_tdv2]);
                                Coordinate2D ellipseCenter = new Coordinate2D()
                                {
                                    X = p_ship.X,
                                    Y = p_ship.Y
                                };
                                GeodesicEllipseParameter geodesic = new GeodesicEllipseParameter()
                                {
                                    Center = ellipseCenter,
                                    SemiAxis1Length = asemi * 0.78,
                                    SemiAxis2Length = bsemi * 0.78,
                                    LinearUnit = LinearUnit.Meters,
                                    OutGeometryType = GeometryType.Polygon,
                                    AxisDirection = AngularUnit.Degrees.ConvertToRadians(cog),
                                    VertexCount = 800
                                };
                                //创建原始位置的椭圆
                                Geometry ellipse = GeometryEngine.Instance.GeodesicEllipse(geodesic, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                double moveX = (aoffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                double moveY = (aoffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                Geometry moved_ellipse = GeometryEngine.Instance.Move(ellipse, moveX, moveY);
                                Coordinate2D centerRevise = new Coordinate2D()
                                {
                                    X = p_ship.X + moveX,
                                    Y = p_ship.Y + moveY
                                };
                                //判断是否有空白区域，每100秒建立一个航行领域
                                double voyageDistance = (tdv2 - tdv1) * sog * ConstDefintion.ConstDouble_mpersTOkn;

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
                                MapPoint p_1 = MapPointBuilder.CreateMapPoint(centerTs.X - bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)), centerTs.Y + bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                MapPoint p_2 = MapPointBuilder.CreateMapPoint(centerTs.X + bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)), centerTs.Y - bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                MapPoint p_3 = MapPointBuilder.CreateMapPoint(centerTe.X + bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)), centerTe.Y - bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                MapPoint p_4 = MapPointBuilder.CreateMapPoint(centerTe.X - bsemi * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)), centerTe.Y + bsemi * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                List<MapPoint> list2D = new List<MapPoint>() { p_1 ,p_2,p_3,p_4};
                                Polygon connect_R = PolygonBuilder.CreatePolygon(list2D,SpatialReferenceBuilder.CreateSpatialReference(3857));

                                Geometry simple_r = GeometryEngine.Instance.SimplifyAsFeature(connect_R, false);
                                IList<Geometry> g_List = new List<Geometry>() { e_s_start, simple_r, e_s_end };
                                Geometry ellInstance = GeometryEngine.Instance.Union(g_List);
                                using (RowBuffer rowBuffer = voyageMask.CreateRowBuffer())
                                {
                                    // Either the field index or the field name can be used in the indexer.
                                    rowBuffer[ConstDefintion.ConstFieldName_ddv] = ddv;
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

                                
                                //中心部位
                                int point = (int)voyageDistance / 100;
                                for (int i = 0; i <= point; i++)
                                {
                                    Coordinate2D wayPoint = new Coordinate2D()
                                    {
                                        X = centerTs.X + 100 * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)) * i,
                                        Y = centerTs.Y + 100 * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)) * i
                                    };
                                    using (RowBuffer rowBuffer = voyageRiskKeyPoint.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[ConstDefintion.ConstFieldName_ddv] = ddv;
                                        rowBuffer[voyageRiskKeyPointDefinition.GetShapeField()] = wayPoint.ToMapPoint(SpatialReferenceBuilder.CreateSpatialReference(3857));
                                        using (Feature feature = voyageRiskKeyPoint.CreateRow(rowBuffer))
                                        {
                                            feature.Store();
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }
            await QueuedTask.Run(async () =>
            {
                //Mask边缘
                //合并要素
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(()=> 
                    {
                        using (FeatureClass u_VoyageMask = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_UnionVoyageMask))
                        {
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
            using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
            {
                gdb.ApplyEdits(()=> 
                {
                    FeatureClass voyageRiskKeyPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
                    FeatureClass voyageMaskOutlinePoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_VoyageMaskOutlinePoint);
                    FeatureClassDefinition voyageRiskKeyPointDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_VoyageRiskKeyPoint);
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
                });
            }
        }

        private static void CreateAllDomain(FeatureClass fc_targetShip)
        {
            //创建船周围的船舶领域
            using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
            {
                gdb.ApplyEdits(() =>
                {
                    //置空原船舶领域图层
                    FeatureClass shipDomain = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_ShipDomianEllipse);
                    FeatureClassDefinition shipDomainDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_ShipDomianEllipse);
                    shipDomain.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                    
                    using (RowCursor rowCursor = fc_targetShip.Search(null, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                Feature ship = row as Feature;
                                MapPoint p_ship = ship.GetShape() as MapPoint;

                                double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi]);
                                double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi]);
                                double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset]);
                                double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);

                                Coordinate2D ellipseCenter = new Coordinate2D()
                                {
                                    X = p_ship.X,
                                    Y = p_ship.Y
                                };
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
                                Geometry ellipse = GeometryEngine.Instance.GeodesicEllipse(geodesic, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                double moveX = (aoffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                double moveY = (aoffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                Geometry moved_ellipse = GeometryEngine.Instance.Move(ellipse, moveX, moveY);
                                using (RowBuffer rowBuffer = shipDomain.CreateRowBuffer())
                                {
                                    // Either the field index or the field name can be used in the indexer.
                                    rowBuffer[ConstDefintion.ConstFieldName_asemi] = asemi;
                                    rowBuffer[ConstDefintion.ConstFieldName_bsemi] = bsemi;
                                    rowBuffer[ConstDefintion.ConstFieldName_aoffset] = aoffset;
                                    rowBuffer[shipDomainDefinition.GetShapeField()] = moved_ellipse;
                                    using (Feature feature = shipDomain.CreateRow(rowBuffer))
                                    {
                                        feature.Store();
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }
    }

}
