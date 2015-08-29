using System;
using System.Diagnostics.Contracts;


namespace CuratorClient
{
	/**
 * Standard ensemble provider that wraps a fixed connection string
 */
	public class FixedEnsembleProvider: IEnsembleProvider
	{
		private readonly String connectionString;

		/**
     * The connection string to use
     *
     * @param connectionString connection string
     */
		public FixedEnsembleProvider(String connectionString)
		{
			//Contract.Requires<ArgumentNullException> (string.IsNullOrEmpty (connectionString), "connectionString cannot be null");
			this.connectionString = connectionString;
		}


		public void Start()
		{
			// NOP
		}


		public void Dispose() 
		{
			// NOP
		}


		public String GetConnectionString()
		{
			return connectionString;
		}
	}

}

