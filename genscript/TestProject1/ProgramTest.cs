using genscript;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace TestProject1
{
    /// <summary>
    ///This is a test class for ProgramTest and is intended
    ///to contain all ProgramTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ProgramTest
    {
        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Program Constructor
        ///</summary>
        [TestMethod()]
        public void ProgramConstructorTest()
        {
            Program target = new Program();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            GlobalVars.WorkingFolder = Program.GetExeParentFolder() + "data\\";
        }

        [TestMethod]
        public void Test1_Convert2CSV()
        {
            Program.Convert2CSV();
        }

        private void TestDoJob(int labwareCnt)
        {
            GlobalVars.LabwareWellCnt = labwareCnt;
            try
            {
                Program.DoJob();
            }
            catch (System.Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            
            string sfilePath = GlobalVars.WorkingFolder + string.Format("shouldbe\\{0}.csv",labwareCnt);
            var shouldbeLines = File.ReadAllLines(sfilePath);
            string sReadablePath = GlobalVars.WorkingFolder + "Outputs\\readableOutput.csv";
            var allLines = File.ReadAllLines(sfilePath);
            bool bEqual = true;
            for (int i = 0; i < shouldbeLines.Length; i++)
            {
                if (allLines[i] != shouldbeLines[i])
                {
                    bEqual = false;
                    break;
                }
            }
            Assert.IsTrue(bEqual);

            //file count should be 1
            string sfileCntFile = GlobalVars.WorkingFolder + "Outputs\\fileCnt.txt";
            string fileCntContent = File.ReadAllText(sfileCntFile);
            Assert.AreEqual(fileCntContent, "1");

            //result should be true
            string sResultFile = GlobalVars.WorkingFolder + "Outputs\\result.txt";
            string resultFileContent = File.ReadAllText(sResultFile);
            Assert.AreEqual(resultFileContent, "true");
        }

        [TestMethod]
        public void Test2_DoJob24Pos()
        {
            TestDoJob(24);
            Thread.Sleep(1200);
        }

        [TestMethod]
        public void DoJob16Pos()
        {
            TestDoJob(16);
        }
    }
}
