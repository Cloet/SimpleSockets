namespace SimpleSockets.Messaging.Metadata
{
	/// <summary>
	/// Used to return client info to the end user, restricting a lot of the properties used in
	/// ISocketState to which the user does not need access
	/// </summary>
	public interface IClientInfo
	{

		/// <summary>
		/// The id of the state
		/// </summary>
		int Id { get; }
		
		/// <summary>
		/// The name of the client
		/// </summary>
		string ClientName { get; }

		/// <summary>
		/// Unique identifier.
		/// </summary>
		string Guid { get; }

		/// <summary>
		/// The operating system the client is running on.
		/// </summary>
		string OsVersion { get; }

		/// <summary>
		/// The UserDomainName of the client.
		/// </summary>
		string UserDomainName { get; }

		/// <summary>
		/// The remote IPv4 of the client.
		/// </summary>
		string RemoteIPv4 { get; }

		/// <summary>
		/// The remote IPv6 of the client.
		/// </summary>
		string RemoteIPv6 { get; }

		/// <summary>
		/// The local IPv4 of the client.
		/// </summary>
		string LocalIPv4 { get; }

		/// <summary>
		/// The local IPv6 of the client.
		/// </summary>
		string LocalIPv6 { get; }

	}
}
