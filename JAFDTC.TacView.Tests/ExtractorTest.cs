// ********************************************************************************************************************
//
// ExtractorTest.cs -- <one_line_descripti8on>
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

using JAFDTC.Core.Extensions;
using JAFDTC.File.ACMI.Extensions;
using JAFDTC.File.ACMI.Models;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.File.ACMI.Tests
{
    [TestClass]
    public sealed class ExtractorTest
    {
        //[TestMethod] 
        ////[Ignore] //when you need more extensive test data...
        //public void Test_Extract__LOCALFILES()
        //{
        //    using var ectractor = new Extractor();

        //    // var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251127-051510-DCS-Host-SPS-Contention-Syria-SARH-1.9.txt.acmi" });
        //    //var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251025-121807-DCS-Host-SPS-Contention-Caucasus-Modern.txt.acmi" });
        //    //var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251130-035316-DCS-Host-SPS-Contention-Syria-CW-E1.txt.acmi" });
        //    //var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251130-035916-DCS-Host-SPS-Contention-Syria-SARH-1.9.txt.acmi" });
        //    var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251130-122605-DCS-Host-Server_1_Operation_Urban_Thunder_V7.7.88.txt.acmi" });

        //    //test for various data issues/states (new enums, etc)
        //    var colors = result.Select(x => x.DebugInfoDict["Color"]).Distinct().Order().ToList();
        //    var missingColors = result.Where(p => p.Color == ColorType.Unknown).Select(x => x.DebugInfoDict["Color"]).Distinct().Order().ToList();
        //    if (missingColors.Count > 0)
        //    {

        //    }

        //    var coalitions = result.Select(x => x.DebugInfoDict["Coalition"]).Distinct().Order().ToList();
        //    var missingCoalitions = result.Where(p => p.Coalition == CoalitionType.Unknown).Select(x => x.DebugInfoDict["Coalition"]).Distinct().Order().ToList();
        //    if (missingCoalitions.Count > 0)
        //    {

        //    }

        //    var categories = result.Select(x => x.DebugInfoDict["Type"]).Distinct().Order().ToList();
        //    var missingCategories = result.Where(p => p.Category == CategoryType.Unknown).Select(x => x.DebugInfoDict["Type"]).Distinct().Order().ToList();
        //    if (missingCategories.Count > 0)
        //    {

        //    }

        //    var units = result.Select(x => { x.DebugInfoDict.TryGetValue("Name", out var r); return r; }).Distinct().Order().ToList();
        //    var missingUnits = result.Where(p => p.Unit == UnitType.Unknown).Select(x => x.DebugInfoDict["Name"]).Distinct().Order().ToList();
        //    if (missingUnits.Count > 0)
        //    {
        //        var unitHash = new HashSet<string>(Enum.GetNames<UnitType>());
        //        unitHash.Remove("Unknown");
        //        unitHash.Remove("BULLSEYE");

        //        foreach (var unit in missingUnits)
        //        {
        //            var cleaned = unit.ToNormalized();
        //            if (!unitHash.Contains(cleaned))
        //                unitHash.Add(cleaned);
        //        }

        //        //merged total
        //        var output = string.Join("\r\n", unitHash.Select(p => $"{p},").Order());
        //    }

        //    var groups = result.Select(x => { x.DebugInfoDict.TryGetValue("Group", out var r); return r; }).Distinct().Order().ToList();
        //    var names = result.Select(x => { x.DebugInfoDict.TryGetValue("Pilot", out var r); return r; }).Distinct().Order().ToList();


        //    Assert.IsTrue(result != null);
        //}

        [TestMethod]
        public void Test_Extract_Filter_TYPICAL()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi"),
                IsAlive = true, //just whats alive
                Coalitions = [ CoalitionType.RED], //only blue/red
                UnitCategories =
                [
                    UnitCategoryType.GROUND
                ],
                TimeSnippet = null //usually last frame will suffice
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
        }

        [TestMethod]
        public void Test_Extract_Null()
        {
            using var ectractor = new Extractor();
            Assert.ThrowsException<ArgumentException>(() => ectractor.Extract(null));

            Assert.ThrowsException<ArgumentException>(() => ectractor.Extract(new() { FilePath = null }));
        }

        [TestMethod]
        public void Test_Extract_File_Missing()
        {
            using var ectractor = new Extractor();
            Assert.ThrowsException<FileNotFoundException>(() => ectractor.Extract(new() { FilePath = "/MISSING/test1.txt.acmi" }));
        }

        [TestMethod]
        public void Test_Extract_File_TXT()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test1.txt.acmi") });

            Assert.IsTrue(result != null);
        }

        [TestMethod]
        public void Test_Extract_File_ZIP()
        {
            using var ectractor = new Extractor();

            var result = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });

            Assert.IsTrue(result != null);
        }

        [TestMethod]
        public void Test_Extract_TimeSet()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi"),
                TimeSnippet = DateTime.Parse("1/1/2000 11:24:35")
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);

            var full = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });
            Assert.IsTrue(result.Count() < full.Count());
        }

        [TestMethod]
        public void Test_Extract_Filter_Categories()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi"),
                UnitCategories =
                [
                    UnitCategoryType.GROUND
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.NAVAL) == 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.AIRCRAFT) == 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.HELICOPTER) == 0);

            var full = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });
            Assert.IsTrue(result.Count() < full.Count());
        }

        [TestMethod]
        public void Test_Extract_Filter_Coalition()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi"),
                Coalitions =
                [
                    CoalitionType.RED
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.BLUE) == 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.RED) > 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.NEUTRAL) == 0);

            var full = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });
            Assert.IsTrue(result.Count() < full.Count());
        }

        [TestMethod]
        public void Test_Extract_Filter_Dead()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi"),
                IsAlive = false
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Units.HasData() && p.Units.Any(x => x.IsAlive)) == 0);

            var full = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });
            Assert.IsTrue(result.Count() < full.Count());
        }

    }
}
