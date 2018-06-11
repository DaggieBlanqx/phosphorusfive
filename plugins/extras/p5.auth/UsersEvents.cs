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

using p5.exp;
using p5.core;
using p5.auth.helpers;
using p5.exp.exceptions;

namespace p5.auth
{
    /// <summary>
    ///     Class wrapping user related Active Events.
    /// </summary>
    static class UsersEvents
    {
        /// <summary>
        ///     Creates a new user.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.auth.users.create")]
        public static void p5_auth_users_create (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure only root account can invoke event.
            if (context.Ticket.Role != "root")
                throw new LambdaSecurityException ("Non-root user tried to create new user", e.Args, context);
            
            // Invoking implementation method.
            Users.CreateUser (context, e.Args);
        }

        /// <summary>
        ///     Retrieves a specific user in system.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.auth.users.get")]
        public static void p5_auth_users_get (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure only root account can invoke event.
            if (context.Ticket.Role != "root")
                throw new LambdaSecurityException ("Non-root user tried to retrieve existing user", e.Args, context);
            
            // House cleaning.
            using (new ArgsRemover (e.Args, true)) {
                Users.GetUser (context, e.Args);
            }
        }

        /// <summary>
        ///     Edits an existing user.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.auth.users.edit")]
        [ActiveEvent (Name = "p5.auth.users.edit-keep-settings")]
        public static void p5_auth_users_edit (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure only root account can invoke event.
            if (context.Ticket.Role != "root")
                throw new LambdaSecurityException ("Non-root user tried to edit existing user", e.Args, context);

            // Invoking implementation method.
            Users.EditUser (context, e.Args);
        }

        /// <summary>
        ///     Deletes a specific user in system.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.auth.users.delete")]
        public static void p5_auth_users_delete (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure only root account can invoke event.
            if (context.Ticket.Role != "root")
                throw new LambdaSecurityException ("Non-root user tried to delete existing user", e.Args, context);

            // Invoking implementation method.
            Users.DeleteUser (context, e.Args);
        }

        /// <summary>
        ///     Lists all users in system.
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.auth.users.list")]
        public static void p5_auth_users_list (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure only root account can invoke event.
            if (context.Ticket.Role != "root")
                throw new LambdaSecurityException ("Non-root user tried to list all users", e.Args, context);

            // Invoking implementation method.
            Users.ListUsers (context, e.Args);
        }
    }
}