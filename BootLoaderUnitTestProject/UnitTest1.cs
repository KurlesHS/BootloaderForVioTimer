using System;
using System.IO;
using System.Text;
using BootLoader.Device;
using BootLoader.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrepareFirmware;

namespace BootLoaderUnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        private int _count;
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem(@"crypt.bin")]
        [DeploymentItem(@"decrypt.bin")]
        public void TestVioCrypt() {
            byte[] cryptTable = LoadTable("crypt.bin", "can't load crypt table");
            byte[] decryptTable = LoadTable("decrypt.bin", "can't load decrypt table");
            var testObject = new VioCrypt {CryptTable = cryptTable, DecryptTable = decryptTable};
            Assert.IsNotNull(testObject.DecryptTable);
            Assert.IsNotNull(testObject.CryptTable);

            var testBytearray = new byte[0x10000];
            var cryptedBytearray = new byte[0x10000];
            var decryptedBytearray = new byte[0x10000];
            var rnd = new Random((int) DateTime.Now.Ticks);
            for (var idx = 0; idx < testBytearray.Length; ++idx) testBytearray[idx] = (byte) rnd.Next(0, 0x100);
            const int packetLen = 0x20;
            var packetCount = testBytearray.Length / packetLen;
            testObject.ResetCryptState();
            var internalBuffer = new byte[packetLen];
            // шифруем
            for (var packetNum = 0; packetNum < packetCount; ++packetNum) {
                Array.Copy(testBytearray, packetNum * packetLen, internalBuffer, 0, packetLen);
                var result = testObject.ContinueCrypt(internalBuffer);
                Array.Copy(result, 0, cryptedBytearray, packetNum * packetLen, packetLen);
            }
            // обратная операция
            testObject.ResetCryptState();
            for (var packetNum = 0; packetNum < packetCount; ++packetNum)
            {
                Array.Copy(cryptedBytearray, packetNum * packetLen, internalBuffer, 0, packetLen);
                var result = testObject.ContinueCrypt(internalBuffer);
                Array.Copy(result, 0, decryptedBytearray, packetNum * packetLen, packetLen);
            }

            for (var idx = 0; idx < testBytearray.Length; ++idx) {
                Assert.AreEqual(testBytearray[idx], decryptedBytearray[idx], "error on index:" + idx);
            }
        }

        private static byte[] LoadTable(string filename, string errorString) {
            if (errorString == null) throw new ArgumentNullException("errorString");
            byte[] table = null;
            try {
                using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read))) {
                    table = reader.ReadBytes(0x100);
                }
            } catch (Exception) {
                Assert.Fail(errorString);
            }
            Assert.IsNotNull(table);
            Assert.AreEqual(0x100, table.Length);
            return table;
        }


        [TestMethod]
        public void TestPacket() {
            var p = new Packet();
            var data = new byte[0x200];
            const string okResponce = "OK";
            const string badResponce = "BAD";
            const int waitResponce = 3000;
            const int retryCount = 3;
            p.DataBytes = data;
            p.OkResponse = okResponce;
            p.BadResponse = badResponce;
            p.WaitResponseTimeout = waitResponce;
            p.RetryCount = retryCount;
            Assert.AreEqual(p.DataBytes, data);
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

        private void OnElapsed(object sender, TimerEventArg timerEventArg) {
            _count++;
        }

        private bool _finished;
        private string _errorString;

        [TestMethod]
        [DeploymentItem(@"xml_loader_prefix.xml")]
        public void TestDeviceImplementation() {
            string xmlString = File.ReadAllText(@"xml_loader_prefix.xml");
            const int packetLenght = 34;
            const int packetCount = 0x100;
            var buffer = new Byte[packetLenght * packetCount + xmlString.Length + 1];
            Array.Copy(Encoding.ASCII.GetBytes(xmlString), buffer, xmlString.Length);
            buffer[xmlString.Length] = 0x00;
            var memoryStream = new MemoryStream(buffer);
            var protocol = new FakeProtocol();
            var timer = new FakeTimer();
            var device = new TimerDeviceImpl(protocol, timer) {PacketLenght = packetLenght};
            _errorString = "";
            _finished = false;
            device.FinishedHandler += device_FinishedHandler;
            device.ErrorHandler += device_ErrorHandler;
            Assert.IsTrue(device.StartFlashing(memoryStream));
            for (var i = 0; i < 100; ++i)
                timer.Advance(3500);
            var iterCount = 0;
            while (!_finished) {
                protocol.Process();
                iterCount++;
                if (iterCount == 100)
                    timer.Advance(1001);
            }
            Assert.AreEqual(packetCount + 1 + 100, protocol.PacketCount);
            Assert.AreEqual("", _errorString);
        }

        private void device_ErrorHandler(object sender, string description) {
            _finished = true;
            _errorString = description;
        }

        private void device_FinishedHandler(object sender) {
            _finished = true;
        }
    }
}