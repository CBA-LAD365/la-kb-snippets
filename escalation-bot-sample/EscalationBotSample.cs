using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using Microsoft.Bot.Builder.ConnectorEx;
using System.Timers;
using Newtonsoft.Json;
using Cafex.LiveAssist.Chat;


namespace Bot_Application2.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static Sdk sdk;
        private static ChatContext chatContext;
        private static string conversationRef;
        private static Timer timer;


        public Task StartAsync(IDialogContext context)
        {
            sdk = sdk ?? new Sdk(new SdkConfiguration()
            {
                AccountNumber = "__CHANGE_ME__"
            });

            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(
            IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (chatContext != null)
            {
                // As chatContext is not null we already have an escalated chat.
                // Post the incoming message line to the escalated chat
                await sdk.PostLine(activity.Text, chatContext);
            }
            else if (activity.Text.Contains("help"))
            {
                // "help" within the message is our escalation trigger.
                await context.PostAsync("Escalating to agent");
                await Escalate(activity); // Implemented in next step.
            }
            else
            {
                // Bot just Echos
                await context.PostAsync($"You sent{activity.Text}");
            }
            context.Wait(MessageReceivedAsync);
        }

        private async Task Escalate(Activity activity)
        {
            // This is our reference to the upstream conversation
            conversationRef = JsonConvert.SerializeObject(
           activity.ToConversationReference());

            var chatSpec = new ChatSpec()
            {
                // Set Agent skill to target
                Skill = "__CHANGE_ME__",
                VisitorName = activity.From.Name
            };

            // Start timer to poll for Live Assist chat events
            if (timer == null)
            {
                timer = timer ?? new Timer(5000);
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
                var reply = JsonConvert.DeserializeObject<ConversationReference>(conversationRef)
                    .GetPostToBotMessage().CreateReply();

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
                                        reply.From.Name = "system";
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
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}