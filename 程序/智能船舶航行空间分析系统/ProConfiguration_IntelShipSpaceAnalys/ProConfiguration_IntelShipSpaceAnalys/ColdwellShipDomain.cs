using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class ColdwellShipDomain
    {
        internal static double GetSemiA(Feature targetShip)
        {
            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);
            double B = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_width]);
            return 10 * L;
        }

        internal static double GetAoffset(Feature targetShip)
        {
            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);
            double B = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_width]);
            return 2.5 * L;
        }

        internal static double GetSemiB(Feature targetShip)
        {
            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);
            double B = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_width]);
            return 5 * L;
        }

        internal static double GetBoffset(Feature targetShip)
        {
            double L = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_length]);
            double B = Convert.ToDouble(targetShip[ConstDefintion.ConstFieldName_width]);
            return 1.25 * L;                                                         
        }
    }
}
