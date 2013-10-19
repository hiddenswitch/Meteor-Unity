using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnTested;

namespace Meteor.Tests
{
	[TestFixture]
	public class ConnectionTestDependency
	{
		public ConnectionTestDependency ()
		{
		}

//		bool meteorStarted;

		[TestSetup]
		public IEnumerator ConnectToLocalhost() {
//			// Start meteor if it isn't running
//			meteorStarted = Process.GetProcesses ().Count (p => {
//				try {
//					return p.ProcessName == "node";
//				} catch (Exception e) {
//					return false;
//				}
//			}) != 0;
//
//			if (!meteorStarted) {
//				var meteorPath = Directory.GetParent (Application.dataPath).GetDirectories ("Meteor") [0].FullName;
//
//				var process = new Process () {
//					StartInfo = new ProcessStartInfo () {
//						WorkingDirectory = meteorPath,
//						FileName = "/usr/local/share/npm/bin/mrt",
//						UseShellExecute = false,
//						RedirectStandardOutput = true
//					},
//					EnableRaisingEvents = true
//				};
//
//				process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
//					if (e.Data.Contains("Meteor server running")) {
//						meteorStarted = true;
//					}
//				};
//
//				process.Start ();
//				process.BeginOutputReadLine ();
//				UnityEngine.Debug.Log(process.Threads.Count ());
//			}

			if (!LiveData.Instance.Connected) {
				yield return LiveData.Instance.Connect ("ws://127.0.0.1:3000/websocket");
			}
//
			Assert.IsTrue (LiveData.Instance.Connected);
//
			yield break;
		}
	}
}

