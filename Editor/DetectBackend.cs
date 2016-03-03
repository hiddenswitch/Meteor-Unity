using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Meteor.Editor
{
	[InitializeOnLoad]
	public class DetectBackend
	{
		static DetectBackend ()
		{
			#if ENABLE_IL2CPP
			Debug.LogError ("Meteor-Unity is currently not supported on the IL2CPP backend.");
			#endif
		}
	}
}