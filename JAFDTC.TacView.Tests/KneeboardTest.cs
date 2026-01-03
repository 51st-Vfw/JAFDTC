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
        public void Test_Kneeboard_Null()
        {
            using var generator = new Generate();
            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(null));
        }

        [TestMethod]
        public void Test_Kneeboard_Criteria_Required()
        {
            using var generator = new Generate();

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "",
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "",
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "JAF_TEST",
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "JAF_TEST",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "",
            }));

        }

        [TestMethod]
        public void Test_Kneeboard_Build()
        {
            using var generator = new Generate();

            var result = generator.GenerateKneeboards(new()
            {
                Mission = new()
                {
                    Name = "Operation KB",
                    Owner = new()
                    {
                        Name = "Rage",
                        STN = "67001",
                        Board = "393",
                        Joker = 4000,
                        Lase = 1688,
                        Tacan = 38,
                        TacanBand = 'Y',
                        CommPresets = new Dictionary<int, int>()
                        {
                            { 1, 2 }, //UHF preset 2
                            { 2, 3 }  //VHF preset 3
                        }
                    },
                    Packages =
                    [
                        new()
                        {
                            Name = "Package 1",
                            Flights = 
                            [
                                new()
                                {
                                    Aircraft = Kneeboard.Models.Aircraft.F16,
                                    Name = "COLT-1",
                                    Pilots = 
                                    [
                                        new()
                                        {
                                            Name = "Rage",
                                            DataId = "67001",
                                            IsLead = true,
                                            SCL = "bla",
                                        },
                                        new()
                                        {
                                            Name = "Raven",
                                            DataId = "67056",
                                            IsLead = false,
                                            SCL = "bla2",
                                        }
                                    ],
                                    Radios =
                                    [
                                        new ()
                                        {
                                            RadioId = 1,
                                            Name = "AN/ARC-164",
                                            Presets =
                                            [
                                                new ()
                                                {
                                                    Frequency = 251.0,
                                                    Description = "TAC COMMON",
                                                    Modulation = "AM"
                                                },
                                                new ()
                                                {
                                                    Frequency = 270.75,
                                                    Description = "OVERLOAD 1-1",
                                                    Modulation = "AM"
                                                }                                                
                                            ]
                                        },
                                        new ()
                                        {
                                            RadioId = 2,
                                            Name = "AN/ARC-210",
                                            Presets =
                                            [
                                                new ()
                                                {
                                                    Frequency = 138.25,
                                                    Description = "Viper Primary",
                                                    Modulation = "FM"
                                                },
                                                new ()
                                                {
                                                    Frequency = 138.75,
                                                    Description = "Viper Secondary",
                                                    Modulation = "FM"
                                                },
                                                new ()
                                                {
                                                    Frequency = 138.5,
                                                    Description = "Viper AUX",
                                                    Modulation = "FM"
                                                }
                                            ]
                                        }
                                    ],                                   
                                    Routes = 
                                    [
                                        new()
                                        {
                                            Name = "Route 1",
                                            NavPoints =
                                            [
                                                new()
                                                {
                                                    Altitude = 93,
                                                    Latitude = 35.401667, //N 35° 24.100’
                                                    Longitude = 35.950383, //E 035° 57.023’
                                                    Name = "", //see if it hanldes to STP1... Bassel Al-Assad
                                                },
                                                new()
                                                {
                                                    Altitude = 250,
                                                    Latitude = 35.950383, //N 34° 35.510’
                                                    Longitude = 35.69915, //E 035° 41.949’
                                                    Name = "IP", //ocean E of Rene
                                                },
                                                new()
                                                {
                                                    Altitude = 16,
                                                    Latitude = 34.589283, //N 34° 35.357’
                                                    Longitude = 36.011433, //E 036° 00.686’
                                                    Name = "Target", //Rene Mouawad
                                                    TOS = "15",
                                                    TOT = "13:01"
                                                },
                                                new()
                                                {
                                                    Altitude = 984,
                                                    Latitude = 35.118033, //N 35° 07.082'
                                                    Longitude = 36.712367, //E 036° 42.742'
                                                    Name = null, //Hama airbase see if it matches it/finds it
                                                }
                                            ]
                                        }
                                        
                                    ]
                                }
                            ]
                        }
                    ],
                    Theater = "Syria",
                    Threats = [
                        new()
                        {
                            Name = "SA-2 Guideline", //at hama airbase...
                            Type = "SAM",
                            Altitude = 97,
                            Latitude = 35.118033,
                            Longitude = 36.712367,
                            WEZ = 22.0
                        },
                        new()
                        {
                            Name = "ZSU-22", //at hama airbase...
                            Type = "AAA",
                            Altitude = 97,
                            Latitude = 35.118033,
                            Longitude = 36.712367,
                            WEZ = 1.5
                        }
                    ]
                },
                Name = $"JAF TEST {DateTime.Now.ToString("yyyyMMddhhmmss")}",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "..\\..\\..\\appdata\\kb\\",
                
                NightMode = false,
                //PathLogo = "..\\..\\..\\appdata\\kb\\misc\\667logo.png",
            });

            Assert.IsTrue(result.HasData());
            Assert.IsTrue(result.Count == 3);
        }

    }
}
