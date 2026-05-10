// ********************************************************************************************************************
//
// DTCMergeHelperTest.cs -- Unit tests for MergeHelper
//
// Copyright(C) 2025 rage
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using JAFDTC.File.MIZ.DTC;

namespace JAFDTC.Tests
{
    [TestClass]
    public sealed class DTCMergeHelperTest
    {

        [TestInitialize]
        public void Setup()
        {

        }

        [TestCleanup]
        public void Cleanup()
        {

        }

        //[TestMethod]
        //public void Read_And_Write()
        //{
        //    // Arrange
        //    var appData = "..\\..\\..\\appdata";
        //    var dtcFolder = Path.Combine(appData, "dtc");
        //    var outputFolder = Path.Combine(dtcFolder, "output");

        //    var srcFile = dtcFolder + "\\source.dtc";


        //    var baseContent = JAFDTC.Core.IO.FileHelper.ReadAllText(srcFile);
        //    var baseJsonNode = JsonNode.Parse(baseContent) as JsonObject;

        //    var mergedJson = baseJsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        //    var c2 = JsonNode.Parse(mergedJson) as JsonObject;

        //    Assert.IsTrue(baseContent == mergedJson);

        //}

        [TestMethod]
        public void Merge_Source_And_STPS()
        {
            // Arrange
            var appData = "..\\..\\..\\appdata";
            var dtcFolder = Path.Combine(appData, "dtc");
            var outputFolder = Path.Combine(dtcFolder, "output");

            var srcFile = dtcFolder + "\\source.dtc";
            var sectionFile = dtcFolder + "\\stps.dtc";


            var sourceFilePaths = new Dictionary<string, Sections>();
            sourceFilePaths.Add(srcFile, Sections.None);
            sourceFilePaths.Add(sectionFile, Sections.STPS);

            var outputFile = Path.Combine(outputFolder, $"Merge_Source_And_STP-{Guid.NewGuid()}.dtc");

            var criteria = new MergeCriteria
            {
                SourceFilePaths = sourceFilePaths,
                DestinationFilePath = outputFile
            };

            var helper = new MergeHelper();
            helper.Merge(criteria);

            Assert.IsTrue(System.IO.File.Exists(outputFile), $"Output file was not created: {outputFile}");

            var srcdata = System.IO.File.ReadAllText(srcFile);
            var sectiondata = System.IO.File.ReadAllText(sectionFile);
            var outputdata = System.IO.File.ReadAllText(outputFile);

            Assert.IsTrue(srcdata != sectiondata);
            Assert.IsTrue(srcdata != outputdata);
            Assert.IsTrue(sectiondata != outputdata);

            //System.IO.File.Delete(outputFile);
        }

        [TestMethod]
        public void Merge_Source_And_CMDS()
        {
            // Arrange
            var appData = "..\\..\\..\\appdata";
            var dtcFolder = Path.Combine(appData, "dtc");
            var outputFolder = Path.Combine(dtcFolder, "output");

            var srcFile = dtcFolder + "\\source.dtc";
            var sectionFile = dtcFolder + "\\cmds.dtc";


            var sourceFilePaths = new Dictionary<string, Sections>();
            sourceFilePaths.Add(srcFile, Sections.None);
            sourceFilePaths.Add(sectionFile, Sections.CMDS);

            var outputFile = Path.Combine(outputFolder, $"Merge_Source_And_CMDS-{Guid.NewGuid()}.dtc");

            var criteria = new MergeCriteria
            {
                SourceFilePaths = sourceFilePaths,
                DestinationFilePath = outputFile
            };

            var helper = new MergeHelper();
            helper.Merge(criteria);

            Assert.IsTrue(System.IO.File.Exists(outputFile), $"Output file was not created: {outputFile}");

            var srcdata = System.IO.File.ReadAllText(srcFile);
            var sectiondata = System.IO.File.ReadAllText(sectionFile);
            var outputdata = System.IO.File.ReadAllText(outputFile);

            Assert.IsTrue(srcdata != sectiondata);
            Assert.IsTrue(srcdata != outputdata);
            Assert.IsTrue(sectiondata != outputdata);

            //System.IO.File.Delete(outputFile);
        }

