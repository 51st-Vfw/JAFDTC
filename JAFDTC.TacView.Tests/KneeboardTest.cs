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
                Owner = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "",
                PathOutput = "",
                PathTemplates = "",
                Owner = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "JAF_TEST",
                PathOutput = "",
                PathTemplates = "",
                Owner = ""
            }));

            Assert.ThrowsException<ArgumentException>(() => generator.GenerateKneeboards(new()
            {
                Mission = null,
                Name = "JAF_TEST",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "",
                Owner = ""
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
                    Name = "JAF TEST MISSION",
                    Packages =
                    [
                        new()
                        {
                            Name = "Package 1",
                            Flights = 
                            [
                                new()
                                {
                                    Aircraft = "F-16C_50",
                                    Name = "COLT-1",
                                    Pilots = 
                                    [
                                        new()
                                        {
                                            Name = "Rage",
                                            IsLead = true
                                        }
                                    ],
                                    Comms = 
                                    [
                                        new ()
                                        {
                                            CommId = 1,
                                            Frequency = 251.0,
                                            Description = "TAC COMMON"
                                        }
                                    ],
                                    Navs = 
                                    [
                                        new ()
                                        {
                                            Name = "Takeoff",
                                            Altitude = 43,
                                            Latitude = 34.0000,
                                            Longitude = 36.0000
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
                            Name = "SA-2 Guideline",
                            Type = "SAM",
                            Altitude = 97,
                            Latitude = 35.118033,
                            Longitude = 36.712367,
                            WEZ = 22.0
                        }
                    ]

                },
                Name = $"JAF_TEST_{DateTime.Now.ToString("yyyyMMddhhmmss")}",
                PathOutput = "..\\..\\..\\appdata\\kb\\output",
                PathTemplates = "..\\..\\..\\appdata\\kb\\",
                Owner = "Rage",
                NightMode = false,
                PathLogo = "..\\..\\..\\appdata\\kb\\misc\\667logo.png",
                //Flight = new()
                //{
                //    Category = UnitCategoryType.AIRCRAFT,
                //    Coalition = CoalitionType.BLUE,
                //    Name = "WF 2",
                //    Route = 
                //    [
                //        new()
                //        {
                //            Altitude = 43,
                //            Latitude = 34.0000,
                //            Longitude = 36.0000,
                //            Name = "", //see if it hanldes to STP1...
                //            TimeOn = 0
                //        },
                //        new()
                //        {
                //            Altitude = 5000,
                //            Latitude = 34.1000,
                //            Longitude = 36.2000,
                //            Name = "IP",
                //            TimeOn = 0
                //        },
                //        new()
                //        {
                //            Altitude = 278,
                //            Latitude = 34.2200,
                //            Longitude = 36.3300,
                //            Name = "Target",
                //            TimeOn = 0
                //        },
                //         new()
                //        {
                //            Altitude = 984,
                //            Latitude = 35.118033, //N 35° 07.082'
                //            Longitude = 36.712367, //E 036° 42.742'
                //            Name = null, //Hama airbase see if it matches it/finds it
                //            TimeOn = 0
                //        }
                //    ],
                //    UniqueID = "asdf",
                //    Units =
                //    [
                //        new ()
                //        {
                //            IsAlive = true,
                //            Name = "WF 2-1",
                //            Position = new()
                //            {
                //                Altitude = 278,
                //                Latitude = 34.2200,
                //                Longitude = 36.3300,
                //                Name = "",
                //                TimeOn = 0
                //            },
                //            Type = "F-16C_50",
                //            UniqueID = "fdad"
                //        },
                //        new ()
                //        {
                //            IsAlive = true,
                //            Name = "WF 2-2",
                //            Position = new()
                //            {
                //                Altitude = 278,
                //                Latitude = 34.2200,
                //                Longitude = 36.3300,
                //                Name = "",
                //                TimeOn = 0
                //            },
                //            Type = "F-16C_50",
                //            UniqueID = "fdad"
                //        }
                //    ]
                //},
                //Pilots =
                //[
                //    new()
                //    {
                //        Name = "Rage",
                //        Callsign = "WF 21",
                //        IsLead = true,
                //        IsTDOA = true,
                //        TNDL = "67001",
                //        Joker = 4000,
                //        LaseCode = 1688,
                //        Tacan = 38,
                //        TacanBand = 'Y'
                //    },
                //    new()
                //    {
                //        Name = "Raven",
                //        IsLead = false,
                //        IsTDOA = true,
                //        TNDL = "67062",
                //    }
                //],
                //Comms =
                //[
                //    new Radio
                //    {
                //        CommMode = 1,
                //        Name = "AN/ARC-164",
                //        FrequencyName = "UHF",
                //        Preset = 2,
                //        FrequencyMin = 200.0,
                //        FrequencyMax = 400.0,
                //        MonitorGuard = true,
                //        Channels =
                //        [
                //            new (){ ChannelId = 1, Frequency = 333.0, Description = "Tac Common" },
                //            new (){ ChannelId = 2, Frequency = 270.5, Description = "AWACS AI" },
                //        ]
                //    },
                //     new Radio
                //    {
                //        CommMode = 2,
                //        Name = "AN/ARC-210",
                //        FrequencyName = "VHF",
                //        Preset = 3,
                //        FrequencyMin = 100.0,
                //        FrequencyMax = 200.0,
                //        MonitorGuard = null,
                //        Channels =
                //        [
                //            new (){ ChannelId = 1, Frequency = 138.25, Description = "Viper 1" },
                //            new (){ ChannelId = 2, Frequency = 138.75, Description = "Viper 2" },
                //            new (){ ChannelId = 3, Frequency = 138.5, Description = "Viper AUX" },
                //        ]
                //    }

                //]
            });

            Assert.IsTrue(result.HasData());
            Assert.IsTrue(result.Count == 3);
        }

    }
}
