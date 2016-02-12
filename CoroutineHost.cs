
using System;

public class CoroutineHost : MonoSingleton<CoroutineHost>
{
	public CoroutineHost ()
	{
	}

	protected override void OnApplicationQuit ()
	{
		base.OnApplicationQuit ();
		try {
			Meteor.LiveData.Instance.Close ();
		#pragma warning disable 0168
		} catch (Exception e) {
		}
		#pragma warning restore 0168
	}
}

