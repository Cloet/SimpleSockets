using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SimpleSockets
{
	public class SslContext
	{
		public TlsProtocol TlsProtocol { get; set; } = TlsProtocol.Tls12;

		public X509Certificate2 Certificate {get; set;}

		public bool AcceptInvalidCertificates { get; set; } = true;

		public bool MutualAuth { get; set; } = false;

		public SslContext(X509Certificate2 certificate) {
			Certificate = certificate;
		}

		public SslContext(byte[] certificateData, string certificatePass) {
			Certificate = new X509Certificate2(certificateData, certificatePass);
		}

		public SslContext(string certificateLocation, string certificatePass) {
			Certificate = new X509Certificate2(File.ReadAllBytes(certificateLocation), certificatePass);
		}

	}
}
