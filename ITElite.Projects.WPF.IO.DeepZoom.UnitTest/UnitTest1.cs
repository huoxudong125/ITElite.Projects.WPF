using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITElite.Projects.WPF.IO.DeepZoom.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [DeploymentItem("03fig03.jpg")]
        public void TestMethod1()
        {
            //var deepZoomCreator = new DeepZoomCreator();
            //deepZoomCreator.CreateSingleComposition(string.Format("",Assembly.GetAssembly().Location"03fig03.jpg")),
            //  @"03fig03.dzi", ImageType.Jpeg);
        }

    }
}