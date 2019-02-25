﻿// The MIT License (MIT)
//
// gitea.net (https://github.com/mkloubert/gitea.net)
// Copyright (c) Marcel Joachim Kloubert <marcel.kloubert@gmx.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER

using Gitea.API.v1.Repositories;
using Gitea.API.v1.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Gitea.API.v1.Repositories
{
    /// <summary>
    /// A builder class for migrating an external repository for an user.
    /// </summary>
    public class RepositoryBuilder : IDisposable
    {
        /// <summary>
        /// Stores the properties for the request.
        /// </summary>
        protected IDictionary<string, object> _properties = new Dictionary<string, object>();
        string _ownerName;
        bool _isOrg = false;

        public RepositoryBuilder(User user)
        {
            User = user;
        }


        /// <summary>
        /// Sets the description for the repository in Gitea.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>That instance.</returns>
        public RepositoryBuilder Description(string description)
        {
            _properties["description"] = description;

            return this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _properties?.Clear();
            _properties = null;
        }

         
        public RepositoryBuilder MakeAutoInit(bool auto_init = true)
        {
            _properties["auto_init"] = auto_init;

            return this;
        }
        
        /// <summary>
        /// Sets if the migrated repository should be private or not.
        /// </summary>
        /// <param name="@private">Make private or not.</param>
        /// <returns>That instance.</returns>
        public RepositoryBuilder MakePrivate(bool @private = true)
        {
            _properties["private"] = @private;

            return this;
        }

        /// <summary>
        /// Sets the name of the repository in Gitea.
        /// </summary>
        /// <param name="repo_name">The new name.</param>
        /// <returns>That instance.</returns>
        public RepositoryBuilder Name(string repo_name)
        {
            _properties["name"] = repo_name;
            return this;
        }
        public RepositoryBuilder MakeGitignores(string gitignores)
        {
            _properties["gitignores"] = gitignores;

            return this;
        }
        public RepositoryBuilder MakeLicense(string license)
        {
            _properties["license"] = license;

            return this;
        }
        public RepositoryBuilder MakeReadme(string readme)
        {
            _properties["readme"] = readme;

            return this;
        }

        public RepositoryBuilder Owner(string name, bool isOrganization)
        {
            _ownerName = name;
            _isOrg = isOrganization;

            return this;
        }
        
        /// <summary>
        /// Starts migration.
        /// </summary>
        /// <returns>The new repository</returns>
        public async Task<Repository> Start()
        {
            using (var rest = User.Endpoint.Client.CreateBaseClient())
            {
                var resp = await rest.PostAsync(
                    _isOrg ? $"org/{_ownerName}/repos" : "user/repos",
                    new StringContent(ToJson(), Encoding.UTF8, "application/json")
                );

                Exception exception = null;

                if (resp.StatusCode != HttpStatusCode.Created)
                {
                    switch ((int)resp.StatusCode)
                    {
                        case 422:
                        case 500:
                            exception = new ApiException(JsonConvert.DeserializeObject<ApiError>
                                (
                                    await resp.Content.ReadAsStringAsync()
                                ),
                                (int)resp.StatusCode, resp.ReasonPhrase);
                            break;

                        default:
                            exception = new UnexpectedResponseException((int)resp.StatusCode,
                                                                        resp.ReasonPhrase);
                            break;
                    }
                }

                if (exception != null)
                {
                    throw exception;
                }

                var repo = JsonConvert.DeserializeObject<Repository>
                    (
                        await resp.Content.ReadAsStringAsync()
                    );

                if (repo?.Owner != null)
                {
                    repo.Owner.Endpoint = User.Endpoint;
                }

                return repo;
            }
        }

        /// <summary>
        /// Converts the current data to a JSON.
        /// </summary>
        /// <returns>The JSON.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(_properties);
        }

        /// <summary>
        /// Gets the underlying user.
        /// </summary>
        public User User { get; internal protected set; }
    }
}