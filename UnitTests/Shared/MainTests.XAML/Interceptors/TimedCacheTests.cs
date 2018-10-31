﻿using Cauldron.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InterceptorsTests
{
    [TestClass]
    public class TimedCacheTests
    {
        [TestMethod]
        public void Async_Method_Test()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = TestMethodAsync().Result;

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 4000);

            stopwatch.Restart();

            result = TestMethodAsync().Result;

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);
        }

        [TestMethod]
        public void Async_Method_With_TryCatch_Test()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = TestMethod_With_Try_Catch_Async().Result;

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 4000);

            stopwatch.Restart();

            result = TestMethod_With_Try_Catch_Async().Result;

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);
        }

        [TestMethod]
        public void Method_Test()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = TestMethod();

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 4000);

            stopwatch.Restart();

            result = TestMethod();

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);
        }

        [TestMethod]
        public void Method_With_TryCatch_Test()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = TestMethod_With_Try_Catch();

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 4000);

            stopwatch.Restart();

            result = TestMethod_With_Try_Catch();

            stopwatch.Stop();
            Assert.AreEqual("Hello", result);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);
        }

        [TimedCache(10)]
        private string TestMethod()
        {
            Thread.Sleep(5000);
            return "Hello";
        }

        [TimedCache(10)]
        private string TestMethod(string arg1, int arg2, Guid guid, TestContext testContext)
        {
            Thread.Sleep(5000);
            return "TestMethod With 5";
        }

        [TimedCache(10)]
        private string TestMethod_With_Try_Catch()
        {
            try
            {
                Thread.Sleep(5000);
                return "Hello";
            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        [TimedCache(10)]
        private async Task<string> TestMethod_With_Try_Catch_Async()
        {
            try
            {
                Thread.Sleep(5000);
                return await Task.Run(() => "Hello");
            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        [TimedCache(10)]
        private async Task<string> TestMethodAsync(string arg1, int arg2, Guid guid, TestContext testContext)
        {
            Thread.Sleep(5000);
            return await Task.Run(() => "TestMethod With 5");
        }

        [TimedCache(10)]
        private async Task<string> TestMethodAsync()
        {
            Thread.Sleep(5000);
            return await Task.Run(() => "Hello");
        }
    }
}