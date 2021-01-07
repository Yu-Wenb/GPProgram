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
    public class CollisionRisk
    {
        public double a;
        public double x;
        public double y;
        public double asemi { get; set; }
        public double bsemi { get; set; }
        public double Vo { get; set; }
        public double Vt { get; set; }
        public double θo { get; set; }
        public double θt { get; set; }
        public double θr { get; set; }
        public double Vrx{get;set;}
        public double Vry { get; set; }
        public double aoffset { get; set; }
        public double boffset { get; set; }

        public double DDV { get; set; }
        
        public double TDV1 { get; set; }
        public double TDV2 { get; set; }

        public double DCPA { get; set; }
        public double TCPA { get; set; }

        public double Ts { get; set; }
        public double Tj { get; set; }
        public double Tmin { get; set; }
        public double TCR { get; set; }
        public double collisionRisk { get; set; }

        public CollisionRisk(Feature ownShip, Feature targetShip)
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

            double r_cog1 = AngularUnit.Degrees.ConvertToRadians(CommonMethod.GIScoord2ShipCoord(cog1));
            double r_cog2 = AngularUnit.Degrees.ConvertToRadians(CommonMethod.GIScoord2ShipCoord(cog2));

            θo = r_cog1;
            θt = r_cog2;

            double vx1 = Math.Cos(r_cog1) * v1;
            double vy1 = Math.Sin(r_cog1) * v1;
            double vx2 = Math.Cos(r_cog2) * v2;
            double vy2 = Math.Sin(r_cog2) * v2;
            Vrx = vx1 - vx2;
            Vry = vy1 - vy2;
            Vo = v1;
            Vt = v2;
            asemi = ColdwellShipDomain.GetSemiA(targetShip)*2;
            aoffset = ColdwellShipDomain.GetAoffset(targetShip)*2;
            bsemi = ColdwellShipDomain.GetSemiB(targetShip)*2;
            boffset = ColdwellShipDomain.GetBoffset(targetShip)*2;
            //asemi = KijimaShipDomain.GetSemiAB(ownShip, targetShip, "a");
            //bsemi = KijimaShipDomain.GetSemiAB(ownShip, targetShip, "b");
            //aoffset = KijimaShipDomain.GetDertaA(ownShip, targetShip);
            //boffset = 0;

            a = r_cog2;
        }

        private bool Calculate()
        {
            if (CalDdvTdv() && CalDcpaTcpa())
            {
                //计算空间危险度
                double kj_risk = 0;
                if (DDV > 1) kj_risk = 0;
                else if (DDV < 0.5) kj_risk = 1;
                else kj_risk = Math.Pow(2 - 2 * DDV,3.03);
                //计算时间危险度
                double sj_risk = 0;
                double vr = Math.Sqrt(Vrx*Vrx+Vry*Vry);
                if(DDV>0.5) Ts = 0;
                else
                {
                    Ts = Math.Sqrt(0.25  - DDV * DDV)*asemi / vr;
                }
                Tj = Math.Sqrt(36*1852*1852 - DDV * DDV * asemi * asemi)  / vr;
                if (Tmin > 0)
                {
                    if (Tmin > Tj) sj_risk = 0;
                    else if (Tmin < Ts) sj_risk = 1;
                    else
                    {
                        double sj_k = (Tj-Tmin) / (Tj - Ts);
                        sj_risk = Math.Pow(sj_k,3.03);
                    }
                }
                else
                {
                    sj_risk = 0;
                }
                TCR = sj_risk;
                if(kj_risk == 0) { collisionRisk = 0; }
                else
                {
                    if (sj_risk == 0)
                        collisionRisk = 0;
                    else
                    {
                        collisionRisk = Math.Max(sj_risk,kj_risk);
                    }
                }
               
                return true;
            }
            else
                return false;
        }
        private bool CalDcpaTcpa()
        {
            double Vr = Math.Sqrt(Vrx * Vrx + Vry * Vry);
            θr = Math.Atan2(Vry,Vrx);
            double k = Math.Tan(θr);
            DCPA = Math.Abs(y - k * x) / Math.Sqrt(k * k + 1);
            TCPA = Math.Sqrt(x * x + y * y - DCPA * DCPA) / Vr;

            return true;
        }
        private bool CalDdvTdv()
        {
            double A1 = Math.Cos(a) * Math.Cos(a) / asemi / asemi + Math.Sin(a) * Math.Sin(a) / bsemi / bsemi;
            double B1 = 2 * Math.Sin(a) * Math.Cos(a) * ((1 / asemi / asemi) - (1 / bsemi / bsemi));
            double C1 = Math.Cos(a) * Math.Cos(a) / bsemi / bsemi + Math.Sin(a) * Math.Sin(a) / asemi / asemi;
            double h = -(aoffset * Math.Cos(a) + boffset * Math.Sin(a));
            double k = -(aoffset * Math.Sin(a) - boffset * Math.Cos(a));
            double XE = x + h;
            double YE = y + k;

            double A2 = A1 * h * h + B1 * h * k + C1 * k * k - 1;
            double B21 = (2 * A1 * x + B1 * y) * h + (2 * C1 * y + B1 * x) * k;
            double B22 = (2 * A1 * Vrx + B1 * Vry) * h + (2 * C1 * Vry + B1 * Vrx) * k;
            double C21 = A1 * x * x + B1 * x * y + C1 * y * y;
            double C22 = 2 * (A1 * x * Vrx + C1 * y * Vry) + B1 * (x * Vry + y * Vrx);
            double C23 = A1 * Vrx * Vrx + B1 * Vrx * Vry + C1 * Vry * Vry;

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
            if (tmin1 < 0 && tmin2 < 0) //都小于0 表示最小会遇点已经在当前时刻之前，故设其碰撞危险度为0 。一个大于一个小于 说明处于危险中。两个大于表示在将来t时刻到达最小会遇点
            {
                DDV = 0;
            }
            else
            {

                double ft11 = (-(B21 + B22 * tmin1) - Math.Sqrt(D1 * tmin1 * tmin1 + E1 * tmin1 + F1)) / (2 * A2);
                double ft21 = (-(B21 + B22 * tmin1) + Math.Sqrt(D1 * tmin1 * tmin1 + E1 * tmin1 + F1)) / (2 * A2);
                double ft12 = (-(B21 + B22 * tmin2) - Math.Sqrt(D1 * tmin2 * tmin2 + E1 * tmin2 + F1)) / (2 * A2);
                double ft22 = (-(B21 + B22 * tmin2) + Math.Sqrt(D1 * tmin2 * tmin2 + E1 * tmin2 + F1)) / (2 * A2);
                double f = ft11;
                Tmin = tmin1;
                if (ft21 > 0 && ft21 < f)
                {
                    f = ft21;
                    Tmin = tmin1;
                }
                if (ft12 > 0 && ft12 < f)
                {
                    f = ft12;
                    Tmin = tmin2;
                }
                if (ft22 > 0 && ft22 < f)
                {
                    f = ft22;
                    Tmin = tmin2;
                }
                DDV = f;
            }

            //TDV
            double A3 = A1 * Vrx * Vrx + B1 * Vrx * Vry + C1 * Vry * Vry;
            double B3 = 2 * (A1 * XE * Vrx + C1 * YE * Vry) + B1 * (XE * Vry + YE * Vrx);
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
        public static async void GetCollisionRisk()
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
                            CollisionRisk ctd = new CollisionRisk(f_ownShip, f_targetShip);
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
                                        target[ConstDefintion.ConstFieldName_boffset] = ctd.boffset;
                                        target[ConstDefintion.ConstFieldName_ddv] = ctd.DDV;
                                        target[ConstDefintion.ConstFieldName_tdv1] = ctd.TDV1;
                                        target[ConstDefintion.ConstFieldName_tdv2] = ctd.TDV2;
                                        target[ConstDefintion.ConstFieldName_dcpa] = ctd.DCPA;
                                        target[ConstDefintion.ConstFieldName_tcpa] = ctd.TCPA;
                                        target[ConstDefintion.ConstFieldName_tmin] = ctd.Tmin;
                                        target[ConstDefintion.ConstFieldName_tcr] = ctd.TCR;
                                        target[ConstDefintion.ConstFieldName_CollisionRisk] = ctd.collisionRisk;
                                        
                                        target.Store();
                                    });
                                }
                            }
                        }
                    }
                }
                CreateAllDomain(fc_targetShip);
                //CreateVoyageMaskAsync(fc_targetShip);
            });
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

                                double asemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_asemi])/2;
                                double bsemi = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_bsemi])/2;
                                double aoffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_aoffset])/2;
                                double boffset = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_boffset])/2;
                                double DDV = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_CollisionRisk]);
                                double cog = Convert.ToDouble(ship[ConstDefintion.ConstFieldName_cog]);
                                cog = CommonMethod.GIScoord2ShipCoord(cog);
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
                                double moveX = (aoffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)) + boffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)));
                                double moveY = (aoffset * Math.Sin(AngularUnit.Degrees.ConvertToRadians(cog)) - boffset * Math.Cos(AngularUnit.Degrees.ConvertToRadians(cog)));
                                Geometry moved_ellipse = GeometryEngine.Instance.Move(ellipse, moveX, moveY);
                                using (RowBuffer rowBuffer = shipDomain.CreateRowBuffer())
                                {
                                    // Either the field index or the field name can be used in the indexer.
                                    rowBuffer[ConstDefintion.ConstFieldName_asemi] = asemi;
                                    rowBuffer[ConstDefintion.ConstFieldName_bsemi] = bsemi;
                                    rowBuffer[ConstDefintion.ConstFieldName_aoffset] = aoffset;
                                    rowBuffer[ConstDefintion.ConstFieldName_CollisionRisk] = DDV;
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
