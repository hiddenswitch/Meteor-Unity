using System;
using Meteor.LiveData;
using UnityEngine;

namespace Meteor
{
	public static class Meteor
	{
		public static bool IsConnected {
			get {
				return LiveData.LiveData.Instance.Connected;
			}
		}

		public static Coroutine Connect(string url) {
			return LiveData.LiveData.Instance.Connect (url);
		}
	}
}

