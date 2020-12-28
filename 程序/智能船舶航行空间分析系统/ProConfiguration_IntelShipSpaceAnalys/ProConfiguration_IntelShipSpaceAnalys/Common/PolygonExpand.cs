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
    public class PolygonExpand
    {
        public async static void CreatePolygonExpantion()
        {
            await QueuedTask.Run(() => 
            {
                using (Geodatabase gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(GeoDataTool.DefaultProject.DefaultGeodatabasePath))))
                {
                    gdb.ApplyEdits(() =>
                    {
                        using (FeatureClass fcStaticObstructPolygon = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_StaticObstructPolygon))
                        {
                            FeatureClassDefinition fcdStaticObstructPoint = gdb.GetDefinition<FeatureClassDefinition>(ConstDefintion.ConstFeatureClass_StaticObstructPolygon);
                            FeatureClass fcSOPBuffer = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPBuffer);
                            FeatureClass fc_SOPIDEPoint = gdb.OpenDataset<FeatureClass>(ConstDefintion.ConstFeatureClass_SOPIDEPoint);
                            fc_SOPIDEPoint.DeleteRows(new QueryFilter() { WhereClause = "OBJECTID >= 1" });
                            using (RowCursor rc = fcStaticObstructPolygon.Search(null, false))
                            {
                                while (rc.MoveNext())
                                {
                                    using (Feature f = rc.Current as Feature)
                                    {
                                        int affectDis = Convert.ToInt32(f[ConstDefintion.ConstFieldName_AffectDis]);
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
            }) ;
        }
    }
}
