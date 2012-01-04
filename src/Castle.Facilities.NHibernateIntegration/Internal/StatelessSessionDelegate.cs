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
namespace Castle.Facilities.NHibernateIntegration
{
	using System;
	using System.Data;
	using System.Linq.Expressions;
	using NHibernate;
	using NHibernate.Engine;

	/// <summary>
	/// Proxies an IStatelessSession so the user cannot close a stateless session
	/// which is controlled by a transaction, or, when this is not the case, 
	/// make sure to remove the session from the storage.
	/// <seealso cref="ISessionStore"/>
	/// <seealso cref="ISessionManager"/>
	/// </summary>
	[Serializable]
	public class StatelessSessionDelegate : MarshalByRefObject, IStatelessSession
	{
		private readonly IStatelessSession innerSession;
		private readonly ISessionStore sessionStore;
		private readonly bool canClose;
		private bool disposed;
		private object cookie;

		/// <summary>
		/// Initializes a new instance of the <see cref="StatelessSessionDelegate"/> class.
		/// </summary>
		/// <param name="canClose">if set to <c>true</c> [can close].</param>
		/// <param name="innerSession">The inner session.</param>
		/// <param name="sessionStore">The session store.</param>
		public StatelessSessionDelegate(bool canClose, IStatelessSession innerSession, ISessionStore sessionStore)
		{
			this.innerSession = innerSession;
			this.sessionStore = sessionStore;
			this.canClose = canClose;
		}

		/// <summary>
		/// Gets the inner session.
		/// </summary>
		/// <value>The inner session.</value>
		public IStatelessSession InnerSession
		{
			get { return innerSession; }
		}

		/// <summary>
		/// Gets or sets the session store cookie.
		/// </summary>
		/// <value>The session store cookie.</value>
		public object SessionStoreCookie
		{
			get { return cookie; }
			set { cookie = value; }
		}

		#region IStatelessSession delegation

		/// <summary>
		/// Returns the current ADO.NET connection associated with this instance.
		/// </summary>
		/// <remarks>
		/// If the session is using aggressive connection release (as in a
		/// CMT environment), it is the application's responsibility to
		/// close the connection returned by this call. Otherwise, the
		/// application should not close the connection.
		/// </remarks>
		public IDbConnection Connection
		{
			get { return innerSession.Connection; }
		}

		/// <summary>
		/// Is the <c>IStatelessSession</c> currently connected?
		/// </summary>
		public bool IsConnected
		{
			get
			{
				return innerSession.IsConnected;
			}
		}

		/// <summary>
		/// Is the <c>IStatelessSession</c> still open?
		/// </summary>
		public bool IsOpen
		{
			get
			{
				return innerSession.IsOpen;
			}
		}

		/// <summary>
		/// Get the current Hibernate transaction.
		/// </summary>
		public ITransaction Transaction
		{
			get { return innerSession.Transaction; }
		}

		/// <summary>
		/// Begin a NHibernate transaction.
		/// </summary>
		public ITransaction BeginTransaction()
		{
			return innerSession.BeginTransaction();
		}

		/// <summary>
		/// Begin a NHibernate transaction with the specified isolation level
		/// </summary>
		/// <param name="isolationLevel">The isolation level</param>
		/// <returns>
		/// A NHibernate transaction
		/// </returns>
		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			return innerSession.BeginTransaction(isolationLevel);
		}

