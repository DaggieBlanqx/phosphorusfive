/*
 * Phosphorus Five, copyright 2014 - 2017, Thomas Hansen, thomas@gaiasoul.com
 * 
 * This file is part of Phosphorus Five.
 *
 * Phosphorus Five is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3, as published by
 * the Free Software Foundation.
 *
 *
 * Phosphorus Five is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Phosphorus Five.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * If you cannot for some reasons use the GPL license, Phosphorus
 * Five is also commercially available under Quid Pro Quo terms. Check 
 * out our website at http://gaiasoul.com for more details.
 */

using System;
using System.IO;
using p5.exp;
using p5.core;
using p5.crypto.helpers;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace p5.crypto
{
    /// <summary>
    ///     Class wrapping the meta events for PGP keys in Phosphorus Five.
    /// </summary>
    public static class GnuPGKeys
    {
        /// <summary>
        ///     Lists all public keys matching the given filter from the PGP context.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.list-public-keys")]
        static void p5_crypto_list_public_keys (ApplicationContext context, ActiveEventArgs e)
        {
            // Using common helper to iterate all secret keys.
            PGPKeyIterator.Find (context, e.Args, delegate (PgpPublicKey key) {

                // Retrieving fingerprint of currently iterated key, and returning to caller.
                var fingerprint = BitConverter.ToString (key.GetFingerprint ()).Replace ("-", "").ToLower ();
                e.Args.Add (fingerprint);

            }, false);
        }

        /// <summary>
        ///     Lists all private keys matching the given filter from the PGP context.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.list-private-keys")]
        static void p5_crypto_list_private_keys (ApplicationContext context, ActiveEventArgs e)
        {
            // Using common helper to iterate all secret keys.
            PGPKeyIterator.Find (context, e.Args, delegate (PgpSecretKey key) {

                // Retrieving fingerprint of currently iterated key, and returning to caller.
                var fingerprint = BitConverter.ToString (key.PublicKey.GetFingerprint ()).Replace ("-", "").ToLower ();
                e.Args.Add (fingerprint);

            }, false);
        }

        /// <summary>
        ///     Returns the details (meta information) about all specified PGP key.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.get-key-details")]
        static void p5_crypto_get_key_details (ApplicationContext context, ActiveEventArgs e)
        {
            // Using common helper to iterate all secret keys.
            PGPKeyIterator.Find (context, e.Args, delegate (PgpPublicKey key) {

                // This key is matching specified filter criteria.
                var fingerprint = BitConverter.ToString (key.GetFingerprint ()).Replace ("-", "").ToLower ();
                var node = e.Args.Add (fingerprint).LastChild;
                node.Add ("id", ((int)key.KeyId).ToString ("X"));
                node.Add ("algorithm", key.Algorithm.ToString ());
                node.Add ("strength", key.BitStrength);
                node.Add ("creation-time", key.CreationTime);
                node.Add ("is-encryption-key", key.IsEncryptionKey);
                node.Add ("is-master-key", key.IsMasterKey);
                node.Add ("is-revoked", key.IsRevoked ());
                node.Add ("version", key.Version);
                DateTime expires = key.CreationTime.AddSeconds (key.GetValidSeconds ());
                node.Add ("expires", expires);

                // Adding all user IDs that are strings.
                foreach (var idxUserId in key.GetUserIds ()) {
                    if (idxUserId is string)
                        node.FindOrInsert ("user-ids").Add ("", idxUserId);
                }

                // Adding key IDs of all keys that have signed this key.
                foreach (PgpSignature signature in key.GetSignatures ()) {
                    node.FindOrInsert ("signed-by").Add (((int)signature.KeyId).ToString ("X"), signature.CreationTime);
                }

            }, false);
        }

        /// <summary>
        ///     Returns the specified public PGP keys.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.get-public-key")]
        static void p5_crypto_get_public_key (ApplicationContext context, ActiveEventArgs e)
        {
            // Using common helper to iterate all secret keys.
            PGPKeyIterator.Find (context, e.Args, delegate (PgpPublicKey key) {

                // Retrieving fingerprint of currently iterated key, and returning to caller.
                var fingerprint = BitConverter.ToString (key.GetFingerprint ()).Replace ("-", "").ToLower ();
                var node = e.Args.Add (fingerprint).LastChild;

                // This is the key we're looking for
                using (var memStream = new MemoryStream ()) {
                    using (var armored = new ArmoredOutputStream (memStream)) {
                        key.Encode (armored);
                        armored.Flush ();
                    }
                    memStream.Flush ();
                    memStream.Position = 0;
                    var sr = new StreamReader (memStream);
                    node.Value = sr.ReadToEnd ();
                }
            }, false);
        }

        /// <summary>
        ///     Removes a private key from GnuPG database
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.delete-private-key")]
        static void p5_crypto_delete_private_key (ApplicationContext context, ActiveEventArgs e)
        {
            // House cleaning.
            using (new ArgsRemover (e.Args, true)) {

                // Creating new GnuPG context.
                using (var ctx = new GnuPrivacyContext (true)) {

                    // Signaler boolean.
                    bool somethingWasRemoved = false;
                    var bundle = ctx.SecretKeyRingBundle;

                    // Looping through each ID given by caller.
                    foreach (var idxId in XUtil.Iterate<string> (context, e.Args)) {

                        // Looping through each public key ring in GnuPG database until we find given ID.
                        foreach (PgpSecretKeyRing idxSecretKeyRing in bundle.GetKeyRings ()) {

                            // Looping through each key in keyring.
                            foreach (PgpSecretKey idxSecretKey in idxSecretKeyRing.GetSecretKeys ()) {

                                // Checking for a match, making sure we do not match UserIDs.
                                if (PGPKeyIterator.IsMatch (idxSecretKey.PublicKey, idxId, false)) {

                                    // Removing entire keyring, and signaling to save keyring bundle.
                                    somethingWasRemoved = true;
                                    bundle = PgpSecretKeyRingBundle.RemoveSecretKeyRing (bundle, idxSecretKeyRing);

                                    // Breaking inner most foreach.
                                    break;
                                }
                            }

                            // Checking if currently iterated filter was found in currently iterated secret keyring.
                            if (somethingWasRemoved)
                                break;
                        }
                    }

                    // Checking to see if something was removed, and if so, saving GnuPG context
                    if (somethingWasRemoved)
                        ctx.SaveSecretKeyRingBundle (bundle);
                }
            }
        }

        /// <summary>
        ///     Removes a public key from GnuPG database
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.crypto.delete-public-key")]
        static void p5_crypto_delete_public_key (ApplicationContext context, ActiveEventArgs e)
        {
            // House cleaning
            using (new ArgsRemover (e.Args, true)) {

                // Creating new GnuPG context
                using (var ctx = new GnuPrivacyContext (true)) {

                    // Signaler boolean
                    bool somethingWasRemoved = false;
                    var bundle = ctx.PublicKeyRingBundle;

                    // Looping through each ID given by caller
                    foreach (var idxId in XUtil.Iterate<string> (context, e.Args)) {

                        // Looping through each public key ring in GnuPG database until we find given ID
                        foreach (PgpPublicKeyRing idxPublicKeyRing in bundle.GetKeyRings ()) {

                            // Looping through each key in keyring
                            foreach (PgpPublicKey idxPublicKey in idxPublicKeyRing.GetPublicKeys ()) {

                                // Checking for a match, making sure we do not match UserIDs.
                                if (PGPKeyIterator.IsMatch (idxPublicKey, idxId, false)) {

                                    // Removing entire keyring, and signaling to save keyring bundle
                                    somethingWasRemoved = true;
                                    bundle = PgpPublicKeyRingBundle.RemovePublicKeyRing (bundle, idxPublicKeyRing);

                                    // Breaking inner most foreach
                                    break;
                                }
                            }

                            // Checking if currently iterated filter was found in currently iterated secret keyring.
                            if (somethingWasRemoved)
                                break;
                        }
                    }

                    // Checking to see if something was removed, and if so, saving GnuPG context
                    if (somethingWasRemoved)
                        ctx.SavePublicKeyRingBundle (bundle);
                }
            }
        }
    }
}

