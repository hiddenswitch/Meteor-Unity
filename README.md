Meteor-Unity
============

**Download Version 3.0:** http://hiddenswitch.github.io/Meteor-Unity/Meteor-Unity_v3.0.unitypackage.

A Unity SDK for Meteor. Supports Unity3D 5.3.2 and higher. See the [Documentation](http://hiddenswitch.github.io/Meteor-Unity/annotated.html).

This release supports `il2cpp` backends, allowing it to be used in production for iOS builds. iOS, Android and desktop platforms are supported. WebGL is not supported.

See the example code at the bottom of the Readme for an overview of all the supported features. Wherever possible, the API matches the Meteor API, and the details match Meteor details. There is an exception to how things usually work in Meteor:

 - In Meteor, disconnecting and reconnecting triggers a removal of all records and adding of all new records, as though a subscription was torn down and recreated. In Meteor-Unity, disconnecting does not result in removing records, while reconnecting will result in add messages / added called in observe handles.

Compared to Meteor, Meteor-Unity has some limitations. It cannot simulate code yet, so database side effects must come from the server. It cannot handle documents that aren't explicitly typed.

##### Getting Started

  0. Check out the documentation at http://hiddenswitch.github.io/Meteor-Unity/annotated.html.

  1. Install `meteor`.
 
    ```sh
    # Install meteor
    curl https://install.meteor.com/ | sh
    ```

  2. Create a new Unity project.
    
    ```sh
    # For Mac
    /Applications/Unity/Unity.app/Contents/MacOS/Unity -createProject ~/Documents/Example
    cd ~/Documents/Example
    ```

  4. Download and install the [Meteor-Unity package](http://hiddenswitch.github.io/Meteor-Unity/Meteor-Unity_v3.0.unitypackage).
  

  5. Create a `meteor` project, add the `accounts-password` package, and run the project.
  
    ```sh
    meteor create Web
    cd Web
    meteor add accounts-password
    meteor
    ```
  
  6. Connect to your server from Unity. All `meteor` work should live in coroutines. Here is an example which uses a coroutine (`IEnumerator`) to sequence a bunch of actions one after another. In this example, we assume we've defined a collection called `collectionName` on the server and created a few methods and subscriptions. You can't copy and paste the code below and expect it to work with an empty `meteor` project, but it will compile.
  
    ```c#
    IEnumerator MeteorExample() {
      var production = false;

  		// Connect to the meteor server. Yields when you're connected
  		if (production) {
  		  yield return Meteor.Connection.Connect ("wss://productionserver.com/websocket");
  		} else {
  		  yield return Meteor.Connection.Connect ("ws://localhost:3000/websocket");
  		}
  
  		// Login
  		yield return (Coroutine)Meteor.Accounts.LoginAsGuest ();
  
  		// Create a collection
  		var collection = new Meteor.Collection<DocumentType> ("collectionName");
  
  		// Add some handlers with the new observer syntax
  		var observer = collection.Find ().Observe (added: (string id, DocumentType document) => {
  			Debug.Log(string.Format("Document added: [_id={0}]", document._id));
  		});
  
  		// Subscribe
  		var subscription = Meteor.Subscription.Subscribe ("subscriptionEndpointName", /*arguments*/ 1, 3, 4);
  		// The convention to turn something into a connection is to cast it to a Coroutine
  		yield return (Coroutine)subscription;
  
  		// Create a method call that returns a string
  		var methodCall = Meteor.Method<string>.Call ("getStringMethod", /*arguments*/1, 3, 4);
  
  		// Execute the method. This will yield until all the database side effects have synced.
  		yield return (Coroutine)methodCall;
  
  		// Get the value returned by the method.
  		Debug.Log (string.Format ("Method response:\n{0}", methodCall.Response));
  	}
  
  	public class DocumentType : Meteor.MongoDocument {
  		public string stringField;
  		public int intField;
  	}
    ```
