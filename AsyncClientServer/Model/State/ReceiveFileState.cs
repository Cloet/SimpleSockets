using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.State
{
	public class ReceiveFileState : ClientState
	{
		public ReceiveFileState(AsyncClient client) : base(client)
		{

		}

		public void DeleteFile(IStateObject state)
		{
			if (state.Flag == 1)
			{
				if (File.Exists(state.Header))
					File.Delete(state.Header);
			}
		}

		public void FirstWrite(IStateObject state, int receive)
		{

			//Creates a new byte array and copy data to it
			byte[] bytes = new byte[receive - (8 + state.HeaderSize)];
			Array.Copy(state.Buffer, 8 + state.HeaderSize, bytes, 0, receive - (8 + state.HeaderSize));

			//Get data for file and write it
			using (BinaryWriter writer = new BinaryWriter(File.Open(state.Header, FileMode.Append)))
			{			
				//Write bytes to file and close writer
				writer.Write(bytes);
				writer.Close();
			}


			//Add bytes that have been read to int and increment state flag
			state.AppendRead(bytes.Length);
			state.Flag++;


		}

		public void NormalWrite(IStateObject state, int receive)
		{

			byte[] bytes = CheckMessage(state, receive);

			
			using (BinaryWriter writer = new BinaryWriter(File.Open(state.Header, FileMode.Append)))
			{	
				writer.Write(bytes);
				writer.Close();
			}

		}

		public byte[] CheckMessage(IStateObject state, int receive)
		{
			byte[] bytes = new byte[receive];
			state.AppendRead(bytes.Length);

			if (state.Read > state.MessageSize)
			{
				Array.Copy(state.Buffer, 0, bytes, 0, receive - (state.Read - state.MessageSize));
				Client.ChangeState(new InitReceiveState(Client));
				state.Flag = -2;
			}
			else
			{
				Array.Copy(state.Buffer, 0, bytes,0, receive);
			}

			return bytes;

		}

		public void FileWriter(IStateObject state, int receive)
		{

			if (state.Flag == 1)
			{
				FirstWrite(state, receive);

			}
			else
			{
				NormalWrite(state, receive);
			}



		}


		public override void Receive(IStateObject state, int receive)
		{
			DeleteFile(state);
			FileWriter(state, receive);
		}

	}
}
