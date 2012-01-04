#region License
//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  
#endregion
namespace Castle.Facilities.NHibernateIntegration.Internal
{
	using System;
	using System.Collections;
	using System.Transactions;
	using NHibernate;
	using NHibernate.Engine;
	using NHibernate.Engine.Transaction;
	using NHibernate.Impl;
	using NHibernate.Transaction;

	/// <summary>
	/// 
	/// </summary>
	public class CastleTransactionsFriendlyTransactionFactory : ITransactionFactory
	{
		private static readonly IInternalLogger logger = LoggerProvider.LoggerFor(typeof(ITransactionFactory));

		private readonly AdoNetTransactionFactory adoNetTransactionFactory = new AdoNetTransactionFactory();

		public void Configure(IDictionary props)
		{
		}

		public ITransaction CreateTransaction(ISessionImplementor session)
		{
			return new NHTransaction(session);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		public void EnlistInDistributedTransactionIfNeeded(ISessionImplementor session)
		{
			if (session.TransactionContext != null)
				return;
			
			if (System.Transactions.Transaction.Current == null)
				return;
			
			var transactionContext = new DistributedTransactionContext(session, System.Transactions.Transaction.Current);
			session.TransactionContext = transactionContext;

			logger.DebugFormat("enlisted into DTC transaction: {0}", transactionContext.AmbientTransation.IsolationLevel);

			//session.AfterTransactionBegin(null);

			if (!session.ConnectionManager.Transaction.IsActive)
			{
				transactionContext.ShouldCloseSessionOnDistributedTransactionCompleted = true;
				session.ConnectionManager.Transaction.Begin(transactionContext.AmbientTransation.IsolationLevel.AsDataIsolationLevel());
			}
			
			transactionContext.AmbientTransation.EnlistVolatile(transactionContext, EnlistmentOptions.EnlistDuringPrepareRequired);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		/// <returns></returns>
		public bool IsInDistributedActiveTransaction(ISessionImplementor session)
		{
			var distributedTransactionContext = ((DistributedTransactionContext) session.TransactionContext);

			return distributedTransactionContext != null && distributedTransactionContext.IsInActiveTransaction;
		}


		public void ExecuteWorkInIsolation(ISessionImplementor session, IIsolatedWork work, bool transacted)
		{
			using(var tx = new TransactionScope(TransactionScopeOption.Suppress))
			{
				// instead of duplicating the logic, we suppress the DTC transaction and create
				// our own transaction instead
				adoNetTransactionFactory.ExecuteWorkInIsolation(session, work, transacted);
				tx.Complete();
			}
		}

		public class DistributedTransactionContext : ITransactionContext, IEnlistmentNotification
		{
			public System.Transactions.Transaction AmbientTransation { get; set; }
			public bool ShouldCloseSessionOnDistributedTransactionCompleted { get; set; }
			private readonly ISessionImplementor session;
			public bool IsInActiveTransaction;
			private readonly ITransaction nhtx;

			public DistributedTransactionContext(ISessionImplementor session, System.Transactions.Transaction transaction)
			{
				nhtx =  session.ConnectionManager.Transaction;
				this.session = session;
				AmbientTransation = transaction;
				IsInActiveTransaction = true;
			}

			#region IEnlistmentNotification Members

			void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					try
					{
						//using (var tx = new TransactionScope(AmbientTransation))
						{
							session.BeforeTransactionCompletion(null);
							if (session.FlushMode != FlushMode.Never && session.ConnectionManager.IsConnected)
							{
								using (session.ConnectionManager.FlushingFromDtcTransaction)
								{
									logger.Debug(string.Format("[session-id={0}] Flushing from Dtc Transaction", session.SessionId));
									session.Flush();
								}
							}
							logger.Debug("prepared for DTC transaction");

							//tx.Complete();
						}
						preparingEnlistment.Prepared();
					}
					catch (Exception exception)
					{
						logger.Error("DTC transaction prepre phase failed", exception);
						preparingEnlistment.ForceRollback(exception);
					}
				}
			}

			void IEnlistmentNotification.Commit(Enlistment enlistment)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					logger.Debug("committing DTC transaction");
					
					nhtx.Commit();
					End(true);
					
					enlistment.Done();

					IsInActiveTransaction = false;
				}
			}

			void IEnlistmentNotification.Rollback(Enlistment enlistment)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					session.AfterTransactionCompletion(false, null);
					logger.Debug("rolled back DTC transaction");

					nhtx.Rollback();
					End(false);
					
					enlistment.Done();
					
					IsInActiveTransaction = false;
				}
			}

			void IEnlistmentNotification.InDoubt(Enlistment enlistment)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					session.AfterTransactionCompletion(false, null);
					logger.Debug("DTC transaction is in doubt");
					
					End(false);
					
					enlistment.Done();
					IsInActiveTransaction = false;
				}
			}

			void End(bool wasSuccessful)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					((DistributedTransactionContext)session.TransactionContext).IsInActiveTransaction = false;
							
					session.AfterTransactionCompletion(wasSuccessful, null);

					if (ShouldCloseSessionOnDistributedTransactionCompleted)
					{
						session.CloseSessionFromDistributedTransaction();
					}

					session.TransactionContext = null;
				}
			}

			#endregion

			public void Dispose()
			{
				//if (AmbientTransation != null)
				//    AmbientTransation.Dispose();
			}
		}
	}
}