using genscript;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.IO;

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
            
            Assert.Inconclusive("TODO: Implement code to verify target");
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
            Program.DoJob();
            string sfilePath = GlobalVars.WorkingFolder + string.Format("shouldbe\\{0}.csv",labwareCnt);
            var shouldbeLines = File.ReadAllLines(sfilePath);
            string sResultPath = GlobalVars.WorkingFolder + "Outputs\\readableOutput.csv";
            var allResultLines = File.ReadAllLines(sfilePath);
            bool bEqual = true;
            for (int i = 0; i < shouldbeLines.Length; i++)
            {
                if (allResultLines[i] != shouldbeLines[i])
                {
                    bEqual = false;
                    break;
                }
            }
            Assert.IsTrue(bEqual);
        }

        [TestMethod]
        public void Test2_DoJob24Pos()
        {
            TestDoJob(24);
        }

        [TestMethod]
        public void DoJob16Pos()
        {
            TestDoJob(16);
        }
    }
}
