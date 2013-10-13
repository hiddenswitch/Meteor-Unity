namespace Net.DDP.Client.Messages
{
	public class SubscribeMessage : Message
	{
		public string name;
		[JsonFx.Json.JsonName("params")]
		public object[] Params;
		public string id;

		public SubscribeMessage() {
			msg = "sub";
		}
	}
}
