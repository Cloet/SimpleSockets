namespace SimpleSockets {

    public interface IClientInfo {
        int Id { get; }

        string ClientName { get; set; }

        string Guid { get; set; }

        string OsVersion { get; set; }

        string UserDomainName { get; set; }

		string IPv4 { get; set; }

		string IPv6 { get; set; }
    }

}