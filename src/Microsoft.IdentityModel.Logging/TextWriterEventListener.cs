//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;

namespace Microsoft.IdentityModel.Logging
{
    /// <summary>
    /// Event listener that writes logs to a file or a fileStream provided by user.
    /// </summary>
    public class TextWriterEventListener : EventListener
    {
        private StreamWriter _streamWriter;
        private object _streamLock = new object();
        private bool _disposeStreamWriter = true;
        private int _numberOfEventsToCache = 1000;
        private int _numberCached = 0;
        private StringBuilder _cachedEvents = new StringBuilder();

        /// <summary>
        /// Name of the default log file, excluding its path.
        /// </summary>
        public readonly static string DefaultLogFileName = "IdentityModelLogs.txt";

        /// <summary>
        /// Initializes a new instance of <see cref="TextWriterEventListener"/> that writes logs to text file.
        /// </summary>
        public TextWriterEventListener()
        {
            try
            {
                Stream fileStream = new FileStream(DefaultLogFileName, FileMode.OpenOrCreate, FileAccess.Write);
                _streamWriter = new StreamWriter(fileStream);
                _streamWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionMessage(new InvalidOperationException(LogMessages.MIML10001, ex));
                throw;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextWriterEventListener"/> that writes logs to text file.
        /// </summary>
        /// <param name="filePath">location of the file where log messages will be written.</param>
        public TextWriterEventListener(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw LogHelper.LogArgumentNullException(nameof(filePath));

            try
            {
                Stream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                _streamWriter = new StreamWriter(fileStream);
                _streamWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionMessage(new InvalidOperationException(LogMessages.MIML10001, ex));
                throw;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextWriterEventListener"/> that writes logs to text file.
        /// </summary>
        /// <param name="streamWriter">StreamWriter where logs will be written.</param>
        public TextWriterEventListener(StreamWriter streamWriter)
        {
            if (streamWriter == null)
                throw LogHelper.LogArgumentNullException("streamWriter");

            _streamWriter = streamWriter;
            _disposeStreamWriter = false;
        }

        /// <summary>
        /// Call to flush out any unwritten events.
        /// </summary>
        public void Flush()
        {
            _streamWriter.WriteLine(_cachedEvents.ToString());
            _cachedEvents.Clear();
        }

        /// <summary>
        /// Called whenever an event has been written by an event source for which the event listener has enabled events.
        /// </summary>
        /// <param name="eventData"><see cref="EventWrittenEventArgs"/></param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null || eventData.Payload == null || eventData.Payload.Count <= 0)
                return;

            System.Threading.Interlocked.Increment(ref _numberCached);
            if (_numberCached > _numberOfEventsToCache)
            {
                lock (_streamLock)
                {
                    _numberCached = 0;
                    _streamWriter.WriteLine(_cachedEvents.ToString());
                    _cachedEvents.Clear();
                }
            }
            else
            {
                for (int i = 0; i < eventData.Payload.Count; i++)
                {
                    _cachedEvents.AppendLine(eventData.Payload[i].ToString());
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="TextWriterEventListener"/> class.
        /// </summary>
        public override void Dispose()
        {
            if (_disposeStreamWriter && _streamWriter != null)
            {
                _streamWriter.Flush();
                _streamWriter.Dispose();
            }

            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
