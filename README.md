Meteor-Unity
============

A Unity SDK for Meteor. Supports Unity3D 5.3.2 and higher. See the [Documentation](http://hiddenswitch.github.io/Meteor-Unity/annotated.html).

Current version: v3.0. This release supports `il2cpp` backends, allowing it to be used in production for iOS builds. iOS, Android and desktop platforms are supported. WebGL is not supported.

See the example code at the bottom of the Readme for an overview of all the supported features. Wherever possible, the API matches the Meteor API, and the details match Meteor details. There are a few exceptions:

 - In Meteor, disconnecting and reconnecting triggers a removal of all records and adding of all new records, as though a subscription was torn down and recreated. In Meteor-Unity, disconnecting does not result in removing records, while reconnecting will result in add messages / added called in observe handles.

Compared to Meteor, Meteor-Unity has some limitations. It cannot simulate code yet, so database side effects must come from the server. It cannot handle documents that aren't explicitly typed.

##### Getting started

  0. Check out the documentation at http://hiddenswitch.github.io/Meteor-Unity/annotated.html.

  1. Install `meteor` and `git`
 
    ```sh
    # Install meteor
    curl https://install.meteor.com/ | sh
    # Install brew
    ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"
    # Install git
    brew install git
    ```

  2. Create a new Unity project.
    
    ```sh
    # For Mac
    /Applications/Unity/Unity.app/Contents/MacOS/Unity -createProject ~/Documents/Example
    cd ~/Documents/Example
    ```

  3. Set your Unity project to use text serialization and visible metafiles. This is very important for good `git` functionality.
  4. Initialize `git` and add the Meteor SDK as a submodule. This lets you use the bleeding edge of the project and contribute back changes to it really easily.
  
    ```sh
    # Grab my handy and advanced .gitignore. This command downloads something from the Internet and saves it to a file.
    curl https://github.com/hiddenswitch/Meteor-Unity-Tests/blob/develop/.gitignore > .gitignore
    # Initialize git for the root of your project.
    git init
    # Make this your first commit
    git -am "Initial commit."
    # Add the submodules and initialize them. You can use the Meteor-Unity repo directly, or fork it so you can make changes to it (my practice).
    git submodule add https://github.com/hiddenswitch/Meteor-Unity.git Assets/Scripts/Meteor-Unity
    git submodule update --init
    git commit -am "Adding submodules"
    ```

  5. Create a `meteor` project, add the `accounts-password` package, and run the project.
  
    ```sh
    meteor create Web
    cd Web
    meteor add accounts-password
    meteor
    ```
  
  6. Connect to your server from Unity. All `meteor` work should live in coroutines.
  
    ```c#
    // This will give you access to a .Serialize() method on every object to turn it into
    // its JSON representation
    using Meteor.Extensions;
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
  			Debug.Log(string.Format("Document added:\n{0}", document.Serialize()));
  		});
  
  		// Subscribe
  		var subscription = Meteor.Subscription.Subscribe ("subscriptionEndpointName", /*arguments*/ 1, 3, 4);
  		// The convention to turn something into a connection is to cast it to a Coroutine
  		yield return (Coroutine)subscription;
  
  		// Create a method call that returns a string
  		var methodCall = Meteor.Method<string>.Call ("getStringMethod", /*arguments*/1, 3, 4);
  
  		// Execute the method. This will yield until all the database sideffects have synced.
  		yield return (Coroutine)methodCall;
  
  		// Get the value returned by the method.
  		Debug.Log (string.Format ("Method response:\n{0}", methodCall.Response));
  	}
  
  	public class DocumentType : Meteor.MongoDocument {
  		public string stringField;
  		public int intField;
  	}
    ```
