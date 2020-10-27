using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Query.RouteNetworkEventHandling
{
    public static class GeoJsonConversionHelper
    {
        public static Point ConvertFromPointGeoJson(string geojson)
        {
            try
            {
                var reader = new NetTopologySuite.IO.GeoJsonReader();
                var point = reader.Read<Point>("{ \"type\": \"Point\", \"coordinates\": " + geojson + "}");
                return point;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error parsing geometry: " + geojson + " Got exception from NetTopologySuite: " + ex.Message, ex);
            }
        }

        public static LineString ConvertFromLineGeoJson(string geojson)
        {
            try
            {
                var reader = new NetTopologySuite.IO.GeoJsonReader();
                var line = reader.Read<LineString>("{ \"type\": \"LineString\", \"coordinates\": " + geojson + "}");
                return line;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error parsing geometry: " + geojson + " Got exception from NetTopologySuite: " + ex.Message, ex);
            }
        }
    }
}
