using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using log4net.Util;
using NUnit.Framework;
using RollbarSharp;

namespace log4net.RollbarSharp.Test
{
    [TestFixture]
    public class RollbarAppenderTest
    {
        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            Util.LogLog.InternalDebugging = true;

            Hierarchy hierarchy = (Hierarchy) LogManager.GetRepository();
            PatternLayout patternLayout = new PatternLayout { ConversionPattern = "%d [%t] %-5p %m%n" };

            patternLayout.ActivateOptions();

            TraceAppender tracer = new TraceAppender { Layout = patternLayout };

            tracer.ActivateOptions();

            hierarchy.Root.AddAppender(tracer);

            ConsoleAppender console = new ConsoleAppender
                                          {
                                              Name = "console",
                                              Layout = patternLayout,
                                              Target = "Console.Out",
                                              Threshold = Level.All,
                                          };

            console.ActivateOptions();

            hierarchy.Root.AddAppender(console);

            RollingFileAppender rollingFile = new RollingFileAppender
                                                  {
                                                      Name = "file",
                                                      Layout = patternLayout,
                                                      AppendToFile = true,
                                                      RollingStyle = RollingFileAppender.RollingMode.Size,
                                                      MaxSizeRollBackups = 4,
                                                      MaximumFileSize = "100KB",
                                                      StaticLogFileName = true,
                                                      LockingModel = new FileAppender.MinimalLock(),
                                                      File = "logs\\logFile.txt",
                                                  };

            rollingFile.ActivateOptions();

            hierarchy.Root.AddAppender(rollingFile);

            RollbarAppender rollbar = new RollbarAppender
                                          {
                                              Name = "rollbar",
                                              Layout = patternLayout,
                                              AccessToken = "3203880e148b43b4b1a14430fb41957a",
                                              Threshold = Level.Notice,
                                          };

            rollbar.ActivateOptions();

            hierarchy.Root.AddAppender(rollbar);

            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }

        private bool wait = true;

        [Test]
        public void LogToRollbar()
        {
            ILog logger = LogManager.GetLogger(typeof(RollbarAppenderTest));

            logger.Debug("This is a debug message");
            logger.Info("This is an info message");
            logger.Warn("This is a warning message");
            logger.Error("This is an error message");
            logger.Fatal("This is a fatal message");

            Thread.Sleep(30 * 1000);
        }

        [Test]
        public void RawLogToRollbar()
        {
            var client = new RollbarClient("3203880e148b43b4b1a14430fb41957a");

            client.RequestStarting += this.RollbarClient_RequestStarting;
            client.RequestCompleted += this.RollbarClient_RequestCompleted;

            var notice = client.NoticeBuilder.CreateMessageNotice("This is a test message");

            notice.Level = "error";
            notice.Title = "Test Message";

            client.Send(notice);

            while (wait) { }
        }

        private void RollbarClient_RequestCompleted(object source, RequestCompletedEventArgs args)
        {
            if (args.Result.IsSuccess)
                LogLog.Debug(typeof(RollbarAppender), "Request was successful: " + args.Result.Message);
            else
                LogLog.Error(typeof(RollbarAppender), "Request failed: " + args.Result);

            wait = false;
        }

        private void RollbarClient_RequestStarting(object source, RequestStartingEventArgs args)
        {
            var client = (RollbarClient) source;

            LogLog.Debug(typeof(RollbarAppender), string.Format("Sending request to {0}: {1}", client.Configuration.Endpoint, args.Payload));
        }
    }
}
