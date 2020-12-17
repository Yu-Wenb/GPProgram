using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    /// <summary>
    /// EditOwnShip.xaml 的交互逻辑
    /// </summary>
    public partial class EditOwnShip : Window
    {
        public EditOwnShip()
        {
            InitializeComponent();
            //获取地理数据库里的OwnShip
            GetOwnShip();
            SetParameter();
        }
        public string sog;
        public string cog;
        public string length;
        public string width;
        public string locationX;
        public string locationY;

        private void GetOwnShip()
        {
            var task = QueuedTask.Run(() =>
            {
                using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    FeatureClass ownShip = geodatabase.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                    string shapeField = ownShip.GetDefinition().GetShapeField();
                    QueryFilter qf = new QueryFilter()
                    {
                        ObjectIDs = new List<long>() { 1 }
                    };
                    using (RowCursor rowCursor = ownShip.Search(qf, false))
                    {
                        while (rowCursor.MoveNext())
                        {
                            using (Row row = rowCursor.Current)
                            {
                                sog = row[ConstDefintion.ConstFieldName_sog].ToString();
                                cog = row[ConstDefintion.ConstFieldName_cog].ToString();
                                length = row[ConstDefintion.ConstFieldName_length].ToString();
                                width = row[ConstDefintion.ConstFieldName_width].ToString();
                                ArcGIS.Core.Geometry.Geometry geometry = row[shapeField] as ArcGIS.Core.Geometry.Geometry;
                                MapPoint p = geometry as MapPoint;
                                MapPoint p_project = GeometryEngine.Instance.Project(p, SpatialReferenceBuilder.CreateSpatialReference(4326)) as MapPoint;
                                locationX = p_project.X.ToString("0.0000");
                                locationY = p_project.Y.ToString("0.0000");
                            }
                        }
                    }

                }
            });
            task.Wait();
        }
        public void SetParameter()
        {
            tbx_sog.Text = sog;
            tbx_cog.Text = cog;
            tbx_length.Text = length;
            tbx_width.Text = width;
            tbx_locationX.Text = locationX;
            tbx_locationY.Text = locationY;
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            SaveOwnShip();
            this.DialogResult = true;
        }

        private void SaveOwnShip()
        {
            if (CheckCanSave())
            {
                var task = QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                    {
                        FeatureClass ownShip = geodatabase.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_OwnShip);
                        string shapeField = ownShip.GetDefinition().GetShapeField();
                        QueryFilter qf = new QueryFilter()
                        {
                            ObjectIDs = new List<long>() { 1 }
                        };
                        geodatabase.ApplyEdits(() =>
                        {
                            using (RowCursor rowCursor = ownShip.Search(qf, false))
                            {
                                while (rowCursor.MoveNext())
                                {
                                    using (Row row = rowCursor.Current)
                                    {
                                        row[ConstDefintion.ConstFieldName_sog]=double.Parse(sog);
                                        row[ConstDefintion.ConstFieldName_cog]= double.Parse(cog);
                                        row[ConstDefintion.ConstFieldName_length]= double.Parse(length);
                                        row[ConstDefintion.ConstFieldName_width]= double.Parse(width);
                                        MapPoint p = MapPointBuilder.CreateMapPoint(double.Parse(locationX),double.Parse(locationY),0,SpatialReferenceBuilder.CreateSpatialReference(4326));
                                        MapPoint p_project = GeometryEngine.Instance.Project(p , SpatialReferenceBuilder.CreateSpatialReference(3857)) as MapPoint;
                                        row[shapeField] = p_project as ArcGIS.Core.Geometry.Geometry;
                                        row.Store();
                                    }
                                }
                            }
                        });
                    }
                });
                task.Wait();
            }

        }

        private bool CheckCanSave()
        {
            double result;
            bool b_cog = double.TryParse(tbx_cog.Text, out result);
            bool b_sog = double.TryParse(tbx_sog.Text, out result);
            bool b_length = double.TryParse(tbx_length.Text, out result);
            bool b_width = double.TryParse(tbx_width.Text, out result);
            bool b_locationX = double.TryParse(tbx_locationX.Text, out result);
            bool b_locationY = double.TryParse(tbx_locationY.Text, out result);
            if (b_cog && b_sog && b_length && b_width && b_locationX && b_locationY)
            {
                sog = tbx_sog.Text;
                cog = tbx_cog.Text;
                length = tbx_length.Text;
                width = tbx_width.Text;
                locationX = tbx_locationX.Text;
                locationY = tbx_locationY.Text;
                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("参数不合法");
                return false;
            }
        }
    }
}
