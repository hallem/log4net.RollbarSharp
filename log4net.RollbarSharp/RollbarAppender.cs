using log4net.Appender;
using log4net.Core;
//using log4net.Layout;
using log4net.Util;
using RollbarSharp;

namespace log4net.RollbarSharp
{
    public class RollbarAppender : AppenderSkeleton
    {
        #region Public Instance Properties

        public string AccessToken { get; set; }

        //public string Endpoint { get; set; }

        //public PatternLayout Environment { get; set; }

        //public PatternLayout Platform { get; set; }

        //public PatternLayout Language { get; set; }

        //public PatternLayout Framework { get; set; }

        #endregion

        #region Override implementation of AppenderSkeleton

        /// <summary>
        /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent)"/> method.
        /// </summary>
        /// <param name="loggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Writes the event to Rollbar.
        /// </para>
        /// <para>
        /// The format of the output will depend on the appender's layout.
        /// </para>
        /// </remarks>
        override protected void Append(LoggingEvent loggingEvent)
        {
            var client = new RollbarClient(this.AccessToken);

            client.RequestStarting += this.RollbarClient_RequestStarting;
            client.RequestCompleted += this.RollbarClient_RequestCompleted;

            //if (!string.IsNullOrWhiteSpace(this.Endpoint))
            //    client.Configuration.Endpoint = this.Endpoint;

            //if (this.Environment != null)
            //    client.Configuration.Environment = this.Environment.Format(loggingEvent);

            //if (this.Platform != null)
            //    client.Configuration.Platform = this.Platform.Format(loggingEvent);

            //if (this.Language != null)
            //    client.Configuration.Language = this.Language.Format(loggingEvent);

            var notice = loggingEvent.ExceptionObject != null
                ? client.NoticeBuilder.CreateExceptionNotice(loggingEvent.ExceptionObject)
                : client.NoticeBuilder.CreateMessageNotice(base.RenderLoggingEvent(loggingEvent));

            notice.Level = ConvertLogLevel(loggingEvent.Level);
            //notice.Title = string.Empty;

            client.Send(notice);
        }

        private void RollbarClient_RequestCompleted(object source, RequestCompletedEventArgs args)
        {
            if (args.Result.IsSuccess)
            {
                LogLog.Debug(typeof(RollbarAppender), "Request was successful: " + args.Result.Message);
                return;
            }

            LogLog.Warn(typeof(RollbarAppender), "Request failed: " + args.Result);
        }

        private void RollbarClient_RequestStarting(object source, RequestStartingEventArgs args)
        {
            var client = (RollbarClient) source;

            LogLog.Debug(typeof(RollbarAppender), string.Format("Sending request to {0}: {1}", client.Configuration.Endpoint, args.Payload));
        }

        /// <summary>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        /// <remarks>
        /// <para>
        /// This appender requires a <see cref="Layout"/> to be set.
        /// </para>
        /// </remarks>
        override protected bool RequiresLayout
        {
            get { return true; }
        }

        #endregion Override implementation of AppenderSkeleton

        private static string ConvertLogLevel(Level level)
        {
            if (level.Value > Level.Error.Value)
                return "critical";

            if (level.Value == Level.Error.Value)
                return "error";

            if (level.Value > Level.Notice.Value)
                return "warning";

            if (level.Value == Level.Info.Value)
                return "info";

            return "debug";
        }
    }
}
