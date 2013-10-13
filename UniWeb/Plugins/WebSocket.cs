using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace HTTP
{	
	public class WebSocket
	{
		public int niceness = 100;
		const byte FINALBIT = 0x80;
		const byte RESERVEDBIT1 = 0x40;
		const byte RESERVEDBIT2 = 0x20;
		const byte RESERVEDBIT3 = 0x10;
		const byte OP_CODE_MASK = 0xF;
		const byte MASKBIT = 0x80;
		const byte PAYLOAD_LENGTH_MASK = 0x7F;
		const int MASKING_KEY_WIDTH_IN_BYTES = 4;
		const int MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD = 125;
		const int PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD = 126;
		const int PAYLOAD_WITH_EIGHT_BYTE_EXTENDED_FIELD = 127;
		
		public delegate void OnTextMessageHandler (string message);

		public delegate void OnBinaryMessageHandler (byte[] message);
		
		public event OnTextMessageHandler OnTextMessageRecv;
		public event OnBinaryMessageHandler OnBinaryMessageRecv;
		
		[Flags]
		public enum OpCode
		{ 
			OpCodeContinuation = 0x0,
			OpCodeText =  0x1,
			OpCodeBinary = 0x2,
			OpCodeClose = 0x8,
			OpCodePing = 0x9,
			OpCodePong = 0xA
		}
		
		enum ParseFrameResult
		{
			FrameIncomplete,
			FrameOK,
			FrameError
		}
		
		public enum CloseEventCode
		{
			CloseEventCodeNotSpecified = -1,
			CloseEventCodeNormalClosure = 1000,
			CloseEventCodeGoingAway = 1001,
			CloseEventCodeProtocolError = 1002,
			CloseEventCodeUnsupportedData = 1003,
			CloseEventCodeFrameTooLarge = 1004,
			CloseEventCodeNoStatusRcvd = 1005,
			CloseEventCodeAbnormalClosure = 1006,
			CloseEventCodeInvalidUTF8 = 1007,
			CloseEventCodeMinimumUserDefined = 3000,
			CloseEventCodeMaximumUserDefined = 4999
		};

		class FrameData
		{
			
			public OpCode opCode;
			public bool final;
			public bool reserved1;
			public bool reserved2;
			public bool reserved3;
			public bool masked;
			public int payload;
			public int payloadLength;
			public int end;
		}
		
		class SubArray : IEnumerable<byte>
		{ 
			
			List<byte> array;
			int offset;
			int length;
			
			public SubArray (List<byte> array, int offset, int length)
			{
				this.array = array;
				this.offset = offset;
				this.length = length;
			}
		
			IEnumerator IEnumerable.GetEnumerator ()
			{
				return (IEnumerator<byte>)GetEnumerator ();
			}
		
			public IEnumerator<byte> GetEnumerator ()
			{
				return new SubArrayEnum (array, offset, length);
			}
		}
		
		class SubArrayEnum : IEnumerator<byte>
		{
			List<byte> array;
			int offset;
			int length;
			int position = -1;
			
			public SubArrayEnum (List<byte> array, int offset, int length)
			{
				this.array = array;
				this.offset = offset;
				this.length = length;
			}
			
			public bool MoveNext ()
			{ 
				position++;
				return (position < length);
			}
			
			public void Reset ()
			{ 
				position = -1;
			}
			
			object IEnumerator.Current { 
				get { 
					return Current;
				}
			}
			
			public byte Current { 
				get { 
					try {
						return array [offset + position];
					} catch (IndexOutOfRangeException) {
						throw new InvalidOperationException ();
					}	
				}
			}
			
			public void Dispose ()
			{
			}
		}
		
		public struct OutgoingMessage
		{
			public readonly WebSocket.OpCode 	opCode;
			public readonly byte[] 				data;
			
			public OutgoingMessage (WebSocket.OpCode opCode, byte[] data)
			{
				this.opCode = opCode;
				this.data = data;
			}
		}
		
		public bool isDone = false;
		public bool connected = false;
		Thread outgoingWorkerThread;
		Thread incomingWorkerThread;
		HTTP.ActiveConnection connection = null;
		List<string> incomingText = new List<string> ();
		List<byte[]> incomingBinary = new List<byte[]> ();
		List<OutgoingMessage> outgoing = new List<OutgoingMessage> ();
		bool hasContinuousFrame = false;
		OpCode continuousFrameOpCode;
		List<byte> continuousFrameData = new List<byte> ();
		bool receivedClosingHandshake = false;
		CloseEventCode closeEventCode;
		string closeEventReason = "";
		bool closing = false;
		
		void OnTextMessage (string msg)
		{
			lock (incomingText) {
				incomingText.Add (msg);	
			}
		}
		
		void OnBinaryMessage (byte[] msg)
		{
			lock (incomingBinary) {
				incomingBinary.Add (msg);
			}
		}
		
		public IEnumerator Dispatcher ()
		{
			while (true) {
				yield return null;
				if (OnTextMessageRecv != null) {
					lock (incomingText) {
						if (incomingText.Count > 0) {
							foreach (var i in incomingText) {
								OnTextMessageRecv (i);
							}
							incomingText.Clear ();
						}
					}
				}
				if (OnBinaryMessageRecv != null) {
					lock (incomingBinary) {
						if (incomingBinary.Count > 0) {
							foreach (var i in incomingBinary) {
								OnBinaryMessageRecv (i);
							}
							incomingBinary.Clear ();
						}
					}
				}
			}
		}
		
		public void Connect (string uri)
		{
			Connect (new System.Uri (uri));	
		}
		
		public Coroutine Wait() {
			return SimpleWWW.Instance.StartCoroutine(_Wait());
		}
		
		IEnumerator _Wait() {
			while(!isDone) yield return null;
		}
		
		public void Connect (Uri uri)
		{
			isDone = false;
			connected = false;	
			//var host = uri.Host + (uri.Port == 80 ? "" : ":" + uri.Port.ToString ());
			var req = new Request ("GET", uri.ToString ());
			req.headers.Set ("Upgrade", "websocket");
			req.headers.Set ("Connection", "Upgrade");
			var key = WebSocketKey ();
			req.headers.Set ("Sec-WebSocket-Key", key);
			req.headers.Add ("Sec-WebSocket-Protocol", "chat, superchat");
			req.headers.Set ("Sec-WebSocket-Version", "13");
			req.headers.Set ("Origin", "null");
			req.acceptGzip = false;
			req.Send ((Response obj) => {
				if (obj.headers.Get ("Upgrade").ToLower () == "websocket" && obj.headers.Get ("Connection").ToLower () == "upgrade") {
					var receivedKey = obj.headers.Get ("Sec-Websocket-Accept").ToLower ();
					var sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
					sha.ComputeHash (System.Text.ASCIIEncoding.ASCII.GetBytes (key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
					var computedKey = System.Convert.ToBase64String (sha.Hash).ToLower ();
					if (computedKey == receivedKey) {
						//good to go	
						connected = true;
						connection = req.upgradedConnection;
						outgoingWorkerThread = new Thread (OutgoingWorker);
						outgoingWorkerThread.Start ();
						incomingWorkerThread = new Thread (IncomingWorker);
						incomingWorkerThread.Start ();
						SimpleWWW.Instance.StartCoroutine (Dispatcher ());			
						SimpleWWW.Instance.OnQuit(() => {
							Close(CloseEventCode.CloseEventCodeNotSpecified, "Quit");
							req.upgradedConnection.client.Close();
						});
					} else {
						//invalid
						connected = false;
					}	
				}
				isDone = true;
			});
			
		}
		
		public void Send (string data)
		{	
			outgoing.Add (new OutgoingMessage (OpCode.OpCodeText, System.Text.ASCIIEncoding.ASCII.GetBytes (data)));
		}
		
		public void Send (byte[] data)
		{ 
			outgoing.Add (new OutgoingMessage (OpCode.OpCodeBinary, data));
		}
		
		public void Close (CloseEventCode code, string reason)
		{ 
			StartClosingHandshake (code, reason);
		}
		
		void OutgoingWorker ()
		{
			while (true) {
				Thread.Sleep (niceness);
				lock (outgoing) {
					while (connection.stream.CanWrite && outgoing.Count > 0) {
						var msg = outgoing [0];
						var netform = BuildFrame (msg.opCode, msg.data);
						connection.stream.Write (netform, 0, netform.Length);
						outgoing.RemoveAt (0);
					}
				}
			}
		}
		
		void IncomingWorker ()
		{
			List<byte> buffer = new List<byte> ();
			while (connection.stream.CanRead) {
				int c = connection.stream.ReadByte ();
				if (c == -1) {
					throw new HTTPException ("Unterminated Stream");
				}
				//Do something with byte to build a message
				//when message built, add to incoming
				buffer.Add ((byte)c);
				ProcessBuffer (buffer);
			}
		}
		
		bool ProcessBuffer (List<byte> buffer)
		{
			return ProcessFrame (buffer);
		}
		
		bool ProcessFrame (List<byte> buffer)
		{
			FrameData frame;
			if (ParseFrame (buffer, out frame) != ParseFrameResult.FrameOK)
				return false;
			
			switch (frame.opCode) {
			case OpCode.OpCodeContinuation:
				// An unexpected continuation frame is received without any leading frame.
				if (!hasContinuousFrame) {
					Debug.LogWarning ("Received unexpected continuation frame.");
					return false;
				}
				continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
				RemoveProcessed (buffer, frame.end);
				if (frame.final) {
					continuousFrameData = new List<byte> ();
					hasContinuousFrame = false;
					if (continuousFrameOpCode == OpCode.OpCodeText) {
						var message = "";
						if (continuousFrameData.Count > 0) {
							message = System.Text.UTF8Encoding.UTF8.GetString (continuousFrameData.ToArray ());
						}
						OnTextMessage (message);
					} else if (continuousFrameOpCode == OpCode.OpCodeBinary) {
						OnBinaryMessage (continuousFrameData.ToArray ());
					}
				}
				break;
			case OpCode.OpCodeText:
				if (frame.final) {
					String message = "";
					if (frame.payloadLength > 0) {
						var payload = new byte[frame.payloadLength];
						buffer.CopyTo (frame.payload, payload, 0, frame.payloadLength);
						message = System.Text.UTF8Encoding.UTF8.GetString (payload);
					} 
					OnTextMessage (message);
					RemoveProcessed (buffer, frame.end);
				} else {
					hasContinuousFrame = true;
					continuousFrameOpCode = OpCode.OpCodeText;
					continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
					RemoveProcessed (buffer, frame.end);
				}
				break;
			case OpCode.OpCodeBinary:
				if (frame.final) {
					byte[] payload = new byte[frame.payloadLength];
					buffer.CopyTo (frame.payload, payload, 0, frame.payloadLength);
					OnBinaryMessage (payload);
					RemoveProcessed (buffer, frame.end);
				} else {
					hasContinuousFrame = true;
					continuousFrameOpCode = OpCode.OpCodeBinary;
					continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
					RemoveProcessed (buffer, frame.end);
				}
				break;
			case OpCode.OpCodeClose:
				if (frame.payloadLength >= 2) {
					byte highByte = buffer [frame.payload + 0];
					byte lowByte = buffer [frame.payload + 1];
					closeEventCode = (CloseEventCode)(highByte << 8 | lowByte);
				} else {
					closeEventCode = CloseEventCode.CloseEventCodeNoStatusRcvd;
				}
				if (frame.payloadLength >= 3) {	
					byte[] payload = new byte[frame.payloadLength - 2];
					buffer.CopyTo (2, payload, 0, frame.payloadLength - 2); 
					closeEventReason = System.Text.UTF8Encoding.UTF8.GetString (payload);
				} else {
					closeEventReason = "";
				}
				RemoveProcessed (buffer, frame.end);
				receivedClosingHandshake = true;
				StartClosingHandshake (closeEventCode, closeEventReason);
				break;
			case OpCode.OpCodePing:

				var reply = new byte[frame.payloadLength];
				buffer.CopyTo (frame.payload, reply, 0, frame.payloadLength);
				RemoveProcessed (buffer, frame.end);
				
				// reply with Pong!
				lock (outgoing) {
					outgoing.Add (new OutgoingMessage (OpCode.OpCodePong, reply)); 
				}
				
				break;
			case OpCode.OpCodePong:
				// do nothing with a pong, just remove processed bytes
				RemoveProcessed (buffer, frame.end);
				break;
			default:
				Debug.LogError ("SHOULD NOT REACH HERE");
				break;
			}
			;
			
			return buffer.Count != 0;
		}
		
		ParseFrameResult ParseFrame (List<byte> buffer, out FrameData frame)
		{
			
			frame = null;
			
			if (buffer.Count < 2) {
				return ParseFrameResult.FrameIncomplete;
			}
		
			int p = 0;
			
			byte firstByte = buffer [p++];
			byte secondByte = buffer [p++];
		
			bool final = (firstByte & FINALBIT) > 0;
			bool reserved1 = (firstByte & RESERVEDBIT1) > 0;
			bool reserved2 = (firstByte & RESERVEDBIT2) > 0;
			bool reserved3 = (firstByte & RESERVEDBIT3) > 0;
			OpCode opCode = (OpCode)(firstByte & OP_CODE_MASK);
		
			bool masked = (secondByte & MASKBIT) > 0;
			long payloadLength64 = (secondByte & PAYLOAD_LENGTH_MASK);
			if (payloadLength64 > MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD) {
				int extendedPayloadLengthSize;
				if (payloadLength64 == PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD)
					extendedPayloadLengthSize = 2;
				else {
					extendedPayloadLengthSize = 8;
				}
				if (buffer.Count - p < extendedPayloadLengthSize) {
					return ParseFrameResult.FrameIncomplete;
				}
				payloadLength64 = 0;
				for (int i = 0; i < extendedPayloadLengthSize; ++i) {
					payloadLength64 <<= 8;
					payloadLength64 |= buffer [p++];
				}
			}
		
			const long maxPayloadLength = 0x7FFFFFFFFFFFFFFF;
			
			int maskingKeyLength = masked ? MASKING_KEY_WIDTH_IN_BYTES : 0;
			if (payloadLength64 > maxPayloadLength || payloadLength64 + maskingKeyLength > int.MaxValue) {
				Debug.LogError (string.Format ("WebSocket frame length too large: {0} bytes", payloadLength64));
				return ParseFrameResult.FrameError;
			}
			
			int payloadLength = (int)payloadLength64;
		
			if ((buffer.Count - p) < maskingKeyLength + payloadLength) {
				return ParseFrameResult.FrameIncomplete;
			}
		
			if (masked) {
				int maskingKey = p;
				int payload = p + MASKING_KEY_WIDTH_IN_BYTES;
				for (int i = 0; i < payloadLength; ++i) {
					buffer [payload + i] ^= buffer [maskingKey + (i % MASKING_KEY_WIDTH_IN_BYTES)]; // Unmask the payload.
				}
			}
		
			frame = new FrameData ();
			frame.opCode = opCode;
			frame.final = final;
			frame.reserved1 = reserved1;
			frame.reserved2 = reserved2;
			frame.reserved3 = reserved3;
			frame.masked = masked;
			frame.payload = p + maskingKeyLength;
			frame.payloadLength = payloadLength;
			frame.end = p + maskingKeyLength + payloadLength;
			
			return ParseFrameResult.FrameOK;
		}
		
		byte[] BuildFrame (OpCode opCode, byte[] data)
		{	
			var frame = new List<byte> ();
			frame.Add ((byte)(FINALBIT | (byte)opCode));
			if (data.Length <= MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD)
				frame.Add ((byte)(MASKBIT | data.Length & 0xFF));
			else if (data.Length <= 0xFFFF) {
				frame.Add (MASKBIT | PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD);
				frame.Add ((byte)((data.Length & 0xFF00) >> 8));
				frame.Add ((byte)(data.Length & 0xFF));
			} else {
				frame.Add (MASKBIT | PAYLOAD_WITH_EIGHT_BYTE_EXTENDED_FIELD);
				var extendedPayloadLength = new byte[8];
				int remaining = data.Length;
				// Fill the length into extendedPayloadLength in the network byte order.
				for (int i = 0; i < 8; ++i) {
					extendedPayloadLength [7 - i] = (byte)(remaining & 0xFF);
					remaining >>= 8;
				}
				frame.AddRange (extendedPayloadLength);
			}
			
			// Mask the frame.
			int maskingKeyStart = frame.Count;
			frame.AddRange (new byte[MASKING_KEY_WIDTH_IN_BYTES]); // Add placeholder for masking key. Will be overwritten.
			int payloadStart = frame.Count;
			frame.AddRange (data);
			
			Arc4RandomNumberGenerator.CryptographicallyRandomValues (frame, maskingKeyStart, MASKING_KEY_WIDTH_IN_BYTES);
			
			for (int i = 0; i < data.Length; ++i) {
				frame [payloadStart + i] ^= frame [maskingKeyStart + i % MASKING_KEY_WIDTH_IN_BYTES];
			}
		
			return frame.ToArray ();
		}
		
		void RemoveProcessed (List<byte> buffer, int length)
		{
			buffer.RemoveRange (0, length);
		}
		
		void StartClosingHandshake (CloseEventCode code, string reason)
		{
			
			if (closing) {
				return;
			}
			
			List<byte> buf = new List<byte> ();
			//if (!receivedClosingHandshake && code != CloseEventCode.CloseEventCodeNotSpecified) {
			if (!receivedClosingHandshake) {
				byte hb = (byte)((int)code >> 8);
				byte lb = (byte)code;
				buf.Add (hb);
				buf.Add (lb);
				buf.AddRange (System.Text.UTF8Encoding.UTF8.GetBytes (reason));
				outgoing.Add (new OutgoingMessage (OpCode.OpCodeClose, buf.ToArray ()));
			} 
			
			
			closing = true;
		}
		
		string WebSocketKey ()
		{
			return System.Convert.ToBase64String (System.Guid.NewGuid ().ToByteArray ());
		}
	}
	
	
	
	
}