		/// <summary>
		/// Close the stateless session and release the ADO.NET connection.
		/// </summary>
		public void Close()
		{
			innerSession.Close();
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity class,
		/// or a superclass of an entity class. 
		/// </summary>
		/// <typeparam name="T">A class, which is persistent, or has persistent subclasses</typeparam>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria<T>() where T : class
		{
			return innerSession.CreateCriteria<T>();
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity class,
		/// or a superclass of an entity class, with the given alias. 
		/// </summary>
		/// <typeparam name="T">A class, which is persistent, or has persistent subclasses</typeparam>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			return innerSession.CreateCriteria<T>(alias);
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity class,
		/// or a superclass of an entity class. 
		/// </summary>
		/// <param name="entityType">A class, which is persistent, or has persistent subclasses</param>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria(Type entityType)
		{
			return innerSession.CreateCriteria(entityType);
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity class,
		/// or a superclass of an entity class, with the given alias. 
		/// </summary>
		/// <param name="entityType">A class, which is persistent, or has persistent subclasses</param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria(Type entityType, string alias)
		{
			return innerSession.CreateCriteria(entityType, alias);
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity name.
		/// </summary>
		/// <param name="entityName">The entity name. </param>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria(string entityName)
		{
			return innerSession.CreateCriteria(entityName);
		}

		/// <summary>
		/// Create a new <see cref="T:NHibernate.ICriteria"/> instance, for the given entity name,
		/// with the given alias.  
		/// </summary>
		/// <param name="entityName">The entity name. </param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>
		/// The <see cref="T:NHibernate.ICriteria"/>. 
		/// </returns>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public ICriteria CreateCriteria(string entityName, string alias)
		{
			return innerSession.CreateCriteria(entityName, alias);
		}

		/// <summary>
		/// Create a new instance of <tt>Query</tt> for the given HQL query string.
		/// </summary>
		/// <remarks>
		/// Entities returned by the query are detached.
		/// </remarks>
		public IQuery CreateQuery(string queryString)
		{
			return innerSession.CreateQuery(queryString);
		}

		/// <summary>
		/// Create a new instance of <see cref="T:NHibernate.ISQLQuery"/> for the given SQL query string.
		/// Entities returned by the query are detached.
		/// </summary>
		/// <param name="queryString">a SQL query </param>
		/// <returns>
		/// The <see cref="T:NHibernate.ISQLQuery"/>
		/// </returns>
		public ISQLQuery CreateSQLQuery(string queryString)
		{
			return innerSession.CreateSQLQuery(queryString);
		}

		/// <summary>
		/// Delete a entity. 
		/// </summary>
		/// <param name="entity">a detached entity instance </param>
		public void Delete(object entity)
		{
			innerSession.Delete(entity);
		}

		/// <summary>
		/// Delete a entity. 
		/// </summary>
		/// <param name="entityName">The entityName for the entity to be deleted </param>
		/// <param name="entity">a detached entity instance </param>
		public void Delete(string entityName, object entity)
		{
			innerSession.Delete(entityName, entity);
		}

		/// <summary>
		/// Retrieve a entity. 
		/// </summary>
		/// <returns>
		/// a detached entity instance 
		/// </returns>
		public object Get(string entityName, object id)
		{
			return innerSession.Get(entityName, id);
		}

		/// <summary>
		/// Retrieve a entity.
		/// </summary>
		/// <returns>
		/// a detached entity instance
		/// </returns>
		public T Get<T>(object id)
		{
			return innerSession.Get<T>(id);
		}

		/// <summary>
		/// Retrieve a entity, obtaining the specified lock mode. 
		/// </summary>
		/// <returns>
		/// a detached entity instance 
		/// </returns>
		public object Get(string entityName, object id, LockMode lockMode)
		{
			return innerSession.Get(entityName, id, lockMode);
		}

		/// <summary>
		/// Retrieve a entity, obtaining the specified lock mode. 
		/// </summary>
		/// <returns>
		/// a detached entity instance 
		/// </returns>
		public T Get<T>(object id, LockMode lockMode)
		{
			return innerSession.Get<T>(id, lockMode);
		}

		/// <summary>
		/// Obtain an instance of <see cref="T:NHibernate.IQuery"/> for a named query string defined in
		/// the mapping file.
		/// </summary>
		/// <remarks>
		/// The query can be either in <c>HQL</c> or <c>SQL</c> format.
		/// Entities returned by the query are detached.
		/// </remarks>
		public IQuery GetNamedQuery(string queryName)
		{
			return innerSession.GetNamedQuery(queryName);
		}

		/// <summary>
		/// Gets the stateless session implementation.
		/// </summary>
		/// <remarks>
		/// This method is provided in order to get the <b>NHibernate</b> implementation of the session from wrapper implementations.
		///             Implementors of the <seealso cref="T:NHibernate.IStatelessSession"/> interface should return the NHibernate implementation of this method.
		/// </remarks>
		/// <returns>
		/// An NHibernate implementation of the <see cref="T:NHibernate.Engine.ISessionImplementor"/> interface
		/// </returns>
		public ISessionImplementor GetSessionImplementation()
		{
			return innerSession.GetSessionImplementation();
		}

		/// <summary>
		/// Insert a entity.
		/// </summary>
		/// <param name="entity">A new transient instance </param>
		/// <returns>
		/// the identifier of the instance 
		/// </returns>
		public object Insert(object entity)
		{
			return innerSession.Insert(entity);
		}

		/// <summary>
		/// Insert a row. 
		/// </summary>
		/// <param name="entityName">The entityName for the entity to be inserted </param>
		/// <param name="entity">a new transient instance </param>
		/// <returns>
		/// the identifier of the instance 
		/// </returns>
		public object Insert(string entityName, object entity)
		{
			return innerSession.Insert(entityName, entity);
		}

		/// <summary>
		/// Creates a new <c>IQueryOver&lt;T&gt;</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam>
		/// <returns>
		/// An ICriteria&lt;T&gt; object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			return innerSession.QueryOver<T>();
		}

		/// <summary>
		/// Creates a new <c>IQueryOver&lt;T&gt;</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam>
		/// <returns>
		/// An ICriteria&lt;T&gt; object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			return innerSession.QueryOver(alias);
		}

		/// <summary>
		/// Refresh the entity instance state from the database. 
		/// </summary>
		/// <param name="entity">The entity to be refreshed. </param>
		public void Refresh(object entity)
		{
			innerSession.Refresh(entity);
		}

		/// <summary>
		/// Refresh the entity instance state from the database. 
		/// </summary>
		/// <param name="entityName">The entityName for the entity to be refreshed. </param>
		/// <param name="entity">The entity to be refreshed.</param>
		public void Refresh(string entityName, object entity)
		{
			innerSession.Refresh(entityName, entity);
		}

		/// <summary>
		/// Refresh the entity instance state from the database. 
		/// </summary>
		/// <param name="entity">The entity to be refreshed. </param>
		/// <param name="lockMode">The LockMode to be applied.</param>
		public void Refresh(object entity, LockMode lockMode)
		{
			innerSession.Refresh(entity, lockMode);
		}

		/// <summary>
		/// Refresh the entity instance state from the database. 
		/// </summary>
		/// <param name="entityName">The entityName for the entity to be refreshed. </param>
		/// <param name="entity">The entity to be refreshed. </param>
		/// <param name="lockMode">The LockMode to be applied. </param>
		public void Refresh(string entityName, object entity, LockMode lockMode)
		{
			innerSession.Refresh(entityName, entity, lockMode);
		}

		/// <summary>
		/// Sets the batch size of the session
		/// </summary>
		/// <param name="batchSize">The batch size.</param>
		/// <returns>
		/// The same instance of the session for methods chain.
		/// </returns>
		public IStatelessSession SetBatchSize(int batchSize)
		{
			return innerSession.SetBatchSize(batchSize);
		}

		/// <summary>
		/// Update a entity.
		/// </summary>
		/// <param name="entity">a detached entity instance </param>
		public void Update(object entity)
		{
			innerSession.Update(entity);
		}

		/// <summary>
		/// Update a entity.
		/// </summary>
		/// <param name="entityName">The entityName for the entity to be updated </param>
		/// <param name="entity">a detached entity instance </param>
		public void Update(string entityName, object entity)
		{
			innerSession.Update(entityName, entity);
		}

		#endregion

		#region IDisposable delegation

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			DoClose(false);
		}

		#endregion

		/// <summary>
		/// Does the close.
		/// </summary>
		/// <param name="closing">if set to <c>true</c> [closing].</param>
		/// <returns></returns>
		protected IDbConnection DoClose(bool closing)
		{
			if (disposed) return null;

			if (canClose)
			{
				return InternalClose(closing);
			}
			else
			{
				innerSession.Dispose();
			}

			return null;
		}

		internal IDbConnection InternalClose(bool closing)
		{
			IDbConnection conn = null;

			UnregisterFromStore();

			if (closing)
			{
				conn = innerSession.Connection;
				innerSession.Close();
			}

			innerSession.Dispose();

			disposed = true;

			return conn;
		}

		internal void UnregisterFromStore()
		{
			IsUnregistred = true;

			sessionStore.Remove(this);
		}

		internal bool IsUnregistred { get; set; }

		/// <summary>
		/// Returns <see langword="true"/> if the supplied stateless sessions are equal, <see langword="false"/> otherwise.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool AreEqual(IStatelessSession left, IStatelessSession right)
		{
			var ssdLeft = left as StatelessSessionDelegate;
			var ssdRight = right as StatelessSessionDelegate;

			if (ssdLeft != null && ssdRight != null)
			{
				return Object.ReferenceEquals(ssdLeft.innerSession, ssdRight.innerSession);
			}
			else
			{
				throw new NotSupportedException("AreEqual: left is " +
												left.GetType().Name + " and right is " + right.GetType().Name);
			}
		}
	}
}