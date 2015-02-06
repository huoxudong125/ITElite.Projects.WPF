using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITElite.Projects.WPF.IO.DeepZoom.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var deepZoomCreator = new DeepZoomCreator();
            deepZoomCreator.CreateSingleComposition(@"D:\TestProjects\ITelite.Projects\ITElite.Projects.WPF\DemoData\WPF_Poster.png",
              @"D:\TestProjects\ITelite.Projects\ITElite.Projects.WPF\DemoData\WPF_Poster.dzi"
                , ImageType.Png);
        }

    }
}
