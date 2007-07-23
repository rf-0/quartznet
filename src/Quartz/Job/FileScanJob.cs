/* 
* Copyright 2004-2005 OpenSymphony 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); you may not 
* use this file except in compliance with the License. You may obtain a copy 
* of the License at 
* 
*   http://www.apache.org/licenses/LICENSE-2.0 
*   
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
* WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
* License for the specific language governing permissions and limitations 
* under the License.
* 
*/

/*
* Previously Copyright (c) 2001-2004 James House
*/
using System;
using System.IO;

using Common.Logging;

namespace Quartz.Job
{
	/// <summary> 
	/// Inspects a file and compares whether it's "last modified date" has changed
	/// since the last time it was inspected.  If the file has been updated, the
	/// job invokes a "call-back" method on an identified 
	/// <see cref="IFileScanListener" /> that can be found in the 
	/// <see cref="SchedulerContext" />.
	/// </summary>
	/// <author>James House</author>
	/// <seealso cref="IFileScanListener" />
	public class FileScanJob : IStatefulJob
	{
		public static string FILE_NAME = "FILE_NAME";
		public static string FILE_SCAN_LISTENER_NAME = "FILE_SCAN_LISTENER_NAME";
		private static string LAST_MODIFIED_TIME = "LAST_MODIFIED_TIME";
		private static readonly ILog Log = LogManager.GetLogger(typeof (FileScanJob));

		/// <summary>
		/// Called by the <see cref="IScheduler" /> when a <see cref="Trigger" />
		/// fires that is associated with the <see cref="IJob" />.
		/// <p>
		/// The implementation may wish to set a  result object on the
		/// JobExecutionContext before this method exits.  The result itself
		/// is meaningless to Quartz, but may be informative to
		/// <see cref="IJobListener" />s or
		/// <see cref="ITriggerListener" />s that are watching the job's
		/// execution.
		/// </p>
		/// </summary>
		/// <param name="context">The execution context.</param>
		/// <seealso cref="IJob">
		/// </seealso>
		public virtual void Execute(JobExecutionContext context)
		{
			JobDataMap data = context.MergedJobDataMap;
			SchedulerContext schedCtxt;
			try
			{
				schedCtxt = context.Scheduler.Context;
			}
			catch (SchedulerException e)
			{
				throw new JobExecutionException("Error obtaining scheduler context.", e, false);
			}

			string fileName = data.GetString(FILE_NAME);
			string listenerName = data.GetString(FILE_SCAN_LISTENER_NAME);

			if (fileName == null)
			{
				throw new JobExecutionException(string.Format("Required parameter '{0}' not found in JobDataMap", FILE_NAME));
			}
			if (listenerName == null)
			{
				throw new JobExecutionException(string.Format("Required parameter '{0}' not found in JobDataMap", FILE_SCAN_LISTENER_NAME));
			}

			IFileScanListener listener = (IFileScanListener) schedCtxt[listenerName];

			if (listener == null)
			{
				throw new JobExecutionException(string.Format("FileScanListener named '{0}' not found in SchedulerContext", listenerName));
			}

			DateTime lastDate = DateTime.MinValue;
			if (data.Contains(LAST_MODIFIED_TIME))
			{
				lastDate = data.GetDateTime(LAST_MODIFIED_TIME);
			}

			DateTime newDate = GetLastModifiedDate(fileName);

			if (newDate == DateTime.MinValue)
			{
				Log.Warn(string.Format("File '{0}' does not exist.", fileName));
				return;
			}

			if (lastDate != DateTime.MinValue && (newDate != lastDate))
			{
				// notify call back...
				Log.Info(string.Format("File '{0}' updated, notifying listener.", fileName));
				listener.FileUpdated(fileName);
			}
			else
			{
				Log.Debug(string.Format("File '{0}' unchanged.", fileName));
			}

			data.Put(LAST_MODIFIED_TIME, newDate);
		}

		/// <summary>
		/// Gets the last modified date.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		protected internal virtual DateTime GetLastModifiedDate(string fileName)
		{
			FileInfo file = new FileInfo(fileName);

			bool tmpBool;
			if (File.Exists(file.FullName))
			{
				tmpBool = true;
			}
			else
			{
				tmpBool = Directory.Exists(file.FullName);
			}
			if (!tmpBool)
			{
				return DateTime.MinValue;
			}
			else
			{
				return file.LastWriteTime;
			}
		}
	}
}