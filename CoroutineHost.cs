using System;

namespace Meteor.Internal
{
	/// <summary>
	/// An object used to host the internal coroutines for running the Meteor services.
	/// </summary>
	public class CoroutineHost : MonoSingleton<CoroutineHost>
	{
		public CoroutineHost ()
		{
		}

		protected override void OnApplicationQuit ()
		{
			base.OnApplicationQuit ();
			try {
				Meteor.Internal.LiveData.Instance.Close ();
				#pragma warning disable 0168
			} catch (Exception e) {
			}
			#pragma warning restore 0168
		}
	}

}