using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JAFDTC.File.MIZ.DTC
{
    public class MergeHelper
    {
        public void Merge(MergeCriteria criteria)
        {
            if (criteria == null || criteria.SourceFilePaths == null || criteria.SourceFilePaths.Count == 0)
                throw new ArgumentException("Invalid merge criteria or source files.");

            // The first file in the dictionary is the base
            var first = criteria.SourceFilePaths.First();
            var baseContent = JAFDTC.Core.IO.FileHelper.ReadAllText(first.Key);
            var baseJsonNode = JsonNode.Parse(baseContent) as JsonObject;

            foreach (var kvp in criteria.SourceFilePaths.Skip(1))
            {
                var srcContent = JAFDTC.Core.IO.FileHelper.ReadAllText(kvp.Key);
                var srcJsonNode = JsonNode.Parse(srcContent) as JsonObject;
                var sections = kvp.Value;

                if (sections.HasFlag(Sections.COMMS))
                {
                    if (srcJsonNode?["data"] is JsonObject dataObj && dataObj["COMM"] is JsonNode srcComm)
                    {
                        if (baseJsonNode["data"] == null)
                            baseJsonNode["data"] = new JsonObject();
                        baseJsonNode["data"]["COMM"] = srcComm.DeepClone();
                    }
                }
                if (sections.HasFlag(Sections.ELINT))
                {
                    if (srcJsonNode?["data"] is JsonObject dataObj && dataObj["ELINT"] is JsonNode srcElint)
                    {
                        if (baseJsonNode["data"] == null)
                            baseJsonNode["data"] = new JsonObject();
                        baseJsonNode["data"]["ELINT"] = srcElint.DeepClone();
                    }
                }
                if (sections.HasFlag(Sections.STPS))
                {
                    if (srcJsonNode?["data"] is JsonObject dataObj && 
                        dataObj["MPD"] is JsonObject mpdObj && 
                        mpdObj["NAV_PTS"] is JsonNode srcNavPts)
                    {
                        if (baseJsonNode["data"] == null)
                            baseJsonNode["data"] = new JsonObject();
                        if (baseJsonNode["data"]["MPD"] == null)
                            baseJsonNode["data"]["MPD"] = new JsonObject();
                        baseJsonNode["data"]["MPD"]["NAV_PTS"] = srcNavPts.DeepClone();
                    }
                }
                if (sections.HasFlag(Sections.CMDS))
                {
                    if (srcJsonNode?["data"] is JsonObject dataObj && 
                        dataObj["MPD"] is JsonObject mpdObj && 
                        mpdObj["CMDS"] is JsonNode srcCmds)
                    {
                        if (baseJsonNode["data"] == null)
                            baseJsonNode["data"] = new JsonObject();
                        if (baseJsonNode["data"]["MPD"] == null)
                            baseJsonNode["data"]["MPD"] = new JsonObject();
                        baseJsonNode["data"]["MPD"]["CMDS"] = srcCmds.DeepClone();
                    }
                }
            }

            var outputFileName = System.IO.Path.GetFileNameWithoutExtension(criteria.DestinationFilePath);
            ReplaceAllNameAttributes(baseJsonNode, outputFileName);

            var mergedJson = baseJsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(criteria.DestinationFilePath, mergedJson);
        }

        private void ReplaceAllNameAttributes(JsonNode? node, string newName)
        {
            if (node is JsonObject obj)
            {
                foreach (var prop in obj.ToList())
                {
                    if (prop.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        obj["name"] = newName;
                    }
                    else
                    {
                        ReplaceAllNameAttributes(prop.Value, newName);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    ReplaceAllNameAttributes(item, newName);
                }
            }
        }
    }
}
