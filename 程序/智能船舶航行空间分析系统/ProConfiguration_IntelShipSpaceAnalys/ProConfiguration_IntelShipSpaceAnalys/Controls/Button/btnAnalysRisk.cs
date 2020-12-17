using ArcGIS.Desktop.Framework.Contracts;
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
            InverseDistanceInterpolation.GenerateIDEKeyPointAsync(ConstDefintion.ConstFeatureClass_StaticObstructPoint);
        }
    }
}
