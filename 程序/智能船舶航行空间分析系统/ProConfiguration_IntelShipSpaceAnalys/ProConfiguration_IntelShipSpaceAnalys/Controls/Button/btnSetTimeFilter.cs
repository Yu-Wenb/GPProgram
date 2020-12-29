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
using System.Windows;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    internal class btnSetTimeFilter : Button
    {
        protected override void OnClick()
        {
            #region 
            TimeFilter timeFilter = new TimeFilter()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (timeFilter.ShowDialog() == true)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("设置成功");
            }
            #endregion
        }
    }
}
