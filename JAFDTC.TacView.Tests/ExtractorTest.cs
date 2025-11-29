using JAFDTC.TacView.Models;

namespace JAFDTC.TacView.Tests
{
    [TestClass]
    public sealed class ExtractorTest
    {
        [TestMethod] 
        [Ignore] //when you need more extensive test data...
        public void Test_Extract__LOCALFILES()
        {
            using var ectractor = new Extractor();

            // var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251127-051510-DCS-Host-SPS-Contention-Syria-SARH-1.9.txt.acmi" });
            var result = ectractor.Extract(new() { FilePath = @"C:\Users\VT\Downloads\Tacview-20251025-121807-DCS-Host-SPS-Contention-Caucasus-Modern.txt.acmi" });

            //test for various data issues/states (new enums, etc)
            Assert.IsTrue(result != null);
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
                Categories = 
                [
                    CategoryType.Ground_AntiAircraft,
                    CategoryType.Ground_Heavy_Armor_Vehicle_Tank,
                    CategoryType.Ground_Static_Aerodrome,
                    CategoryType.Ground_Static_Building,
                    CategoryType.Ground_Vehicle,
                ] 
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p=>p.Category == CategoryType.Sea_Watercraft) == 0);
            Assert.IsTrue(result.Count(p=>p.Category == CategoryType.Air_FixedWing) == 0);
            Assert.IsTrue(result.Count(p=>p.Category == CategoryType.Unknown) == 0);
            Assert.IsTrue(result.Count(p=>p.Category == CategoryType.Weapon_Bomb) == 0);
            Assert.IsTrue(result.Count(p=>p.Category == CategoryType.Navaid_Static_Bullseye) == 0);

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
                    CoalitionType.Enemies
                ]
            });

            Assert.IsTrue(result != null);
            Assert.IsTrue(result.Count() > 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.Allies) == 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.Unknown) == 0);
            Assert.IsTrue(result.Count(p => p.Coalition == CoalitionType.Neutrals) == 0);

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
            Assert.IsTrue(result.Count(p => p.IsAlive) == 0);

            var full = ectractor.Extract(new() { FilePath = Path.Combine(Directory.GetParent(Environment.ProcessPath).FullName, "..\\..\\..\\appdata\\test2.zip.acmi") });
            Assert.IsTrue(result.Count() < full.Count());
        }

    }
}
