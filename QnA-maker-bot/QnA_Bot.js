// For more information about this template visit http://aka.ms/azurebots-node-qnamaker

"use strict";
const transfer_skill = 'human'; // Assign a real skill
var builder = require("botbuilder");
var botbuilder_azure = require("botbuilder-azure");
var builder_cognitiveservices = require("botbuilder-cognitiveservices");
var path = require('path');

var useEmulator = (process.env.NODE_ENV == 'development');

var connector = useEmulator ? new builder.ChatConnector() : new botbuilder_azure.BotServiceConnector({
    appId: process.env['MicrosoftAppId'],
    appPassword: process.env['MicrosoftAppPassword'],
    stateEndpoint: process.env['BotStateEndpoint'],
    openIdMetadata: process.env['BotOpenIdMetadata']
});

var bot = new builder.UniversalBot(connector);
bot.localePath(path.join(__dirname, './locale'));

bot.use({
    botbuilder: function (session, next) {
        next();
    },
    send: function (event, next) {
        next();
    }
});

var recognizer = new builder_cognitiveservices.QnAMakerRecognizer({
                knowledgeBaseId: process.env.QnAKnowledgebaseId,
    subscriptionKey: process.env.QnASubscriptionKey});

var basicQnAMakerDialog = new builder_cognitiveservices.QnAMakerDialog({
    recognizers: [recognizer],
                defaultMessage: 'No match! Try changing the query terms!',
                qnaThreshold: 0.3}
);

basicQnAMakerDialog.invokeAnswer = function (session, recognizeResult, threshold, noMatchMessage) {
        var qnaMakerResult = recognizeResult;
        session.privateConversationData.qnaFeedbackUserQuestion = session.message.text;
        if (qnaMakerResult.score >= threshold && qnaMakerResult.answers.length > 0) {
            if (basicQnAMakerDialog.isConfidentAnswer(qnaMakerResult) || basicQnAMakerDialog.qnaMakerTools == null) {
                basicQnAMakerDialog.respondFromQnAMakerResult(session, qnaMakerResult);
                basicQnAMakerDialog.defaultWaitNextMessage(session, qnaMakerResult);
            }
            else {
                basicQnAMakerDialog.qnaFeedbackStep(session, qnaMakerResult);
            }
        }
        else {
            // Overridden case with this method
            noMatch(session, noMatchMessage, qnaMakerResult);
        }
    };

basicQnAMakerDialog.respondFromQnAMakerResult = function (session, qnaMakerResult) {

        // If the response invokes a transfer, parse the response
        let response = qnaMakerResult.answers[0].answer.split('/t ');

        session.send(response[0]);

        if (response[1])
        {
            session.transferSkill = response[1];
            session.beginDialog('/force_transfer');
        }
    };

function noMatch(session, noMatchMessage, qnaMakerResult) {
            session.beginDialog('/offer_transfer');

};

bot.dialog('/qna', basicQnAMakerDialog);

bot.dialog('/offer_transfer', [
    function (session)
    {
        // In built sentiment analysis prompt - https://docs.microsoft.com/en-us/bot-framework/nodejs/bot-builder-nodejs-dialog-prompt#promptsconfirm
        builder.Prompts.confirm(session, "I'm afraid I cannot help you with that. Would you like to be transferred to a human agent?");
    },
    function(session, results)
    {
        if (results.response)
        {
            session.send('I will transfer you now.');

            // Live Assist specific messaging - https://www.liveassistfor365.com/en/support/knowledge-base/bot-agent-connector-api/
            let msg = new builder.Message(session).sourceEvent({directline: {type: "transfer", skill: transfer_skill}});
            session.send(msg);
        }
        else
        {
            session.endDialog('Great! What else can I help you with?');
        }
    }
]);

bot.dialog('/force_transfer', function(session){
    session.send('I am going to transfer you to a human agent that can assist you better.');
    // Live Assist
    let msg = new builder.Message(session).sourceEvent({directline: {type: "transfer", skill: session.transferSkill}});
    session.send(msg);
});

bot.dialog('/', function (session) {
    switch(session.message.sourceEvent.type)
    {
        case "visitorContextData":

            session.send('Hi, I am a bot and I am here to help you. Ask me any question you would like and I will assist you.');
            session.send('If I am unable to answer your question or meet your needs, I will happily transfer you to our human department!');
            session.send('So please, let me know, how can I help you today?');
            break;

        case "systemMessage":
            //I am not interested in any system messages that are generated
            break;

        case "transferFailed":
            session.send('I am sorry, but I am unable to transfer you at the moment: '+session.message.sourceEvent.reason);
            break;

        case "otherAgentMessage":

            //If this is an internal message, then I will use it as a trigger to transfer to the skill group specified.
            //Assume supervisor wants a transfer to this skill
            if (session.message.sourceEvent.isInternal)
            {
                session.transferSkill = session.message.text;
                session.beginDialog('/force_transfer');
            }
            break;

        case "visitorMessage":

            session.beginDialog('/qna');
            break;

        default:
            session.beginDialog('/qna');
    }
});

if (useEmulator) {
    var restify = require('restify');
    var server = restify.createServer();
    server.listen(3978, function() {
        console.log('test bot endpoint at http://localhost:3978/api/messages');
    });
    server.post('/api/messages', connector.listen());
} else {
    module.exports = { default: connector.listen() }
}