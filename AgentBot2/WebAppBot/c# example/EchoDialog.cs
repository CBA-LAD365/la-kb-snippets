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
        protected int count = 1;
        static string TRANSFER_MESSAGE = "transfer to ";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

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

                        if (message.Text.StartsWith(TRANSFER_MESSAGE))
                        {
                            var reply = context.MakeMessage();
                            var transferTo = message.Text.Substring(TRANSFER_MESSAGE.Length);

                            reply.ChannelData = new LiveAssistChannelData()
                            {
                                Type = "transfer",
                                Skill = transferTo
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
            }else if (message.Text == "reset")
            {
                PromptDialog.Confirm(
                    context,
                    AfterResetAsync,
                    "Are you sure you want to reset the count?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.Auto);
            }
            else
            {
                await context.PostAsync($"{this.count++}: You said {message.Text}");
                context.Wait(MessageReceivedAsync);
            }
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }

        // Live Assist custom channel data.
        public class LiveAssistChannelData
        {
            [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
            public string Type { get; set; }

            [JsonProperty("skill", NullValueHandling = NullValueHandling.Ignore)]
            public string Skill { get; set; }
        }
    }
}