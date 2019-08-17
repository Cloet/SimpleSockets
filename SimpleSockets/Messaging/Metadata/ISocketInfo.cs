namespace SimpleSockets.Messaging.Metadata
{
	/// <summary>
	/// Used to return client info to the end user, restricting a lot of the properties used in
	/// ISocketState to which the user does not need access
	/// </summary>
	public interface ISocketInfo
	{

		/// <summary>
		/// The id of the state
		/// </summary>
		int Id { get; }
		
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
