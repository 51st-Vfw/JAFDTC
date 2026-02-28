using JAFDTC.Core.Extensions;
using JAFDTC.File.MIZ.Models;
using System.Globalization;
using System.Text;

namespace JAFDTC.File.MIZ
{
    public class SteerpointExporter
    {
        public void Export(SteerpointCriteria steerpointCriteria)
        {
            steerpointCriteria.Required();
            steerpointCriteria.PathOutput.Required();
            steerpointCriteria.Mission.Required();

            //restict to what we currently support
            if (steerpointCriteria.Mission.Packages.Count != 1)
                throw new NotSupportedException("Currently only missions with a single Package are supported.");
            if (steerpointCriteria.Mission.Packages[0].Flights.Count != 1)
                throw new NotSupportedException("Currently only missions with a single Flight are supported.");

            var theater = new CultureInfo("en-US", false).TextInfo.ToTitleCase(steerpointCriteria.Mission.Theater);
            var filePath = Path.Combine(steerpointCriteria.PathOutput, theater + ".lua");
            
            //var existingData = JAFDTC.Core.IO.FileHelper.ReadAllText(filePath); //todo: future appending/replacing...

            //only support the single flight for now..
            var flight = steerpointCriteria.Mission.Packages[0].Flights[0];
            var fltName = (string.IsNullOrEmpty(flight.Name)) ? "Flight" : flight.Name;
            var msnName = (string.IsNullOrEmpty(steerpointCriteria.Mission.Name)) ? "Mission" : steerpointCriteria.Mission.Name;
            var presetName = $"{steerpointCriteria.Name}-{msnName}-{fltName}".Replace("\"", "'");

            /*
             * 
             * presets = 
                {
	                ["NTTR-Test"] = 
	                {
		            [1] = 
		            {
			            ["alt"] = 562.0512,
			            ["type"] = "Turning Point",
			            ["ETA"] = 900,
			            ["ETA_locked"] = true,
			            ["y"] = -17231.142578125,
			            ["x"] = -398179.59375,
			            ["name"] = "Nellis",
			            ["action"] = "Turning Point",
			            ["alt_type"] = "BARO",
			            ["speed_locked"] = false,
		            }, -- end of [1]

             * 
             */

            var output = new StringBuilder();
            output.AppendLine("presets =");
            output.AppendLine("{");
            output.AppendLine($"[\"{presetName}\"] = "); 
            output.AppendLine("{");

            for (var i = 0; i < flight.Routes[0].NavPoints.Count; i++) //todo: support multiple routes and flights in the future, but for now just export the first route of the first flight
            {
                var stp = flight.Routes[0].NavPoints[i];

                //var coords = CoordInterpolator.Instance.LLtoXZ(theater, double.Parse(stp.Location.Latitude), double.Parse(stp.Location.Longitude));

                output.AppendLine($"[{i + 1}] = ");
                output.AppendLine("{");

                output.AppendLine($"[\"alt\"] = {stp.Location.Altitude},");
                output.AppendLine($"[\"type\"] = \"Turning Point\",");
                
                //output.AppendLine($"[\"ETA\"] = {stp.TOT},");
                output.AppendLine($"[\"ETA\"] = 0,"); //fake it directly for now...

                output.AppendLine($"[\"ETA_locked\"] = false,");
                
                //output.AppendLine($"[\"y\"] = {coords.Z},");
                //output.AppendLine($"[\"x\"] = {coords.X},");
                output.AppendLine($"[\"y\"] = {stp.Location.Longitude},"); //fake it directly for now...
                output.AppendLine($"[\"x\"] = {stp.Location.Latitude},"); //fake it directly for now...

                output.AppendLine($"[\"name\"] = \"{((stp.Name ?? "").Replace("\"", "").Replace(",", ""))}\",");
                output.AppendLine($"[\"action\"] = \"Turning Point\",");
                output.AppendLine($"[\"alt_type\"] = \"BARO\","); //RADIO
                output.AppendLine($"[\"speed_locked\"] = false,");

                output.AppendLine("},"); //ok to have hanging , on last item in lua table
            }

            output.AppendLine("}");
            output.AppendLine("}");

            JAFDTC.Core.IO.FileHelper.WriteAllText(output.ToString(), filePath);
        }
    }
}