        [TestMethod]
        public void Merge_Source_And_COMMS()
        {
            // Arrange
            var appData = "..\\..\\..\\appdata";
            var dtcFolder = Path.Combine(appData, "dtc");
            var outputFolder = Path.Combine(dtcFolder, "output");

            var srcFile = dtcFolder + "\\source.dtc";
            var sectionFile = dtcFolder + "\\comms.dtc";


            var sourceFilePaths = new Dictionary<string, Sections>();
            sourceFilePaths.Add(srcFile, Sections.None);
            sourceFilePaths.Add(sectionFile, Sections.COMMS);

            var outputFile = Path.Combine(outputFolder, $"Merge_Source_And_COMMS-{Guid.NewGuid()}.dtc");

            var criteria = new MergeCriteria
            {
                SourceFilePaths = sourceFilePaths,
                DestinationFilePath = outputFile
            };

            var helper = new MergeHelper();
            helper.Merge(criteria);

            Assert.IsTrue(System.IO.File.Exists(outputFile), $"Output file was not created: {outputFile}");

            var srcdata = System.IO.File.ReadAllText(srcFile);
            var sectiondata = System.IO.File.ReadAllText(sectionFile);
            var outputdata = System.IO.File.ReadAllText(outputFile);

            Assert.IsTrue(srcdata != sectiondata);
            Assert.IsTrue(srcdata != outputdata);
            Assert.IsTrue(sectiondata != outputdata);

            //System.IO.File.Delete(outputFile);
        }

        [TestMethod]
        public void Merge_Source_And_ELINT()
        {
            // Arrange
            var appData = "..\\..\\..\\appdata";
            var dtcFolder = Path.Combine(appData, "dtc");
            var outputFolder = Path.Combine(dtcFolder, "output");

            var srcFile = dtcFolder + "\\source.dtc";
            var sectionFile = dtcFolder + "\\elint.dtc";


            var sourceFilePaths = new Dictionary<string, Sections>();
            sourceFilePaths.Add(srcFile, Sections.None);
            sourceFilePaths.Add(sectionFile, Sections.ELINT);

            var outputFile = Path.Combine(outputFolder, $"Merge_Source_And_ELINT-{Guid.NewGuid()}.dtc");

            var criteria = new MergeCriteria
            {
                SourceFilePaths = sourceFilePaths,
                DestinationFilePath = outputFile
            };

            var helper = new MergeHelper();
            helper.Merge(criteria);

            Assert.IsTrue(System.IO.File.Exists(outputFile), $"Output file was not created: {outputFile}");

            var srcdata = System.IO.File.ReadAllText(srcFile);
            var sectiondata = System.IO.File.ReadAllText(sectionFile);
            var outputdata = System.IO.File.ReadAllText(outputFile);

            Assert.IsTrue(srcdata != sectiondata);
            Assert.IsTrue(srcdata != outputdata);
            Assert.IsTrue(sectiondata != outputdata);

            //System.IO.File.Delete(outputFile);
        }

        [TestMethod]
        public void Merge_Source_And_All()
        {
            // Arrange
            var appData = "..\\..\\..\\appdata";
            var dtcFolder = Path.Combine(appData, "dtc");
            var outputFolder = Path.Combine(dtcFolder, "output");

            var files = Directory.GetFiles(dtcFolder, "*.dtc");
            if (files.Length < 2)
                Assert.Inconclusive("Not enough .dtc files in the appdata/dtc folder for this test.");

            var sourceFilePaths = new Dictionary<string, Sections>();
            var baseFile = string.Empty;

            // Determine source file and files to merge based on filename
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToUpper();

                if (baseFile == string.Empty && fileName.Contains("SOURCE"))
                {
                    baseFile = file;
                    sourceFilePaths[file] = Sections.None;
                    break;
                }
            }

            // Add remaining files based on section names in their filenames
            foreach (var file in files)
            {
                if (file == baseFile)
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(file).ToUpper();
                var sections = Sections.None;

                if (fileName.Contains("COMMS"))
                    sections |= Sections.COMMS;
                if (fileName.Contains("ELINT"))
                    sections |= Sections.ELINT;
                if (fileName.Contains("STPS"))
                    sections |= Sections.STPS;
                if (fileName.Contains("CMDS"))
                    sections |= Sections.CMDS;

                if (sections != Sections.None)
                    sourceFilePaths[file] = sections;
            }

            if (sourceFilePaths.Count < 2)
                Assert.Inconclusive("Not enough .dtc files with section identifiers in the appdata/dtc folder for this test.");

            var filename = $"Merge_Source_And_All-{Guid.NewGuid()}";
            var outputFile = Path.Combine(outputFolder, $"{filename}.dtc");

            var criteria = new MergeCriteria
            {
                SourceFilePaths = sourceFilePaths,
                DestinationFilePath = outputFile
            };

            var helper = new MergeHelper();

            // Act
            helper.Merge(criteria);

            // Assert
            Assert.IsTrue(System.IO.File.Exists(outputFile), $"Output file was not created: {outputFile}");


            var outputdata = System.IO.File.ReadAllText(outputFile);

            Assert.IsTrue(outputdata.Contains($"\"name\": \"{filename}"));
            Assert.IsTrue(outputdata.Contains("\"freq\": 333")); //comms
            Assert.IsTrue(outputdata.Contains("\"note\": \"mystptest1")); //stps
            Assert.IsTrue(outputdata.Contains("\"SalvoInterval\": 7.77")); //cmds
            //Assert.IsTrue(outputdata.Contains("\"note\": \"mystcmdtest1")); //elint

            //System.IO.File.Delete(outputFile);
        }
    }
}
