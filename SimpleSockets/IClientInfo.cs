using System;

namespace SimpleSockets {

    public interface IClientInfo {

        int Id { get; }

        string ClientName { get; }

        Guid Guid { get; }

        string OsVersion { get; }

        string UserDomainName { get; }

		string IPv4 { get; }

		string IPv6 { get; }
    }

}