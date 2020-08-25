using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Extensions;
using Postgrest.Models;
using Postgrest.Responses;
using static Postgrest.Helpers;

namespace Postgrest
{
    public class Client
    {
        public string BaseUrl { get; private set; }

        private ClientAuthorization authorization;
        private ClientOptions options;

        private static Client instance;
        public static Client Instance
        {
            get
            {
                if (instance == null)
                    instance = new Client();

                return instance;
            }
        }

        private Client() { }

        public Client Initialize(string baseUrl, ClientAuthorization authorization, ClientOptions options = null)
        {
            BaseUrl = baseUrl;

            if (options == null)
                options = new ClientOptions();

            if (authorization == null)
                authorization = new ClientAuthorization(ClientAuthorization.AuthorizationType.Open, null);

            this.options = options;
            this.authorization = authorization;

            return this;
        }

        public Builder<T> Builder<T>() where T : BaseModel, new() => new Builder<T>(BaseUrl, authorization, options);
    }
}
