using System;
using System.IO;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.StateObjectState
{
	public class FileHandlerState: StateObjectState
	{
		public FileHandlerState(IStateObject state) : base(state,null)
		{
		}

		public FileHandlerState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		//Checks if the file already exists and deletes when it has to.
		private void DeleteFile()
		{
			if (State.Flag == 1)
			{
				if (File.Exists(State.Header))
					File.Delete(State.Header);
			}
		}
		
		//Writes data to file
		private void Write(int receive)
		{

			//Check how much bytes have been received
			byte[] bytes = new byte[receive];

			//Adds bytes to read value
			State.AppendRead(receive);

			//Checks if it is the first write and if the file has to be deleted.
			DeleteFile();

			//If there is more data read then the server expects handle this accordingly.
			if (State.Read > State.MessageSize)
			{
				//How much has been read extra
				int extraRead = State.Read - State.MessageSize;

				//The bytes that have been read of a next message
				byte[] bytes2 = new byte[extraRead];
				Array.Copy(State.Buffer, receive - extraRead, bytes2, 0, extraRead);

				//The bytes that are still of the current message
				bytes = new byte[receive - extraRead];
				Array.Copy(State.Buffer, 0, bytes, 0, receive - extraRead);

				//Substract the bytes that have been read extra
				State.SubtractRead(extraRead);

				//Change the buffer value to the bytes of the next message
				State.ChangeBuffer(bytes2);

				//Change flag
				State.Flag = -3;

			}
			//If all bytes have been read without any extra
			else if(State.Read == State.MessageSize)
			{
				//Change flag
				State.Flag = -2;
				//Get all bytes
				Array.Copy(State.Buffer, 0, bytes, 0, bytes.Length);
			}
			else
			{
				//Get all bytes
				bytes = State.Buffer;
			}

			//Write the bytes to the corresponding file.
			using (BinaryWriter writer = new BinaryWriter(File.Open(State.Header, FileMode.Append)))
			{
				writer.Write(bytes, 0, bytes.Length);
				writer.Close();
			}

			//Increment the state flag
			if (State.Flag == 1)
				State.Flag++;

		}

		/// <inheritdoc />
		/// <summary>
		/// Handles the writing of a file.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{
			//Write
			Write(receive);

			//If the message has been read and there is are no extra bytes
			if (State.Flag == -2)
			{
				State.CurrentState = new FileHasBeenReceivedState(State,Client);
				State.CurrentState.Receive(State.Buffer.Length);
				State.Reset();
			}
			//If there is another message
			else if (State.Flag == -3)
			{
				//Set to FileHasBeenReceivedState and invoke FileReceived event
				State.CurrentState = new FileHasBeenReceivedState(State, Client);
				State.CurrentState.Receive(State.Buffer.Length);

				//Change state to InitState to handle the extra bytes for new message
				State.CurrentState = new InitialHandlerState(State, Client);

				//Resets the state and handle the new message
				State.Reset();
				State.CurrentState.Receive(State.Buffer.Length);
			}
		}
	}
}
