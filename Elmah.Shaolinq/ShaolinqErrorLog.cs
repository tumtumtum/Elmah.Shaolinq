﻿using System;
using System.Collections;
using System.Linq;
using Platform;
using Shaolinq;

namespace Elmah.Shaolinq
{
	public class ShaolinqErrorLog : ErrorLog
    {
		private const int MaxAppNameLength = 60;

		private readonly IElmahDataAccessModel dataModel;

		public ShaolinqErrorLog(IDictionary config)
		{
			var dataAccessModelTypeName = config.Find("dataAccessModelType", string.Empty);

			if (string.IsNullOrEmpty(dataAccessModelTypeName))
			{
				throw new ApplicationException("DataAccessModelType not specified");
			}

			var modelType = Type.GetType(dataAccessModelTypeName);

			if (modelType == null)
			{
				throw new ApplicationException(string.Format("Could not find type {0}", dataAccessModelTypeName));
			}

			if (!modelType.GetInterfaces().Contains(typeof(IElmahDataAccessModel)))
			{
				throw new ApplicationException("DataAccessModelType must implement IElmahDataAccessModel");
			}

			var dataAccessModelConfigSection = config.Find("dataAccessModelConfigSection", modelType.Name);

			var dataAccessModelConfiguration = ConfigurationBlock<DataAccessModelConfiguration>.Load(dataAccessModelConfigSection);

			this.dataModel = (IElmahDataAccessModel) DataAccessModel.BuildDataAccessModel(
				Type.GetType(dataAccessModelTypeName),
				dataAccessModelConfiguration);
			
			// Set the application name as this implementation provides per-application isolation over a single store.
			var appName = config.Find("applicationName", string.Empty);

			if (appName.Length > MaxAppNameLength)
			{
				throw new ApplicationException(String.Format(
					"Application name is too long. Maximum length allowed is {0} characters.",
					MaxAppNameLength.ToString("N0")));
			}

			ApplicationName = appName;
		}

		public override string Name
	    {
		    get { return "Shaolinq Error Log"; }
	    }

	    public override string Log(Error error)
	    {
		    if (error == null)
		    {
			    throw new ArgumentNullException("error");
		    }

		    var errorXml = ErrorXml.EncodeString(error);

		    using (var scope = TransactionScopeFactory.CreateReadCommitted())
		    {
			    var dbElmahError = dataModel.ElmahErrors.Create();

			    dbElmahError.Application = ApplicationName;
			    dbElmahError.Host = error.HostName;
			    dbElmahError.Type = error.Type;
			    dbElmahError.Source = error.Source;
			    dbElmahError.Message = error.Message;
			    dbElmahError.User = error.User;
			    dbElmahError.StatusCode = error.StatusCode;
			    dbElmahError.TimeUtc = error.Time;
			    dbElmahError.AllXml = errorXml;

			    scope.Complete();

			    return dbElmahError.Id.ToString();
		    }
	    }

	    public override ErrorLogEntry GetError(string id)
	    {
		    if (string.IsNullOrEmpty(id))
		    {
			    throw new ArgumentNullException("id");
		    }

		    Guid errorId;
		    if (!Guid.TryParse(id, out errorId))
		    {
			    throw new ArgumentException("Could not parse id as guid", "id");
		    }

			var dbElmahError = dataModel.ElmahErrors.SingleOrDefault(x => x.Application == ApplicationName && x.Id == errorId);

		    if (dbElmahError == null)
		    {
			    return null;
		    }

		    var error = ErrorXml.DecodeString(dbElmahError.AllXml);

			return new ErrorLogEntry(this, id, error);
	    }

	    public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
	    {
		    if (pageIndex < 0)
		    {
			    throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
		    }

		    if (pageSize < 0)
		    {
			    throw new ArgumentOutOfRangeException("pageSize", pageSize, null);
		    }

			var dbElmahErrors = dataModel.ElmahErrors
			    .Where(x => x.Application == ApplicationName)
			    .OrderByDescending(x => x.Sequence)
			    .Skip(pageIndex * pageSize)
			    .Take(pageSize)
				.ToList();

		    foreach (var dbElmahError in dbElmahErrors)
		    {
			    errorEntryList.Add(new ErrorLogEntry(this, dbElmahError.Id.ToString(), ErrorXml.DecodeString(dbElmahError.AllXml)));
		    }

			return dataModel.ElmahErrors.Count(x => x.Application == ApplicationName);
	    }
    }
}