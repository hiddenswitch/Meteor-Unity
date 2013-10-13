// srp4net 1.0
// SRP for .NET - A JavaScript/C# .NET library for implementing the SRP authentication protocol
// http://code.google.com/p/srp4net/
// Copyright 2010, Sorin Ostafiev (http://www.ostafiev.com/)
// License: GPL v3 (http://www.gnu.org/licenses/gpl-3.0.txt)

using System;
using UnityEngine;

namespace srp4net.Helpers
{
    public abstract partial class Crypto
    {
        public abstract class SRP
        {
            public static BigInteger N { get; private set; }
            public static string NHex { get; private set; }
            public static int _nbits;
            private static BigInteger Nminus1;

            public static BigInteger g { get; private set; }
            public static string gHex { get; private set; }

            public static BigInteger k { get; private set; }
            public static string kHex { get; private set; }

			public static BigInteger a {get; private set; }
			public static BigInteger A { get; private set;}
			public static string Ahex {get; private set;}

			public static BigInteger S { get; private set;}
			public static string HAMK { get; private set;}


            private static string HHex(string x)
            {
                return Crypto.HashHex((((x.Length & 1) == 0) ? "" : "0") + x);
            }

            static SRP()
            {
                // initialize N
                {
                    NHex =
                        //512bit
                        //"D4C7F8A2B32C11B8FBA9581EC4BA4F1B04215642EF7355E37C0FC0443EF756EA2C6B8EEB755A1C723027663CAA265EF785B8FF6A9B35227A52D86633DBDFCA43";
						//256bit
						"EEAF0AB9ADB38DD69C33F80AFA8FC5E86072618775FF3C0B9EA2314C9C256576D674DF7496EA81D3383B4813D692C6E0E0D5D8E250B98BE48E495C1D6089DAD15DC7D7B46154D6B6CE8EF4AD69B15D4982559B297BCF1885C529F566660E57EC68EDBC3C05726CC02FD4CBF4976EAA9AFD5138FE8376435B9FC61D2FC0EB06E3".ToLowerInvariant();
                    N = new BigInteger(NHex, 16);
                    _nbits = N.bitCount();
                    Nminus1 = N - 1;

//                    if (!N.isProbablePrime(80))
//                    {
//                        throw new Exception("Warning: N is not prime");
//                    }
//
//                    if (!(Nminus1 / 2).isProbablePrime(80))
//                    {
//                        throw new Exception("Warning: (N-1)/2 is not prime");
//                    }
                }

                // initialize g
                {
                    gHex = "2";
                    g = new BigInteger(gHex, 16);
                }

                // initialize k = H(N || g)
                {
                    BigInteger ktmp = new BigInteger(HHex(
                        (((NHex.Length & 1) == 0) ? "" : "0") + NHex +
                        new string('0', NHex.Length - gHex.Length) + gHex
                        ), 16);

                    k = (ktmp < N) ? ktmp : (ktmp % N);
                    kHex = k.ToString(16).ToLowerInvariant().TrimStart('0');
                }

				// initialize a, A
				{
					a = new BigInteger();
					a.genRandomBits(36);
					A = g.modPow(a, N);

					while (A.modInverse(N) == 0)
					{
						a = new BigInteger();
						a.genRandomBits(36);
						A = g.modPow(a, N);
					}
					Ahex = A.ToString(16).ToLowerInvariant().TrimStart('0');
				}
            }

			/// <summary>
			/// Generate a new SRP verifier. Password is the plaintext password.
			/// </summary>
			/// <returns>The verifier.</returns>
			/// <param name="password">Password.</param>
			public static Schema.Verifier GenerateVerifier(string password, string identity = null, string salt = null)
			{
				if (identity == null)
				{
					BigInteger i = new BigInteger ();
					i.genRandomBits (36);
					identity = i.ToString(16).ToLowerInvariant().TrimStart('0');
				}

				if (salt == null)
				{
					BigInteger s = new BigInteger ();
					s.genRandomBits (36);
					salt = s.ToString(16).ToLowerInvariant().TrimStart('0');
				}

				string x = Hash (salt + Hash (identity + ":" + password));

				BigInteger xi = new BigInteger (x, 16);
				BigInteger v = g.modPow (xi, N);

				return new Schema.Verifier () {
					identity = identity,
					salt = salt,
					verifier = v.ToString(16).ToLowerInvariant().TrimStart('0')
				};
			}

