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
using JAFDTC.Kneeboard.Generate;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.Tests
{
    [TestClass]
    public sealed class KneeboardTest
    {
        [TestInitialize]
        public void Initialize()
        {
            //todo: delete all old test files...
        }

        [TestMethod]
        public void Test_Generate_Null()
        {
            using var generator = new Generate();
            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(null));
        }

        [TestMethod]
        public void Test_Generate_Criteria_Required()
        {
            using var generator = new Generate();

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Airframe = "",
                Name = "",
                PathOutput = "",
                PathTemplates = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Airframe = "F-16C_50",
                Name = "",
                PathOutput = "",
                PathTemplates = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Airframe = "F-16C_50",
                Name = "JAF_TEST",
                PathOutput = "",
                PathTemplates = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Airframe = "F-16C_50",
                Name = "JAF_TEST",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = ""
            }));

        }

        [TestMethod]
        public void Test_Generate_Output_Invalid()
        {
            using var generator = new Generate();
            Assert.ThrowsException<DirectoryNotFoundException>(() => generator.GenerateKneeboards(new() 
            {
                Airframe = "F-16C_50",
                Name = "JAF_TEST",
                PathOutput = "..\\..\\..\\appdata\\missing-output-folder",
                PathTemplates = "..\\..\\..\\appdata\\kb\\",
            }));
        }

        [TestMethod]
        public void Test_Generate_Templates_Missing()
        {
            using var generator = new Generate();
            Assert.ThrowsException<DirectoryNotFoundException>(() => generator.GenerateKneeboards(new()
            {
                Airframe = "F-16C_50",
                Name = "JAF_TEST",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "..\\..\\..\\appdata\\kb\\missing",
            }));
        }

        [TestMethod]
        public void Test_Generate_Process_F16()
        {
            using var generator = new Generate();

            var result = generator.GenerateKneeboards(new()
            {
                Airframe = "F-16C_50",
                Name = $"JAF_TEST_{DateTime.Now.ToString("yyyyMMddhhmmss")}",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "..\\..\\..\\appdata\\kb\\",
            });

            Assert.IsTrue(result.HasData());
            Assert.IsTrue(result.Count == 6);
        }

    }
}
