/*-----------------------------------------------------------------------------
A simple echo bot for the Microsoft Bot Framework. 
-----------------------------------------------------------------------------*/
var restify = require('restify');
var builder = require('botbuilder');
var botbuilder_azure = require("botbuilder-azure");
var builder_cognitiveservices = require("botbuilder-cognitiveservices");
var transfer_skill = 'human';
// Setup Restify Server
var server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, function () {
   console.log('%s listening to %s', server.name, server.url); 
});
  
// Create chat connector for communicating with the Bot Framework Service
var connector = new builder.ChatConnector({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword,
    openIdMetadata: process.env.BotOpenIdMetadata 
});
// Listen for messages from users 
server.post('/api/messages', connector.listen());
/*----------------------------------------------------------------------------------------
* Bot Storage: This is a great spot to register the private state storage for your bot. 
* We provide adapters for Azure Table, CosmosDb, SQL Azure, or you can implement your own!
* For samples and documentation, see: https://github.com/Microsoft/BotBuilder-Azure
* ---------------------------------------------------------------------------------------- */
var tableName = 'botdata';
var azureTableClient = new botbuilder_azure.AzureTableClient(tableName, process.env['AzureWebJobsStorage']);
var tableStorage = new botbuilder_azure.AzureBotStorage({ gzipData: false }, azureTableClient);
// Create your bot with a function to receive messages from the user
var bot = new builder.UniversalBot(connector);
bot.set('storage', tableStorage);

var recognizer = new builder_cognitiveservices.QnAMakerRecognizer({

    knowledgeBaseId: process.env.QnAKnowledgebaseId,

    authKey: process.env.QnAAuthKey || process.env.QnASubscriptionKey, // Backward compatibility with QnAMaker (Preview)

    endpointHostName: process.env.QnAEndpointHostName});

var basicQnAMakerDialog = new builder_cognitiveservices.QnAMakerDialog({
    recognizers: [recognizer],
                defaultMessage: 'No match! Try changing the query terms!',
                qnaThreshold: 0.3}
);
basicQnAMakerDialog.invokeAnswer = function(session, recognizeResult, threshold, noMatchMessage) {
    var qnaMakerResult = recognizeResult;
    session.privateConversationData.qnaFeedbackUserQuestion = session.message.text;
    if (qnaMakerResult.score >= threshold && qnaMakerResult.answers.length > 0) {
        if (basicQnAMakerDialog.isConfidentAnswer(qnaMakerResult) || basicQnAMakerDialog.qnaMakerTools == null) {
            basicQnAMakerDialog.respondFromQnAMakerResult(session, qnaMakerResult);
            basicQnAMakerDialog.defaultWaitNextMessage(session, qnaMakerResult);
        } else {
            basicQnAMakerDialog.qnaFeedbackStep(session, qnaMakerResult);
        }
    } else {
        noMatch(session, noMatchMessage, qnaMakerResult);
    }
};
basicQnAMakerDialog.respondFromQnAMakerResult = function(session, qnaMakerResult) {
    var response = qnaMakerResult.answers[0].answer.split('/t ');
    session.send(response[0]);
    if (response[1]) {
        session.transferSkill = response[1];
        session.beginDialog('/force_transfer');
    }
};
function noMatch(session, noMatchMessage, qnaMakerResult) {
    session.beginDialog('/offer_transfer');
};
bot.dialog('/qna', basicQnAMakerDialog);
bot.dialog('/offer_transfer', [
    function(session) {
        builder.Prompts.confirm(session, "I'm afraid I cannot help you with that. Would you like to be transferred to a human agent?");
    },
    function(session, results) {
        if (results.response) {
            session.send('I will transfer you now.');
            var msg = new builder.Message(session).sourceEvent({
                directline: {
                    type: "transfer",
                    skill: transfer_skill
                }
            });
            session.send(msg);
        } else {
            session.endDialog('Great! What else can I help you with?');
        }
    }
]);
bot.dialog('/force_transfer', function(session) {
    session.send('I am going to transfer you to a human agent that can assist you better.');
    var msg = new builder.Message(session).sourceEvent({
        directline: {
            type: "transfer",
            skill: session.transferSkill
        }
    });
    session.send(msg);
});
bot.dialog('/', function(session) {
    switch (session.message.sourceEvent.type) {
        case "visitorContextData":
            //this is the first message type received and marks the start of a chat conversation
            var hi = 'Hi';
            if (session.message.sourceEvent.firstName) {
                hi += ' ' + session.message.sourceEvent.firstName;
            }
            session.send('%s, I am the resident Beyond Hotel bot and I am here to help you. Ask me any question you would like and I will assist you.', hi);
            session.send('If I am unable to answer your question or meet your needs, I will happily transfer you to our human department!');
            session.send('So please, let me know, how can I help you today?');
            break;
        case "systemMessage":
            //I am not interested in any system messages that are generated
            break;
        case "transferFailed":
            session.send('I am sorry, but I am unable to transfer you at the moment: ' + session.message.sourceEvent.reason);
            break;
        case "otherAgentMessage":
            //If this is an internal message, then I will use it as a trigger to transfer to the skill group specified.
            if (session.message.sourceEvent.isInternal) {
                session.transferSkill = session.message.text;
                session.beginDialog('/force_transfer');
            }
            break;
        case "visitorMessage":
            session.send(new builder.Message(session).sourceEvent({
                directline: {
                    type: "typing"
                }
            }));
            session.beginDialog('/qna');
            break;
        default:
            session.beginDialog('/qna');
    }
});
