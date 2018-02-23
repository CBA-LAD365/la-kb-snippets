using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        string TRANSFER_MESSAGE = "transfer to ";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument as Activity;

            if (message.ChannelId == "directline")
            {
                var laChannelData = message.GetChannelData<LiveAssistChannelData>();

                switch (laChannelData.Type)
                {
                    case "visitorContextData":
                        //process context data if required. This is the first message received so say hello.
                         await context.PostAsync("Hi, I am an echo bot and will repeat everything you said.");
                         break;

                    case "systemMessage":
                        //react to system messages if required
                        break;

                    case "transferFailed":
                        //react to transfer failures if required
                        break;

                    case "otherAgentMessage":
                        //react to messages from a supervisor if required
                        break;

                    case "visitorMessage":
                        // Check for transfer message

                        if(message.Text.StartsWith(TRANSFER_MESSAGE))
                        {
                            var reply = context.MakeMessage();
                            var transferTo = message.Text.Substring(TRANSFER_MESSAGE.Length);

                            reply.ChannelData = new LiveAssistChannelData()
                            {
                              Type = "transfer",
                              Agent = transferTo
                            };
                            
                            await context.PostAsync(reply);
                        }
                        else
                        {
                            await context.PostAsync("You said: " + message.Text);
                        }
                        break;

                    default:
                        await context.PostAsync("This is not a Live Assist message " + laChannelData.Type);
                        break;
                }
            }
            else
            {
                // Not from a directLine channel
                 await context.PostAsync("You said: " + message.Text);
            }
            context.Wait(MessageReceivedAsync);
        }
    }

   // Live Assist custom channel data.
   public class LiveAssistChannelData
   {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type {get; set;}

        [JsonProperty("agent", NullValueHandling = NullValueHandling.Ignore)]
        public string Agent {get; set;}
   }
}