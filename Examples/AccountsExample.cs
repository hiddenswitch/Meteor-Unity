using System.Collections;
using UnityEngine;

namespace Meteor.Examples
{
    /// <summary>
    /// A simple example demonstrating the accounts package and basic subscription and method calling.
    ///
    /// To use this with a meteor server, add the following content to a javascript file in the server/ directory:
    /// <code>
    /// let collection = new Mongo.Collection('collectionName', {connection: null});
    ///    Meteor.publish('subscriptionEndpointName', function (number1, number2, number3) {
    ///    console.log(`Subscribed: ${number1}, ${number2}, ${number3}`);
    ///    return collection.find({});
    /// });
    ///    
    /// Meteor.methods({
    ///    'getStringMethod': function (number1, number2, number3) {
    ///    console.log(`Called method: ${number1}, ${number2}, ${number3}`);
    ///    return "test";
    ///    }
    /// });
    ///    
    /// Meteor.setInterval(function () {
    ///    collection.insert({
    ///    stringField: 'test',
    ///    intField: 1
    ///    })
    /// }, 1000);
    ///    
    /// Accounts.onCreateUser(function (user) {
    ///    console.log(`User created: ${user._id}`);
    ///    return user;
    /// });
    /// </code>
    /// </summary>
    public class AccountsExample : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return StartCoroutine(MeteorExample());
        }

        IEnumerator MeteorExample()
        {
            var production = false;

            // Connect to the meteor server. Yields when you're connected
            yield return Meteor.Connection.Connect("ws://localhost:3000/websocket");

            // Login
            yield return (Coroutine) Meteor.Accounts.LoginAsGuest();

            // Create a collection
            var collection = new Meteor.Collection<DocumentType>("collectionName");

            // Add some handlers with the new observer syntax
            var observer = collection.Find().Observe(added: (string id, DocumentType document) =>
            {
                Debug.Log($"Document added: [_id={document._id}]");
            });

            // Subscribe
            var subscription = Meteor.Subscription.Subscribe("subscriptionEndpointName", /*arguments*/ 1, 3, 4);
            // The convention to turn something into a connection is to cast it to a Coroutine
            yield return (Coroutine) subscription;

            // Create a method call that returns a string
            var methodCall = Meteor.Method<string>.Call("getStringMethod", /*arguments*/1, 3, 4);

            // Execute the method. This will yield until all the database side effects have synced.
            yield return (Coroutine) methodCall;

            // Get the value returned by the method.
            Debug.Log($"Method response:\n{methodCall.Response}");
        }

        public class DocumentType : Meteor.MongoDocument
        {
            public string stringField;
            public int intField;
        }
    }
}