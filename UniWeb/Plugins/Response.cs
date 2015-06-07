using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

#if USE_GZIP
using Ionic.Zlib;

#endif
using UnityEngine;

namespace HTTP
{
	public class Response
	{
		public int status = 200;
		public string message = "OK";
		public float progress = 0f;
		public bool chunked, zipped, cacheable;
		MemoryStream output;
		byte[] bytes = null;
		public Headers headers = new Headers();
		
		public string Text {
			get {
				return System.Text.Encoding.Default.GetString(Bytes);
			}
		}

		public byte[] Bytes {
			get {
				if (bytes == null) {
					
#if USE_GZIP
					lock (output) {
						if (zipped) {
							bytes = new byte[0];
							using (var gz = new GZipStream (output, CompressionMode.Decompress)) {
								var buffer = new byte[1024];
								var count = -1;
								output.Seek (0, SeekOrigin.Begin);
								while (count != 0) {
									count = gz.Read (buffer, 0, buffer.Length);
									var offset = bytes.Length;
									Array.Resize<byte> (ref bytes, offset + count);
									Array.Copy (buffer, 0, bytes, offset, count);
								}
							}
						} else {
							bytes = output.ToArray ();
						}
					}
						
#else							
					lock(output) {
						bytes = output.ToArray ();
						output.SetLength (0);	
					}
							
#endif
				}
				
				return bytes;
			}
			set {
				output.SetLength (0);
				output.Write (value, 0, value.Length);
			}
		}

		public AssetBundleCreateRequest Asset {
			get { return AssetBundle.CreateFromMemory (Bytes); }
		}
		
		[System.Obsolete("use headers.Add")]
		public void AddHeader (string name, string value)
		{
			headers.Add(name, value);
		}
		
		[System.Obsolete("use headers.Set")]
		public void SetHeader (string name, string value)
		{
			headers.Set(name, value);
		}
		
		[System.Obsolete("use headers.GetAll")]
		public List<string> GetHeaders (string name)
		{
			return headers.GetAll(name);
		}
		
		[System.Obsolete("use headers.Keys")]
		public List<String> AvailableHeaders ()
		{
			return headers.Keys;
		}
		
		[System.Obsolete("use headers.Get")]
		public string GetHeader (string name)
		{
			return headers.Get(name);
		}

		public Response (Request request)
		{
			output = new MemoryStream ();
		}

		string ReadLine (Stream stream)
		{
			var line = new List<byte> ();
			while (true) {
				int c = -1;
				try {
					c = stream.ReadByte ();
				} catch(IOException) {
					throw new HTTPException ("Terminated Stream");
				}
				if (c == -1) {
					throw new HTTPException ("Unterminated Stream");
				}
				var b = (byte)c;
				if (b == Request.EOL [1]) {
					break;
				}
				line.Add (b);
			}
			var s = ASCIIEncoding.ASCII.GetString (line.ToArray ()).Trim ();
			return s;
		}

		string[] ReadKeyValue (Stream stream)
		{
			string line = ReadLine (stream);
			if (line == "") {
				return null;
			} else {
				var split = line.IndexOf (':');
				if (split == -1) {
					return null;
				}
				var parts = new string[2];
				parts [0] = line.Substring (0, split).Trim ();
				parts [1] = line.Substring (split + 1).Trim ();
				return parts;
			}
			
		}

		public void ReadFromStream (Stream inputStream)
		{
			progress = 0;
			cacheable = false;
			if (inputStream == null) {
				throw new HTTPException ("Cannot read from server, server probably dropped the connection.");
			}
			var top = ReadLine (inputStream).Split (' ');
			lock (output) {
				output.SetLength (0);
			}
			
			status = -1;
			
			if (!(top.Length > 0 && int.TryParse (top [1], out status))) {
				throw new HTTPException ("Bad Status Code, server probably dropped the connection.");
			}
			message = string.Join (" ", top, 2, top.Length - 2);
			headers.Clear ();
			
			while (true) {
				// Collect Headers
				string[] parts = ReadKeyValue (inputStream);
				if (parts == null) {
					break;
				}
				headers.Add (parts [0], parts [1]);
			}
			
			cacheable = string.Compare (headers.Get ("Etag"), "", true) != 0;
			chunked = string.Compare (headers.Get ("Transfer-Encoding"), "chunked", true) == 0;
			zipped = string.Compare (headers.Get ("Content-Encoding"), "gzip", true) == 0;
			byte[] buffer = new byte[1024];
			
			if (chunked) {
				while (true) {
					// Collect Body
					var hexLength = ReadLine (inputStream);
					if (hexLength == "0") {
						break;
					}
					var length = int.Parse (hexLength, NumberStyles.AllowHexSpecifier);
					progress = 0;
					var contentLength = length;
					while (length > 0) {
						var count = inputStream.Read (buffer, 0, Mathf.Min (buffer.Length, length));
						WriteOutput (buffer, count);
						progress = Mathf.Clamp01 (1 - ((float)length / (float)contentLength));
						length -= count;
					}
					progress = 1;
					//forget the CRLF.
					inputStream.ReadByte ();
					inputStream.ReadByte ();
					
				}
				
				while (true) {
					//Collect Trailers
					string[] parts = ReadKeyValue (inputStream);
					if (parts == null) {
						break;
					}
					headers.Add (parts [0], parts [1]);
				}
				
			} else {
				// Read Body
				int contentLength = 0;
				if (int.TryParse (headers.Get ("Content-Length"), out contentLength)) {
					if (contentLength > 0) {
						var remaining = contentLength;
						while (remaining > 0) {
							var count = inputStream.Read (buffer, 0, buffer.Length);
							if (count == 0) {
								break;
							}
							remaining -= count;
							WriteOutput (buffer, count);
							progress = Mathf.Clamp01 (1.0f - ((float)remaining / (float)contentLength));
						}
					}
				}
			}
		}

		void WriteOutput (byte[] buffer, int count)
		{
			lock (output) {
				output.Write (buffer, 0, count);
			}
		}
	}
	
}

