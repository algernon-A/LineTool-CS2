// <copyright file="Logging.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LineTool
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Colossal.Logging;

    /// <summary>
    /// Logging utility class.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Game logger reference.
        /// </summary>
        private static readonly ILog GameLogger = LogManager.GetLogger("LineTool");

        /// <summary>
        /// Logs a debugging message.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        [Conditional("DEBUG")]
        public static void LogDebug(params object[] messages) => LogMessage(Level.Debug, null, messages);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void LogInfo(params object[] messages) => LogMessage(Level.Info, null, messages);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void LogError(params object[] messages) => LogMessage(Level.Error, null, messages);

        /// <summary>
        /// Logs an exception message.
        /// </summary>
        /// <param name="e">Exception to log.</param>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        public static void LogException(Exception e, params object[] messages) => LogMessage(Level.Error, e, messages);

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="logLevel">Logging level to use.</param>
        /// <param name="e">Exception to log (<c>null</c> if none.</param>
        /// <param name="messages">Message to log (individual strings will be concatenated).</param>
        private static void LogMessage(Level logLevel, Exception e, params object[] messages)
        {
            // Build message.
            StringBuilder logMessage = new ();
            for (int i = 0; i < messages.Length; ++i)
            {
                // Append "null" in place of any null values.
                logMessage.Append(messages[i] ?? "null");
            }

            // Write to log.
            GameLogger.Log(logLevel, logMessage.ToString(), e);
        }
    }
}