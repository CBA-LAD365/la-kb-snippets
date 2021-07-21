// Initially Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Initially Generated with EchoBot .NET Template version v4.13.2

using Cafex.LiveAssist.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xrm.Tools.WebAPI;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace EchoBot.Bots
{
    /* A simple adaptation of Microsoft Echobot handling one Live Assist escalation at a time.
       After escalation transfer the bot passes messages between visitor and agent
       A production solution handling concurrent chats would persist multiple chatContexts, conversationRefs, chatActivityIds,
       transcripts and botChatStartTime. 
       For authenticated chats it would need a mechanism to get the contactId */
    public class EchoBot : ActivityHandler
    {
        private static Sdk sdk;
        private static ChatContext chatContext;
        private static object chatActivityId;
        private static List<TranscriptLine> transcript = new();
        private static string conversationRef;
        private static System.Timers.Timer timer;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
        private static DateTime botChatStartTime;

        private string CrmDomain = "https://mydomain.crmX.dynamics.com";

        //Generated in Azure AAD
        private string ApplicationClientId = "xxxxxxx-xxxxx-xxxxx-xxxx-xxxxxxxxxxxx"; 
        private string ApplicationClientSecret = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        //contact GUID from CRM
        private string contactId = "xxxxxxx-xxxxx-xxxxx-xxxx-xxxxxxxxxxxx"; 

        //CRM's tenant ID
        private string tenantId = "xxxxxxx-xxxxx-xxxxx-xxxx-xxxxxxxxxxxx"; 

        // Live assist account number.
        private readonly string LA365_AccountNumber = "6199999";
        // Host name of the context data service depending on region na1, eu1 or ap1
        private readonly string LA365_ContextDataHost = "service.na1.liveassistfor365.com";

        //Note: IHttpClientFactory injected when services.AddHttpClient(); added to ConfigureServices(IServiceCollection) method in your bot Startup.cs file
        public EchoBot(IHttpClientFactory httpClientFactory) =>
            _httpClientFactory = httpClientFactory;
            
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Bot says Hello! If you want to speak with an agent type 'transfer'";

            botChatStartTime = DateTime.Now;
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
            sdk ??= new Sdk(new SdkConfiguration()
            {
                AccountNumber = LA365_AccountNumber,
                ContextDataHost = LA365_ContextDataHost
            });
        }
        
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (chatContext != null)
            {
                // As chatContext is not null we already have an escalated chat.
                // Post the incoming message line to the agent handling the chat
                await sdk.PostLine(turnContext.Activity.Text, chatContext);
            }
            else { 
                // Add visitor line to transcript               
                transcript.Add(new TranscriptLine()
                {
                    IsBot = false,
                    Timestamp = DateTime.Now,
                    SrcName = turnContext.Activity.From.Name,
                    Line = turnContext.Activity.Text
                });

                var replyText = $"Echo: {turnContext.Activity.Text}";

                // Visitor has signalled to Escalate to agent
                if(replyText.Contains("transfer")) {
                    await Escalate(context: turnContext);
                }
                else {
                    // Add bot reply transcript
                    transcript.Add(new TranscriptLine()
                    {
                        IsBot = true,
                        Timestamp = DateTime.Now,
                        SrcName = turnContext.Activity.From.Name,
                        Line = replyText
                    });
                    await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                }
            }
        }


        private async Task Escalate(ITurnContext context)
        {
            var activity = context.Activity;
            chatActivityId = await CreateLiveAssistActivity();
            Console.WriteLine("created chatActivity Id: " + chatActivityId);
 
            // This is our reference to the upstream conversation
            ConversationReference conRef = new ConversationReference
            {
                ActivityId = activity.Id,
                Bot = new ChannelAccount { Id = activity.Recipient.Id, Name = activity.Recipient.Name },
                ChannelId = activity.ChannelId,
                User = new ChannelAccount { Id = activity.From.Id, Name = activity.From.Name },
                Conversation = new ConversationAccount { Id = activity.Conversation.Id, IsGroup = activity.Conversation.IsGroup, Name = activity.Conversation.Name },
                ServiceUrl = activity.ServiceUrl
            };
            conversationRef = JsonConvert.SerializeObject(conRef);

            var chatSpec = new ChatSpec()
            {
                // Set Agent skill to target
                Skill = "human",
                Transcript = transcript,
                VisitorName = "Visitor First or FullName",
                ContextData = CreateJwt
            };
 
            // Start timer to poll for Live Assist chat events
            if (timer == null)
            {
                timer = timer ?? new System.Timers.Timer(3000);
                // OnTimedEvent is implemented in the next step
                timer.Elapsed += (sender, e) => OnTimedEvent(sender, e);
                timer.Start();
            }
 
            // Request a chat via the Sdk    
            chatContext = await sdk.RequestChat(chatSpec);
        }  

        async void OnTimedEvent(Object source, ElapsedEventArgs eea) 
        {                
            if (chatContext != null) 
            { 
                // Create an upstream reply 
                var reply = JsonConvert.DeserializeObject<ConversationReference>(conversationRef).GetContinuationActivity().CreateReply(); 
  
                // Create upstream connection on which to send reply  
                var client = new ConnectorClient(new Uri(reply.ServiceUrl)); 
  
                // Poll Live Assist for events 
                var chatInfo = await sdk.Poll(chatContext); 
  
                if (chatInfo != null) 
                { 
                    // ChatInfo.ChatEvents will contain events since last call to poll. 
                    if (chatInfo.ChatEvents != null && chatInfo.ChatEvents.Count > 0) 
                    { 
                        foreach (ChatEvent e in chatInfo.ChatEvents) 
                        { 
                            switch (e.Type) 
                            { 
                                // type is either "state" or "line". 
                                case "line": 
                                    // Source is either: "system", "agent" or "visitor" 
                                    if (e.Source.Equals("system")) 
                                    { 
                                        reply.From.Name = "system"; //do not change this 
  
                                     } 
                                    else if (e.Source.Equals("agent")) 
                                    { 
                                        reply.From.Name = chatInfo.AgentName; 
                                    } 
                                    else 
                                    { 
                                        break; 
                                    } 
  
                                    reply.Type = "message"; 
                                    reply.Text = e.Text; 
                                    client.Conversations.ReplyToActivity(reply); 
                                    break; 
  
                                case "state": 
                                    // State changes 
                                    // Valid values: "waiting", "chatting", "ended" 
                                    if (chatInfo.State.Equals("ended")) 
                                    { 
                                        chatContext = null; 
                                        transcript = new List<TranscriptLine>(); 
                                    } 
                                    break; 
                            } 
                        } 
                    } 
                } 
            } 
        }

        public string CreateJwt(string contextId)
        {
            var contextData = new ContextData()
            {
                 chatActivityId = new AssertedString()
                 {
                     value = chatActivityId.ToString()
                 },
                customer = new Customer()
                {
                    id = new AssertedString()
                    {
                        value = contactId,
                        isAsserted = true
                    }
                }
            };
            return Jwt.Create(contextId, contextData);
        }

        //D365 chat Activity Creation:
        public async Task<Guid> CreateLiveAssistActivity()
        {
            var crmapi = await GetCRMWebAPI();

            dynamic data = new System.Dynamic.ExpandoObject();
            var dataIndexer = data as IDictionary<string, Object>;

            data.subject = "Not used as overwritten with wording: 'chat with AGENT_NAME SKILL' when chat is escalated";
            //sets activity status to open
            data.statecode = 0;
            dataIndexer["cxlvhlp_chatstarttime"] = botChatStartTime;
            dataIndexer["cxlvhlp_IsCustomerAuthenticated"] = true;
            dataIndexer["cxlvhlp_customer_cxlvhlp_chatactivity_contact@odata.bind"] = $"/contacts({contactId})";
            dataIndexer["regardingobjectid_contact@odata.bind"] = $"/contacts({contactId})";
            dataIndexer["cxlvhlp_ChatOrigin"] = "100000000"; //web or "100000001" for mobile
            dataIndexer["cxlvhlp_SourcePageURL"] = "https://bot_site.com";
            dataIndexer["cxlvhlp_botname"] = "LA365EnablerBot";
            dataIndexer["cxlvhlp_channel"] = "Bot Channel 1";
            dataIndexer["cxlvhlp_NumberOfChatTransfers"] = "1";
            dataIndexer["cxlvhlp_botescalation"] = true;
            dataIndexer["cxlvhlp_escalatedon"] = DateTime.Now;
            
            Guid chatActivityId = await crmapi.Create("cxlvhlp_chatactivities", data);
            return chatActivityId;
        }

        public async Task<CRMWebAPI> GetCRMWebAPI()
        {
            string authority = "https://login.microsoftonline.com/";

            var clientcred = new ClientCredential(ApplicationClientId, ApplicationClientSecret);
            var authContext = new AuthenticationContext(authority + tenantId);
            var authenticationResult = await authContext.AcquireTokenAsync(CrmDomain, clientcred);

            return new CRMWebAPI(CrmDomain + "/api/data/v9.1/", authenticationResult.AccessToken);
        }
    }
}