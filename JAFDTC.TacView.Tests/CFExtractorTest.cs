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

using JAFDTC.File.CF;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.Tests
{
    [TestClass]
    public sealed class CFExtractorTest
    {
        [TestMethod]
        public void Test_Extract_Filter_TYPICAL()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test-valid.cf"),
                Coalitions = [ CoalitionType.BLUE], //only blue/red
                UnitCategories =
                [
                    UnitCategoryType.AIRCRAFT
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result[2].Route.Count == 8); //viper3 8 spts

        }

        [TestMethod]
        public void Test_Extract_Null()
        {
            using var ectractor = new Extractor();
            Assert.ThrowsException<ArgumentException>(() => ectractor.Extract(null));

            Assert.ThrowsException<ArgumentException>(() => ectractor.Extract(new() { FilePath = null }));
        }

        [TestMethod]
        public void Test_Extract_File_Flights_Dupe()
        {
            using var ectractor = new Extractor();
            Assert.ThrowsException<InvalidDataException>(() => ectractor.Extract(new() { FilePath = "..\\..\\..\\appdata\\test-invalid.cf" }));
        }

        [TestMethod]
        public void Test_Extract_File_Missing()
        {
            using var ectractor = new Extractor();
            Assert.ThrowsException<FileNotFoundException>(() => ectractor.Extract(new() { FilePath = "/MISSING/test-valid.cf" }));
        }

        [TestMethod]
        public void Test_Extract_File()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test-valid.cf") });

            Assert.IsTrue(result != null);
        }


        [TestMethod]
        public void Test_Extract_Filter_Categories()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test-valid.cf"),
                UnitCategories =
                [
                    UnitCategoryType.AIRCRAFT
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.GROUND) == 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.NAVAL) == 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.AIRCRAFT) > 0);
            Assert.IsTrue(result.Count(p => p.Category == UnitCategoryType.HELICOPTER) == 0);
        }

        [TestMethod]
        public void Test_Extract_Filter_Coalition()
        {
            using var ectractor = new Extractor();
            var result = ectractor.Extract(new()
            {
                FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test-valid.cf"),
                Coalitions =
                [
                    CoalitionType.BLUE
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.BLUE) > 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.RED) == 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.NEUTRAL) == 0);
        }

    }
}
