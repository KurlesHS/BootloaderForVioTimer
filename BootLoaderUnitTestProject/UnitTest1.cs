using BootLoader.Device;
using BootLoader.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BootLoaderUnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        private int _count;

        [TestMethod]
        public void TestPacket() {

            var p = new Packet();
            var data = new byte[0x200];
            const Packet.TypeOfPacket type = Packet.TypeOfPacket.FirstPacket;
            const string okResponce = "OK";
            const string badResponce = "BAD";
            const int waitResponce = 3000;
            const int retryCount = 3;
            p.DataBytes = data;
            p.Type = type;
            p.OkResponse = okResponce;
            p.BadResponse = badResponce;
            p.WaitResponseTimeout = waitResponce;
            p.RetryCount = retryCount;
            Assert.AreEqual(p.DataBytes, data);
            Assert.AreEqual(p.Type, type);
            Assert.AreEqual(p.OkResponse, okResponce);
            Assert.AreEqual(p.BadResponse, badResponce);
            Assert.AreEqual(p.WaitResponseTimeout, waitResponce);
            Assert.AreEqual(p.RetryCount, retryCount);
        }

        [TestMethod]
        public void TestFakeTimer() {
            var t = new FakeTimer();
            _count = 0;
            t.Elapsed += OnElapsed;
            t.Start(100.0);
            t.Advance(1001.0);
            Assert.AreEqual(10, _count);
        }

        private void OnElapsed(object sender, TimerEventArg timerEventArg)
        {
            _count++;
        }
    }

}