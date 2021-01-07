using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProConfiguration_IntelShipSpaceAnalys
{
    public class CommonMethod
    {
        public static async Task<Map> OpenMap(string MapName)
        {
            Map map;
            if (FrameworkApplication.Panes.ActivePane != null && FrameworkApplication.Panes.ActivePane.Caption == MapName)
            {
                map = MapView.Active.Map;
            }
            else
            {
                map = await FindOpenExistingMapAsync(MapName);
            }
            return map;
        }
        public static double GIScoord2ShipCoord(double angle)
        {
            return 90 - angle;
        }
        public static async Task<Map> FindOpenExistingMapAsync(string mapName)
        {
            return await QueuedTask.Run(async () =>
            {
                Map map = null;
                Project proj = Project.Current;

                //Finding the first project item with name matches with mapName
                MapProjectItem mpi =
                    proj.GetItems<MapProjectItem>()
                        .FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.CurrentCultureIgnoreCase));
                if (mpi != null)
                {
                    map = mpi.GetMap();
                    //Opening the map in a mapview
                    await ProApp.Panes.CreateMapPaneAsync(map);
                }
                return map;
            });

        }
        public static bool JungeLeft(double rx, double ry, double cog)
        {
            cog = 90 - cog;
            double k = rx / ry;
            double angleT = Math.Atan(k);
            if (rx / k > 0) angleT += Math.PI;
            //angleT = Math.PI / 2 - angleT;//转为真北方位
            //都为坐标系方位
            double angleO = AngularUnit.Radians.ConvertToRadians(cog);
            if (Math.Cos(angleT - angleO) >= 0)
            {
                return false;
            }

            return true;
        }
        public static double GetShipSymbolSize (double min, double max , double angle)
        {
            //以90度和270度为最小点
            double step = (max - min) / 90;
            double size;
            if (angle < 180)
            {
                size =max - Math.Abs(angle) * step;
            }
            else
            {
                size =max - Math.Abs(angle-180) * step;
            }
            size = Math.Round(size,1);
            return size;
        }
        public static Polygon GetShipPolygon()
        {
            MapPoint pt1 = MapPointBuilder.CreateMapPoint(0, 2.0);
            MapPoint pt2 = MapPointBuilder.CreateMapPoint(1.0, 1.0);
            MapPoint pt3 = MapPointBuilder.CreateMapPoint(1.0, -2.0);
            MapPoint pt4 = MapPointBuilder.CreateMapPoint(-1.0, -2.0);
            MapPoint pt5 = MapPointBuilder.CreateMapPoint(-1.0, 1.0);
 
            List<MapPoint> list = new List<MapPoint>() { pt1, pt2, pt3, pt4, pt5 };
            Polygon polygon = PolygonBuilder.CreatePolygon(list);
            return polygon;
        }

        public static Polygon SetShipRotate(double angle)
        {
            Polygon init_p = GetShipPolygon();
            MapPoint center = MapPointBuilder.CreateMapPoint(0,0);
            double radians = AngularUnit.Degrees.ConvertToRadians(angle);
            Polygon p = GeometryEngine.Instance.Rotate(init_p, center, -radians) as Polygon;
            return p;
        }

        public static void SetShipAngle()
        {
            var task = QueuedTask.Run(() =>
            {
                //Create a list of the above two CIMUniqueValueClasses
                List<CIMUniqueValueClass> listUniqueValueClasses = new List<CIMUniqueValueClass>();
                for (int i = 0; i < 360; i++)
                {
                    List<CIMUniqueValue> listUniqueValues = new List<CIMUniqueValue>();
                    CIMUniqueValue cuv = new CIMUniqueValue { FieldValues = new string[] { i.ToString() } };
                    //CIMMarker cm = SymbolFactory.Instance.ConstructMarker(ColorFactory.Instance.BlackRGB, GetShipSymbolSize(5,8,i), SetShipRotate(i));
                    CIMMarker cm = SymbolFactory.Instance.ConstructMarker(ColorFactory.Instance.BlackRGB, GetShipSymbolSize2(10, i), SetShipRotate(i));
                    listUniqueValues.Add(cuv);
                    CIMUniqueValueClass UniqueValueClass = new CIMUniqueValueClass
                    {
                        Editable = true,
                        Patch = PatchShape.Default,
                        Symbol = SymbolFactory.Instance.ConstructPointSymbol(cm).MakeSymbolReference(),
                        Visible = true,
                        Values = listUniqueValues.ToArray()

                    };
                    listUniqueValueClasses.Add(UniqueValueClass);
                }
                
                //Create a list of CIMUniqueValueGroup
                CIMUniqueValueGroup uvg = new CIMUniqueValueGroup
                {
                    Classes = listUniqueValueClasses.ToArray(),
                };
                List<CIMUniqueValueGroup> listUniqueValueGroups = new List<CIMUniqueValueGroup> { uvg };
                CIMMarker cm1 = SymbolFactory.Instance.ConstructMarker(ColorFactory.Instance.BlackRGB, 8, SetShipRotate(0));
                //Create the CIMUniqueValueRenderer
                CIMUniqueValueRenderer uvr = new CIMUniqueValueRenderer
                {
                    UseDefaultSymbol = true,
                    DefaultSymbol = SymbolFactory.Instance.ConstructPointSymbol(cm1).MakeSymbolReference(),
                    Groups = listUniqueValueGroups.ToArray(),
                    Fields = new string[] { ConstDefintion.ConstFieldName_cog}
                };
                //Set the feature layer's renderer.
                FeatureLayer featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().First() as FeatureLayer;
                featureLayer.SetRenderer(uvr);
            });
        }

        private static double GetShipSymbolSize2(int v, int i)
        {
            if (i == 90 || i == 270)
            {
                return v *0.5;
            }
            else if(i == 130 || i == 230)
            {
                return v *0.9;
            }
            else
            {
                return v;
            }
        }

        public static Task<Envelope> GetRasterExtent(string path)
        {
            Task<Envelope> task = QueuedTask.Run(() =>
            {
                string directory = System.IO.Path.GetDirectoryName(path);
                string fileName = System.IO.Path.GetFileName(path);
                FileSystemConnectionPath connectionPath = new FileSystemConnectionPath(new System.Uri(directory), FileSystemDatastoreType.Raster);
                FileSystemDatastore dataStore = new FileSystemDatastore(connectionPath);
                RasterDataset fileRasterDataset = dataStore.OpenDataset<RasterDataset>(fileName);
                Raster raster = fileRasterDataset.CreateFullRaster();
                Envelope env = raster.GetExtent();
                return env;
            });
            return task;
        } 
    }
}
