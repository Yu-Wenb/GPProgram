using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using System;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class ShipDomain
    {
        public const double Sd = 185;
        public static double GetAD(double L, double V)
        {
            return L * Math.Exp(0.3591 * Math.Log(V) + 0.0952);
        }
        public static double GetDT(double L, double V)
        {
            return L * Math.Exp(0.5441 * Math.Log(V) + 0.0795);
        }
        public static double GetT90(double AD, double DT, double V)
        {
            return 0.67 * Math.Sqrt(AD * AD + ((DT / 2) * (DT / 2) / V));
        }
        public static string GetEncounterSituation(MapPoint mo , MapPoint mt)
        {
            double x = mo.X - mt.X;
            double y = mo.Y - mt.Y;
            double r_angle = Math.Atan2(y,x);
            double angle = AngularUnit.Degrees.ConvertFromRadians(r_angle);
            if (85<=angle && angle<=95)
            {
                return ConstDefintion.ConstStr_EncounterSituationHeadOn;
            }
            else if (202.5 <= angle && angle <= 337.5)
            {
                return ConstDefintion.ConstStr_EncounterSituationCrossing;
            }
            else
            {
                return ConstDefintion.ConstStr_EncounterSituationOvertaking;
            }
        }
        public static double GetCombinedSpeedAngle(double vo, double vt, double cog_o, double cog_t, string type)
        {
            double r_cog_o = AngularUnit.Degrees.ConvertToRadians(cog_o);
            double r_cog_t = AngularUnit.Degrees.ConvertToRadians(cog_t);
            double vx1 = Math.Cos(r_cog_o) * vo;
            double vy1 = Math.Sin(r_cog_o) * vo;
            double vx2 = Math.Cos(r_cog_t) * vt;
            double vy2 = Math.Sin(r_cog_t) * vt;
            double vx_he = vx1 - vx2;
            double vy_he = vy1 - vy2;
            double v_he = Math.Sqrt(vx_he * vx_he + vy_he * vy_he);
            double cog_he = Math.Atan2(vy_he, vx_he);
            double d_cog_he = AngularUnit.Radians.ConvertFromRadians(cog_he);
            double angle = Math.Abs(180 - d_cog_he);
            if (type == ConstDefintion.ConstStr_CalJoinAngle)
            {
                return angle;
            }
            else
            {
                return v_he;
            }
        }
        public static double Gets(MapPoint mo , MapPoint mt, double vo, double vt, double cog_o, double cog_t)
        {
            string situation = GetEncounterSituation(mo,mt);
            if (situation == ConstDefintion.ConstStr_EncounterSituationHeadOn)
            {
                return 2 - ((GetCombinedSpeedAngle(vo, vt, cog_o, cog_t, ConstDefintion.ConstStr_CalJoinSpeed)) / vt);
            }
            else if (situation == ConstDefintion.ConstStr_EncounterSituationCrossing)
            {
                double angle = GetCombinedSpeedAngle(vo, vt, cog_o, cog_t, ConstDefintion.ConstStr_CalJoinAngle);
                return 2 - angle / 180;
            }
            else
            {
                return 1;
            }
        }
        public static double Gett(MapPoint mo, MapPoint mt ,double vo, double vt, double cog_o, double cog_t)
        {
            string situation = GetEncounterSituation(mo,mt);
            if (situation == ConstDefintion.ConstStr_EncounterSituationCrossing)
            {
                double angle = GetCombinedSpeedAngle(vo, vt, cog_o, cog_t, ConstDefintion.ConstStr_CalJoinAngle);
                return angle / 180;
            }
            else
            {
                return 1;
            }
        }
        public static double GetSemiAB(Feature ownShip, Feature targetShip, string type)
        {
            double vo = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;
            double vt = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;

            double cog_o = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_cog]);
            double cog_t = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_cog]);

            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);
            double B = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_width]);


            double AD = GetAD(L, vt);
            double DT = GetDT(L, vt);
            double T90 = GetT90(AD, DT, vt);
            MapPoint m1 = ownShip.GetShape() as MapPoint;
            MapPoint m2 = targetShip.GetShape() as MapPoint;
            double s = Gets(m1,m2, vo, vt, cog_o, cog_t);
            double t = Gett(m1, m2, vo, vt, cog_o, cog_t);
            double a = L + (1 + s / 2) * T90 * vt + Sd;
            double b = B + (1 + t) * DT + Sd;
            if (type == "a")
            {
                return a;
            }
            else
            {
                return b;
            }
        }
        public static double GetDertaA(Feature ownShip, Feature targetShip)
        {
            MapPoint m1 = ownShip.GetShape() as MapPoint;
            MapPoint m2 = targetShip.GetShape() as MapPoint;
            double vo = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;
            double vt = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_sog]) * ConstDefintion.ConstDouble_mpersTOkn;

            double cog_o = Convert.ToDouble(ownShip[ConstDefintion.ConstFieldName_cog]);
            double cog_t = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_cog]);

            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);

            double s = Gets(m1,m2,vo, vt, cog_o, cog_t);

            double AD = GetAD(L, vt);
            double DT = GetDT(L, vt);
            double T90 = GetT90(AD, DT, vt);
            double a = L + (1 + s / 2) * T90 * vt + Sd;
            double a1 = L + (1 + s) * T90 * vt + Sd;
            double a2 = L + T90 * vt + Sd;
            return a - a2;
        }
    }
}
