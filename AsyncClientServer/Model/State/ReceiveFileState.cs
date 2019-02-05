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
		public ReceiveFileState(IAsyncClient client) : base(client)
		{

		}

		public ReceiveFileState(IAsyncSocketListener server) : base(server)
		{
		}

		public void DeleteFile(IStateObject state)
		{
			//If it's the first loop delete file when it exists.
			if (state.Flag == 1)
			{
				if (File.Exists(state.Header))
					File.Delete(state.Header);
			}
		}

		public void FirstWrite(IStateObject state, int receive)
		{

			//Creates a new byte array and copy data to it
			byte[] bytes = new byte[state.Buffer.Length - (8 + state.HeaderSize)];
			Array.Copy(state.Buffer, 8 + state.HeaderSize, bytes, 0, state.Buffer.Length - (8 + state.HeaderSize));

			//Get data for file and write it
			using (BinaryWriter writer = new BinaryWriter(File.Open(state.Header, FileMode.Append)))
			{			
				//Write bytes to file and close writer
				writer.Write(bytes);
				writer.Close();
			}

			//Add bytes that have been read to state
			state.AppendRead(bytes.Length);

			//Reset the buffer length
			if (state.Buffer.Length < state.BufferSize)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			//Increment flag
			state.Flag++;


		}

		public void NormalWrite(IStateObject state, int receive)
		{
			//Get bytes
			byte[] bytes = CheckMessage(state, receive);
		
			//Write bytes to file
			using (BinaryWriter writer = new BinaryWriter(File.Open(state.Header, FileMode.Append)))
			{	
				writer.Write(bytes);
				writer.Close();
			}

			//If full message has been received check for another message right after it
			if (state.Flag == -2)
			{
				byte[] bytes2 = new byte[state.BufferSize - bytes.Length];
				Array.Copy(state.Buffer, bytes.Length, bytes2, 0, state.Read - state.MessageSize);
				state.SubtractRead(receive - bytes.Length);
				state.ChangeBuffer(bytes2);
			}

		}

		public byte[] CheckMessage(IStateObject state, int receive)
		{
			//Init a new byte array
			byte[] bytes = new byte[receive];

			//Append read
			state.AppendRead(receive);

			//Check if too much has been read.
			if (state.Read > state.MessageSize)
			{
				//Get bytes of the remaining file and store in new array.
				bytes = new byte[receive - (state.Read - state.MessageSize)];
				Array.Copy(state.Buffer, 0, bytes, 0, receive - (state.Read - state.MessageSize));

				//Change the state to "file has been received. and change the flag
				if (Client != null)
				{
					Client.ChangeState(new FileReceivedState(Client));
				}

				if (Server != null)
				{
					Server.CurrentState = new FileReceivedState(Server);
				}

				state.Flag = -2;

			}else if (state.Read == state.MessageSize)
			{
				//File has been received
				Array.Copy(state.Buffer, 0, bytes, 0, receive);

				if (Client != null)
				{
					Client.ChangeState(new FileReceivedState(Client));
				}

				if (Server != null)
				{
					Server.CurrentState = new FileReceivedState(Server);
				}

			}
			else
			{
				//The full message has not yet been received
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
				if (state.Flag == -2)
				{
					if (Client != null)
					{
						Client.CState.Receive(state, receive);
					}

					if (Server != null)
					{
						Server.CurrentState.Receive(state, receive);
					}

				}
			}



		}


		public override void Receive(IStateObject state, int receive)
		{
			DeleteFile(state);
			FileWriter(state, receive);
		}

	}
}