			/// <summary>
			/// Initiate an SRP exchange.
			/// </summary>
			/// <returns>The exchange.</returns>
			public static Schema.StartExchange StartExchange()
			{
				return new Schema.StartExchange () {
					A = Ahex
				};
			}

			/// <summary>
			/// Respond to the server's challenge with a proof of password.
			/// </summary>
			/// <returns>The to challenge.</returns>
			/// <param name="password">Password.</param>
			/// <param name="identity">Identity.</param>
			/// <param name="salt">Salt.</param>
			/// <param name="Bhex">Bhex.</param>
			public static Schema.ChallengeResponse RespondToChallenge(string password, string identity, string salt, string Bhex)
			{
				BigInteger B = new BigInteger (Bhex, 16);
				BigInteger u = new BigInteger (Hash (Ahex + Bhex), 16);
				BigInteger x = new BigInteger (Hash (salt + Hash (identity + ":" + password)),16);

				BigInteger kgx = k * (g.modPow (x, N));
				BigInteger aux = a + (u * x);
				S = (B - kgx).modPow (aux, N);
				string Shex = S.ToString(16).ToLowerInvariant().TrimStart('0');
				string M = Hash (Ahex + Bhex + Shex);
				HAMK = Hash (Ahex + M + Shex);

				return new Schema.ChallengeResponse () {
					M = M.ToLowerInvariant().TrimStart('0')
				};
			}

            public static void AuthStep1(
                string vHex,
                string AHex,
                out string bHex,
                out string BHex,
                out string uHex)
            {
                BigInteger v = new BigInteger(vHex, 16);
                //BigInteger A = new BigInteger(AHex, 16); REMOVED WARNING

                BigInteger b;
                // b - ephemeral private key
                // b = random between 2 and N-1
                {
                    b = new BigInteger();
                    //[TODO] perhaps here use a better random generator
                    b.genRandomBits(_nbits);

                    if (b >= N)
                    {
                        b = b % Nminus1;
                    }
                    if (b < 2)
                    {
                        b = 2;
                    }
                }
                bHex = b.ToHexString();

                // B = public key
                // B = kv + g^b (mod N)
                BigInteger B = (v * k + g.modPow(b, N)) % N;
                BHex = B.ToHexString();

                BigInteger u;
                // u - scrambling parameter
                // u = H (A || B)
                {
                    int nlen = 2 * ((_nbits + 7) >> 3);

                    BigInteger utmp = new BigInteger(HHex(
                        new string('0', nlen - AHex.Length) + AHex +
                        new string('0', nlen - BHex.Length) + BHex
                        ), 16);

                    u = (utmp < N) ? utmp : (utmp % Nminus1);
                }

                uHex = u.ToHexString();
            }

            public static void AuthStep2(
                string vHex,
                string uHex,
                string AHex,
                string bHex,
                string BHex,
                out string m1serverHex,
                out string m2Hex)
            {
                BigInteger v = new BigInteger(vHex, 16);
                BigInteger u = new BigInteger(uHex, 16);
                BigInteger A = new BigInteger(AHex, 16);
                BigInteger b = new BigInteger(bHex, 16);
               //BigInteger B = new BigInteger(BHex, 16); REMOVED WARNING JOE

                // S - common exponential value
                // S = (A * v^u) ^ b (mod N)
                BigInteger S = ((v.modPow(u, N) * A) % N).modPow(b, N);

                // K - the strong cryptographically session key
                // K = H(S)
                string KHex = HHex(S.ToHexString()).TrimStart('0');

                // m2 - expected client's proof as computed by the server
                m1serverHex = HHex(
                    AHex +
                    BHex +
                    KHex).TrimStart('0');

                // m2 - server's proof that it has the correct key
                m2Hex = HHex(
                    AHex +
                    m1serverHex +
                    KHex).TrimStart('0');
            }
        }
    }
}
