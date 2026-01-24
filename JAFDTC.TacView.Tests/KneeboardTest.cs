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
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                PathOutput = "",
                PathTemplates = "",
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
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
                                            SCL = "bla",
                                            Position = new()
                                        },
                                        new()
                                        {
                                            Name = "Raven",
                                            DataId = "67056",
                                            SCL = "bla2",
                                            Position = new()

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
                                                    PresetId = 1,
                                                    Frequency = "251.0",
                                                    Description = "TAC COMMON",
                                                    Modulation = "AM"
                                                },
                                                new ()
                                                {
                                                    PresetId = 5, //skip some to test
                                                    Frequency = "270.75",
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
                                                    PresetId = 1,
                                                    Frequency = "138.25",
                                                    Description = "Viper Primary",
                                                    Modulation = "FM"
                                                },
                                                new ()
                                                {
                                                    PresetId = 2,
                                                    Frequency = "138.75",
                                                    Description = "Viper Secondary",
                                                    Modulation = "FM"
                                                },
                                                new ()
                                                {
                                                    PresetId = 3,
                                                    Frequency = "138.5",
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
                                            RouteId = 1,
                                            Name = "Route 1",
                                            NavPoints =
                                            [
                                                new()
                                                {
                                                    NavpointId = 1,
                                                    Name = "", //see if it hanldes to STP1... Bassel Al-Assad
                                                    Location = new()
                                                    {
                                                        Altitude = "93",
                                                        Latitude = "N 35° 24.100'",
                                                        Longitude = "E 035° 57.023'"
                                                    },
                                                },
                                                new()
                                                {
                                                    NavpointId = 2,
                                                    Name = "IP", //ocean E of Rene
                                                    Location = new()
                                                    {
                                                        Altitude = "250",
                                                        Latitude = "N 34° 35.510'",
                                                        Longitude = "E 035° 41.949'"
                                                    },
                                                },
                                                new()
                                                {
                                                    NavpointId = 3,
                                                    Name = "Target", //Rene Mouawad
                                                    TOS = "15",
                                                    TOT = "13:01",
                                                    Location = new()
                                                    {
                                                        Altitude = "16",
                                                        Latitude = "N 34° 35.357'",
                                                        Longitude = "E 036° 00.686'"
                                                    },
                                                },
                                                new()
                                                {
                                                    NavpointId = 4,
                                                    Name = null, //Hama airbase see if it matches it/finds it
                                                    Location = new()
                                                    {
                                                        Altitude = "984",
                                                        Latitude = "N 35° 07.082'",
                                                        Longitude = "E 036° 42.742'"
                                                    },
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
                            Coalition =  CoalitionType.RED,
                            Location = new()
                            {
                                Altitude = "97",
                                Latitude = "N 35° 07.082'",
                                Longitude = "E 036° 42.742'"
                            },
                            WEZ = 22.0
                        },
                        new()
                        {
                            Name = "ZSU-22", //at hama airbase...
                            Type = "AAA",
                            Coalition =  CoalitionType.RED,
                            Location = new()
                            {
                                Altitude = "97",
                                Latitude = "N 35° 07.082'",
                                Longitude = "E 036° 42.742'"
                            },
                            WEZ = 1.5
                        }
                    ]
                },
                //Name = $"JAF TEST {DateTime.Now.ToString("yyyyMMddhhmmss")}",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "..\\..\\..\\appdata\\kb\\",
                
                //NightMode = false,
                //PathLogo = "..\\..\\..\\appdata\\kb\\misc\\667logo.png",
            });

            Assert.IsTrue(result.HasData());
            Assert.IsTrue(result.Count == 3);
        }

    }
}
