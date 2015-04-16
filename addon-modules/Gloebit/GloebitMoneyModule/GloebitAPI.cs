/*
 * Copyright (c) 2015 Gloebit LLC
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;

namespace Gloebit.GloebitMoneyModule {

    public class GloebitAPI {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        // TODO - build this redirect_uri correctly with the correct public hostname and port
        private const string REDIRECT_URI = "http://localhost:9000/gloebit/auth_complete";

        private string m_key;
        private string m_keyAlias;
        private string m_secret;
        private Uri m_url;


        public class User {
            private readonly string agentId;
            private readonly string userId;
            private readonly string token;

            private static Dictionary<string,string> s_tokenMap = new Dictionary<string, string>();

            private User(string agentId, string userId, string token) {
                this.agentId = agentId;
                this.userId = userId;
                this.token = token;
            }

            public static User Get(UUID agentID) {
                string agentIdStr = agentID.ToString();
                string token;
                lock(s_tokenMap) {
                    s_tokenMap.TryGetValue(agentIdStr, out token);
                }

                // TODO - enable AvatarService persistence of tokens
                //Scene s = LocateSceneClientIn(agentID);
                //AvatarData ad = s.AvatarService.GetAvatar(agentID);
                //Dictionary<string,string> data = ad.Data;
                //string token = data["GLBAvatarToken"];
                // TODO - use the Gloebit identity service for userId

                return new User(agentIdStr, null, token);
            }

            public static User Init(UUID agentId, string token) {
                string agentIdStr = agentId.ToString();
                lock(s_tokenMap) {
                    s_tokenMap[agentIdStr] = token;
                }
                return new User(agentIdStr, null, token);
            }

            public string AgentID {
                get { return agentId; }
            }

            public string Token {
                get { return token; }
            }
        }

        public GloebitAPI(string key, string keyAlias, string secret, Uri url) {
            m_key = key;
            m_keyAlias = keyAlias;
            m_secret = secret;
            m_url = url;
            // TODO: Populate token map from file
        }
        
        /************************************************/
        /******** OAUTH2 AUTHORIZATION FUNCTIONS ********/
        /************************************************/

        /// <summary>
        /// Request Authorization for this grid/region to enact Gloebit functionality on behalf of the specified OpenSim user.
        /// Sends Authorize URL to user which will launch a Gloebit authorize dialog.  If the user launches the URL and approves authorization from a Gloebit account, an authorization code will be returned to the redirect_uri.
        /// This is how a user links a Gloebit account to this OpenSim account.
        /// </summary>
        /// <param name="user">OpenSim User for which this region/grid is asking for permission to enact Gloebit functionality.</param>
        public void Authorize(IClientAPI user) {

            //********* BUILD AUTHORIZE QUERY ARG STRING ***************//
            Dictionary<string, string> auth_params = new Dictionary<string, string>();

            auth_params["client_id"] = m_key;
            auth_params["r"] = m_keyAlias;
            auth_params["scope"] = "user balance transact";
            auth_params["redirect_uri"] = String.Format("{0}?agentId={1}", REDIRECT_URI, user.AgentId);
            auth_params["response_type"] = "code";
            auth_params["user"] = user.AgentId.ToString();
            // TODO - make use of 'state' param for XSRF protection
            // auth_params["state"] = ???;

            ArrayList query_args = new ArrayList();
            foreach(KeyValuePair<string, string> p in auth_params) {
                query_args.Add(String.Format("{0}={1}", p.Key, HttpUtility.UrlEncode(p.Value)));
            }

            string query_string = String.Join("&", (string[])query_args.ToArray(typeof(string)));

            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Authorize query_string: {0}", query_string);

            //********** BUILD FULL AUTHORIZE REQUEST URI **************//

            Uri request_uri = new Uri(m_url, String.Format("oauth2/authorize?{0}", query_string));
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Authorize request_uri: {0}", request_uri);
            //WebRequest request = WebRequest.Create(request_uri);
            //request.Method = "GET";

            //HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            //string status = response.StatusDescription;
            //StreamReader response_stream = new StreamReader(response.GetResponseStream());
            //string response_str = response_stream.ReadToEnd();
            //m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Authorize response: {0}", response_str);
            //response.Close();
            
            //*********** SEND AUTHORIZE REQUEST URI TO USER ***********//
            // currently can not launch browser directly for user, so send in message

            string message = String.Format("To use Gloebit currency, please autorize Gloebit to link to your avatar's account on this web page: {0}", request_uri);
            // GridInstantMessage im = new GridInstantMessage();
            // im.fromAgentID = Guid.Empty;
            // im.fromAgentName = "Gloebit";
            // im.toAgentID = user.AgentId.Guid;
            // im.dialog = (byte)19;  // Object message
            // im.fromGroup = false;
            // im.message = message;
            // im.imSessionID = UUID.Random().Guid;
            // im.offline = 0;
            // im.Position = Vector3.Zero;
            // im.binaryBucket = new byte[0];
            // im.ParentEstateID = 0;
            // im.RegionID = Guid.Empty;
            // im.timestamp = (uint)Util.UnixTimeSinceEpoch();
            // 
            // user.SendInstantMessage(im);
            user.SendBlueBoxMessage(UUID.Zero, "Gloebit", message);
            // use SendBlueBoxMessage as all others including SendLoadURL truncate to 255 char or below

        }
        
        /// <summary>
        /// Exchanges an authorization code granted from the Authorize endpoint for an access token necessary for enacting Gloebit functionality on behalf of this OpenSim user.
        /// This is the second phase of the OAuth2 process.  It is activated by the redirect_uri of the Authorize function.
        /// This occurs completely behind the scenes for security purposes.
        /// </summary>
        /// <returns>The authenticated User object containing the access token necessary for enacting Gloebit functionality on behalf of this OpenSim user.</returns>
        /// <param name="user">OpenSim User for which this region/grid is asking for permission to enact Gloebit functionality.</param>
        /// <param name="auth_code">Authorization Code returned to the redirect_uri from the Gloebit Authorize endpoint.</param>
        public User ExchangeAccessToken(IClientAPI user, string auth_code) {
            
            //TODO stop logging auth_code
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.ExchangeAccessToken Name:[{0}] AgentID:{1} auth_code:{1}", user.Name, user.AgentId, auth_code);
            
            // ************ BUILD EXCHANGE ACCESS TOKEN POST REQUEST ******** //

            OSDMap auth_params = new OSDMap();
            ////Dictionary<string,string> auth_params = new Dictionary<string,string>();

            auth_params["client_id"] = m_key;
            auth_params["client_secret"] = m_secret;
            auth_params["code"] = auth_code;
            auth_params["grant_type"] = "authorization_code";
            auth_params["scope"] = "user balance transact";
            auth_params["redirect_uri"] = REDIRECT_URI;
            
            HttpWebRequest request = BuildGloebitRequest("oauth2/access-token", "POST", null, "application/x-www-form-urlencoded", auth_params);
            if (request == null) {
                // ERROR
                m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.oauth2/access-token failed to create HttpWebRequest");
                return null;
            }

            // ************ PARSE AND HANDLE EXCHANGE ACCESS TOKEN RESPONSE ********* //

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            string status = response.StatusDescription;
            using(StreamReader response_stream = new StreamReader(response.GetResponseStream())) {
                string response_str = response_stream.ReadToEnd();
                // TODO - do not actually log the token
                m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.ExchangeAccessToken response: {0}", response_str);
                OSDMap responseData = (OSDMap)OSDParser.DeserializeJson(response_str);

                string token = responseData["access_token"];
                // TODO - do something to handle the "refresh_token" field properly
                if(token != String.Empty) {
                    User u = User.Init(user.AgentId, token);
                    return u;
                } else {
                    m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.ExchangeAccessToken error: {0}, reason: {1}", responseData["error"], responseData["reason"]);
                    return null;
                }
            }

        }
        
        /***********************************************/
        /********* GLOEBIT FUNCTIONAL ENDPOINS *********/
        /***********************************************/
        
        // ******* GLOEBIT BALANCE ENDPOINTS ********* //
        // requires "balance" in scope of authorization token
        

        /// <summary>
        /// Requests the Gloebit balance for the OpenSim user with this OpenSim agentID.
        /// Returns zero if a link between this OpenSim user and a Gloebit account have not been created and the user has not granted authorization to this grid/region.
        /// Requires "balance" in scope of authorization token.
        /// </summary>
        /// <returns>The Gloebit balance for the Gloebit accunt the user has linked to this OpenSim agentID on this grid/region.  Returns zero if a link between this OpenSim user and a Gloebit account has not been created and the user has not granted authorization to this grid/region.</returns>
        /// <param name="user">User object for the OpenSim user for whom the balance request is being made. <see cref="GloebitAPI.User.Get(UUID)"/></param>
        public double GetBalance(User user) {
            
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.balance for agentID:{0}", user.AgentID);
            
            //************ BUILD GET BALANCE GET REQUEST ********//
            
            HttpWebRequest request = BuildGloebitRequest("balance", "GET", user);
            if (request == null) {
                // ERROR
                m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.balance failed to create HttpWebRequest");
                return 0;
            }
            
            //************ PARSE AND HANDLE GET BALANCE RESPONSE *********//
            
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            string status = response.StatusDescription;
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.balance status:{0}", status);
            using(StreamReader response_stream = new StreamReader(response.GetResponseStream())) {
                string response_str = response_stream.ReadToEnd();

                OSDMap responseData = (OSDMap)OSDParser.DeserializeJson(response_str);
                m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.balance responseData:{0}", responseData.ToString());

                double balance = responseData["balance"].AsReal();
                return balance;
            }

        }
        
        // ******* GLOEBIT TRANSACT ENDPOINTS ********* //
        // requires "transact" in scope of authorization token

        /// <summary>
        /// Request Gloebit transaction for the gloebit amount specified from the sender to the owner of the Gloebit app this module is connected to.
        /// </summary>
        /// <param name="sender">User object for the user sending the gloebits. <see cref="GloebitAPI.User.Get(UUID)"/></param>
        /// <param name="senderName">OpenSim Name of the user on this grid sending the gloebits.</param>
        /// <param name="amount">quantity of gloebits to be transacted.</param>
        /// <param name="description">Description of purpose of transaction recorded in Gloebit transaction histories.</param>
        public void Transact(User sender, string senderName, int amount, string description) {
            
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.transact senderID:{0} senderName:{1} amount:{2} description:{3}", sender.AgentID, senderName, amount, description);
            
            UUID transactionId = UUID.Random();

            OSDMap transact_params = new OSDMap();

            transact_params["version"] = 1;
            transact_params["application-key"] = m_key;
            transact_params["request-created"] = (int)(DateTime.UtcNow.Ticks / 10000000);  // TODO - figure out if this is in the right units
            transact_params["username-on-application"] = String.Format("{0} - {1}", senderName, sender.AgentID);

            transact_params["transaction-id"] = transactionId.ToString();
            transact_params["gloebit-balance-change"] = amount;
            transact_params["asset-code"] = description;
            transact_params["asset-quantity"] = 1;
            
            HttpWebRequest request = BuildGloebitRequest("transact", "POST", sender, "application/json", transact_params);
            if (request == null) {
                // ERROR
                m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.transact failed to create HttpWebRequest");
                return;
                // TODO once we return, return error value
            }

            //************ PARSE AND HANDLE TRANSACT RESPONSE *********//
            
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            string status = response.StatusDescription;
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Transact response status: {0}", status);
            using(StreamReader response_stream = new StreamReader(response.GetResponseStream())) {
                string response_str = response_stream.ReadToEnd();
                m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Transact response: {0}", response_str);

                OSDMap responseData = (OSDMap)OSDParser.DeserializeJson(response_str);

                bool success = (bool)responseData["success"];
                // TODO: if success=false: id, balance, product-count are invalid.  Do not set balance.
                double balance = responseData["balance"].AsReal();
                string reason = responseData["reason"];
                m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.Transact success: {0} balance: {1} reason: {2}", success, balance, reason);
                // TODO - update the user's balance
            }
        }
        
        // TODO: create U2U endpoint in Gloebit system
        // TODO: does recipient have to authorize app?  Do they need to become a merchant on that platform or opt in to agreeing to receive gloebits?  How do they currently authorize sale on a grid?
        // TODO: can funds be sent to email address if recipient has not yet linked an account on this system?
        // TODO: Should we pass a bool for charging a fee or the actual fee % -- to the module owner --- could always charge a fee.  could be % set in app for when charged.  could be % set for each transaction type in app.
        // TODO: Should we always charge our fee, or have a bool or transaction type for occasions when we may not charge?
        // TODO: Do we need an endpoint for reversals/refunds, or just an admin interface from Gloebit?
        
        /// <summary>
        /// Request Gloebit transaction for the gloebit amount specified from the sender to the recipient.
        /// </summary>
        /// <param name="senderID">User object for the user sending the gloebits. <see cref="GloebitAPI.User.Get(UUID)"/></param>
        /// <param name="senderName">OpenSim Name of the user on this grid sending the gloebits.</param>
        /// <param name="recipient">User object for the user receiving the gloebits. <see cref="GloebitAPI.User.Get(UUID)"/></param>
        /// <param name="recipientName">OpenSim Name of the user on this grid receiving the gloebits.</param>
        /// <param name="amount">quantity of gloebits to be transacted.</param>
        /// <param name="description">Description of purpose of transaction recorded in Gloebit transaction histories.</param>
        
        public void TransactU2U(User sender, string senderName, User recipient, string recipientName, int amount, string description) {
            
            // ************ IDENTIFY GLOEBIT RECIPIENT ******** //
            // TODO: How do we identify recipient?  Get email from profile from OpenSim UUID?
            // TODO: If we use emails, we may need to make sure account merging works for email/3rd party providers.
            // TODO: If we allow anyone to receive, need to ensure that gloebits received are locked down until user authenticates as merchant.
            
            // ************ BUILD AND SEND TRANSACT U2U POST REQUEST ******** //
            
            UUID transactionId = UUID.Random();
            
            OSDMap transact_params = new OSDMap();
            
            transact_params["version"] = 1;
            transact_params["application-key"] = m_key;
            transact_params["request-created"] = (int)(DateTime.UtcNow.Ticks / 10000000);  // TODO - figure out if this is in the right units
            transact_params["username-on-application"] = String.Format("{0} - {1}", senderName, sender.AgentID);
            
            transact_params["transaction-id"] = transactionId.ToString();
            transact_params["gloebit-balance-change"] = amount;
            transact_params["asset-code"] = description;
            transact_params["asset-quantity"] = 1;
            
            // TODO - add params describing recipient, transaction type, fees
            
            HttpWebRequest request = BuildGloebitRequest("transact-U2U", "POST", sender, "application/json", transact_params);
            if (request == null) {
                // ERROR
                m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.transact-U2U failed to create HttpWebRequest");
                return;
                // TODO once we return, return error value
            }
            
            //************ PARSE AND HANDLE TRANSACT U2U RESPONSE *********//
            
            // TODO - implement
            
            
        }
    
        /***********************************************/
        /********* GLOEBIT API HELPER FUNCTIONS ********/
        /***********************************************/
    
        // TODO: OSDMap or Dictionary for params
    
        /// <summary>
        /// Build an HTTPWebRequest for a Gloebit endpoint.
        /// </summary>
        /// <param name="relative_url">endpoint & query args.</param>
        /// <param name="method">HTTP method for request -- eg: "GET", "POST".</param>
        /// <param name="user">User object for this authenticated user if one exists.</param>
        /// <param name="content_type">content type of post/put request  -- eg: "application/json", "application/x-www-form-urlencoded".</param>
        /// <param name="paramMap">parameter map for body of request.</param>
        private HttpWebRequest BuildGloebitRequest(string relativeURL, string method, User user, string contentType = "", OSDMap paramMap = null) {
            
            // TODO: stop logging paramMap which can include client_secret
            m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest relativeURL:{0}, method:{1}, contentType:{2}, paramMap:{3}", relativeURL, method, contentType, paramMap);
        
            // combine Gloebit base url with endpoint and query args in relative url.
            Uri requestURI = new Uri(m_url, relativeURL);
        
            // Create http web request from URL
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestURI);
        
            // Add authorization header
            if (user != null && user.Token != "") {
                request.Headers.Add("Authorization", String.Format("Bearer {0}", user.Token));
            }
        
            // Set request method and body
            request.Method = method;
            switch (method) {
                case "GET":
                    m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest GET relativeURL:{0}", relativeURL);
                    break;
                case "POST":
                case "PUT":
                    string paramString = "";
                    byte[] postData = null;
                    request.ContentType = contentType;
                
                    // Build paramString in proper format
                    if (paramMap != null) {
                        if (contentType == "application/x-www-form-urlencoded") {
                            StringBuilder paramBuilder = new StringBuilder();
                            foreach (KeyValuePair<string, OSD> p in (OSDMap)paramMap) {
                                if(paramBuilder.Length != 0) {
                                    paramBuilder.Append('&');
                                }
                                paramBuilder.AppendFormat("{0}={1}", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value.ToString()));
                            }
                            paramString = paramBuilder.ToString();
                        } else if (contentType == "application/json") {
                            paramString = OSDParser.SerializeJsonString(paramMap);
                        } else {
                            // ERROR - we are not handling this content type properly
                            m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest relativeURL:{0}, unrecognized content type:{1}", relativeURL, contentType);
                            return null;
                        }
                
                        // Byte encode paramString and write to requestStream
                        postData = System.Text.Encoding.UTF8.GetBytes(paramString);
                        request.ContentLength = postData.Length;
                        using (Stream s = request.GetRequestStream()) {
                            s.Write(postData, 0, postData.Length);
                        }
                    } else {
                        // Probably should be a GET request if it has no paramMap
                        m_log.WarnFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest relativeURL:{0}, Empty paramMap on {1} request", relativeURL, method);
                    }
                    // TODO: stop logging postData which can include client_secret
                    m_log.InfoFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest {0} relativeURL:{1}, postData:{2}, Length:{3}", method, relativeURL, System.Text.Encoding.Default.GetString(postData), postData.Length);
                    break;
                default:
                    // ERROR - we are not handling this request type properly
                    m_log.ErrorFormat("[GLOEBITMONEYMODULE] GloebitAPI.BuildGloebitRequest relativeURL:{0}, unrecognized web request method:{1}", relativeURL, method);
                    return null;
            }
            return request;
        }
    }
}
