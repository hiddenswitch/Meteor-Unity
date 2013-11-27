using System;
using System.Collections;
using Extensions;

namespace Meteor
{
	public class CachedCollection<TRecordType> : Collection<TRecordType>
		where TRecordType : MongoDocument, new()
	{
		protected CachedCollection() : base() {}

		public static Collection<TRecordType> CreateFromUrl(string name, string url) {
			var collection = Collection<TRecordType>.Create (name);
			CoroutineHost.Instance.StartCoroutine (DownloadFromUrl(collection, url));
			return collection;
		}

		protected static IEnumerator DownloadFromUrl(Collection<TRecordType> collection, string url) {
			var request = new HTTP.Request ("GET", url, true);

			var icollection = collection as ICollection;

			request.Send ();
			yield return request.Wait ();

			// Deserialize the collection
			var result = request.response.Text.Deserialize<Collection<TRecordType>> ();

			// Iterate through and update from the result. Remove old stuff
			foreach (var kv in result) {
				if (collection.Contains(kv)) {
					icollection.Changed (kv._id, null, kv.Coerce<Hashtable> ());
				}
			}
		}
	}
}

