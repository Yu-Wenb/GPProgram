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
   public class InverseDistanceInterpolation
    {
        public static async Task GenerateIDEKeyPointAsync(string fcName)
        {
            //1.
           await QueuedTask.Run(()=> 
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(()=> 
                    {
                        using (FeatureClass fcStaticObstructPoint = gdb.OpenDataset<FeatureClass>(fcName))
                        {
                            FeatureClassDefinition fcdStaticObstructPoint = gdb.GetDefinition<FeatureClassDefinition>(fcName);
                            FeatureClass fcSOPBuffer = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPBuffer);
                            FeatureClass fc_SOPIDEPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPIDEPoint);
                            fc_SOPIDEPoint.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (RowCursor rc = fcStaticObstructPoint.Search(null, false))
                            {
                                while (rc.MoveNext())
                                {
                                    using (Feature f = rc.Current as Feature)
                                    {
                                        int affectDis =Convert.ToInt32(f[ConstDefintion.ConstFieldName_AffectDis]);
                                        double affectDegree = (double)f[ConstDefintion.ConstFieldName_AffectDegree];
                                        MapPoint p = f[fcdStaticObstructPoint.GetShapeField()] as MapPoint;
                                        GeodesicEllipseParameter geodesic = new GeodesicEllipseParameter()
                                        {
                                            Center = p.Coordinate2D,
                                            SemiAxis1Length = affectDis,
                                            SemiAxis2Length = affectDis,
                                            LinearUnit = LinearUnit.Meters,
                                            OutGeometryType = GeometryType.Polygon,
                                            AxisDirection = 0,
                                            VertexCount = 800
                                        };
                                        Geometry circle = GeometryEngine.Instance.GeodesicEllipse(geodesic, SpatialReferenceBuilder.CreateSpatialReference(3857));
                                        using (RowBuffer rowBuffer = fcSOPBuffer.CreateRowBuffer())
                                        {
                                            // Either the field index or the field name can be used in the indexer.
                                            rowBuffer[ConstDefintion.ConstFieldName_AffectDegree] = 0;
                                            rowBuffer["Shape"] = circle;
                                            using (Feature feature = fcSOPBuffer.CreateRow(rowBuffer))
                                            {
                                                feature.Store();
                                            }
                                        }
                                        using (RowBuffer rowBuffer = fc_SOPIDEPoint.CreateRowBuffer())
                                        {
                                            // Either the field index or the field name can be used in the indexer.
                                            rowBuffer[ConstDefintion.ConstFieldName_AffectDegree] = affectDegree;
                                            rowBuffer["Shape"] = p;
                                            using (Feature feature = fc_SOPIDEPoint.CreateRow(rowBuffer))
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

            });
            //2.运行要素边缘转点
            string inpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_SOPBufferPoint;
            var args = Geoprocessing.MakeValueArray(inpath);
            var result = await Geoprocessing.ExecuteToolAsync("Delete_management", args, null, null, null);

            string inpath1 = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_SOPBuffer;
            string outpath = GeoDataTool.DefaultProject.DefaultGeodatabasePath + "\\" + ConstDefintion.ConstFeatureClass_SOPBufferPoint;
            var args1 = Geoprocessing.MakeValueArray(inpath1, outpath, "ALL");
            var result1 = await Geoprocessing.ExecuteToolAsync("FeatureVerticesToPoints_management", args1, null, null, null);
            //3.
            await QueuedTask.Run(()=> 
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        FeatureClass SOPIDEPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPIDEPoint);
                        FeatureClass SOPBufferPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPBufferPoint);
                        FeatureClassDefinition SOPIDEPointDefinition = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_SOPIDEPoint);
                        using (RowCursor rowCursor = SOPBufferPoint.Search(null, false))
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = rowCursor.Current as Feature)
                                {
                                    using (RowBuffer rowBuffer = SOPIDEPoint.CreateRowBuffer())
                                    {
                                        rowBuffer[ConstDefintion.ConstFieldName_AffectDegree] = 0;
                                        rowBuffer[SOPIDEPointDefinition.GetShapeField()] = f.GetShape();
                                        using (Feature feature = SOPIDEPoint.CreateRow(rowBuffer))
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
    }
}
